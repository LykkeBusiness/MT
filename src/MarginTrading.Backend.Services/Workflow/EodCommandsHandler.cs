// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BookKeeper.Client;
using BookKeeper.Client.Responses.Eod;
using BookKeeper.Client.Workflow.Commands;
using BookKeeper.Client.Workflow.Events;

using Common.Log;

using JetBrains.Annotations;

using Lykke.Cqrs;

using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow
{
    [UsedImplicitly]
    public class EodCommandsHandler
    {
        private readonly IQuotesApi _quotesApi;
        private readonly ISnapshotBuilder _snapshotService;
        private readonly IDateService _dateService;
        private readonly IDraftSnapshotKeeperFactory _draftSnapshotKeeperFactory;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly ISnapshotRecreateFlagKeeper _snapshotRecreateFlagKeeper;
        private readonly ILog _log;

        public EodCommandsHandler(
            IQuotesApi quotesApi,
            ISnapshotBuilder snapshotService,
            IDateService dateService,
            IDraftSnapshotKeeperFactory draftSnapshotKeeperFactory,
            IIdentityGenerator identityGenerator,
            ISnapshotRecreateFlagKeeper snapshotRecreateFlagKeeper,
            ILog log)
        {
            _quotesApi = quotesApi;
            _snapshotService = snapshotService;
            _dateService = dateService;
            _draftSnapshotKeeperFactory = draftSnapshotKeeperFactory;
            _identityGenerator = identityGenerator;
            _snapshotRecreateFlagKeeper = snapshotRecreateFlagKeeper;
            _log = log;
        }

        [UsedImplicitly]
        private async Task Handle(CreateSnapshotCommand command, IEventPublisher publisher)
        {
            //deduplication is inside _snapshotService
            try
            {
                var quotes = await _quotesApi.GetCfdQuotes(command.TradingDay);

                if (quotes.ErrorCode != EodMarketDataErrorCodesContract.None)
                {
                    throw new Exception($"Could not receive quotes from BookKeeper: {quotes.ErrorCode.ToString()}");
                }

                var shouldRecreateSnapshot = await _snapshotRecreateFlagKeeper.Get();

                if (shouldRecreateSnapshot && !command.IsMissing)
                {
                    await _snapshotService.MakeTradingDataSnapshot(command.TradingDay,
                        _identityGenerator.GenerateGuid(),
                        SnapshotStatus.Draft);
                }

                var draftSnapshotKeeper = _draftSnapshotKeeperFactory.Create(command.TradingDay);

                await _snapshotService.Convert(command.OperationId,
                    MapQuotes(quotes.EodMarketData.Underlyings),
                    MapFxRates(quotes.EodMarketData.Forex),
                    draftSnapshotKeeper);

                publisher.PublishEvent(new SnapshotCreatedEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                });
            }
            catch (Exception exception)
            {
                await _log.WriteErrorAsync(nameof(EodCommandsHandler), nameof(CreateSnapshotCommand),
                    exception);

                publisher.PublishEvent(new SnapshotCreationFailedEvent
                {
                    OperationId = command.OperationId,
                    CreationTime = _dateService.Now(),
                    FailReason = exception.Message,
                });
            }
        }

        private IEnumerable<ClosingAssetPrice> MapQuotes(IEnumerable<BestPriceContract> bestPrices)
        {
            return bestPrices.Select(x => new ClosingAssetPrice()
            {
                ClosePrice = x.Ask, // equal to bid
                AssetId = x.Id,
            });
        }

        private IEnumerable<ClosingFxRate> MapFxRates(IEnumerable<BestPriceContract> bestPrices)
        {
            return bestPrices.Select(x => new ClosingFxRate()
            {
                ClosePrice = x.Ask, // equal to bid
                AssetId = x.Id,
            });
        }
    }
}