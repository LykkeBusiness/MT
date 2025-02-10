// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Common;
using Common.Log;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Caches;
using MarginTrading.Backend.Services.Policies;
using MarginTrading.Common.Services;

using Polly.Retry;

namespace MarginTrading.Backend.Services.Infrastructure;

public partial class SnapshotBuilderService : ISnapshotBuilderService
{
    private readonly IScheduleSettingsCacheService _scheduleSettingsCacheService;
    private readonly IAccountsCacheService _accountsCacheService;
    private readonly IQuoteCacheService _quoteCacheService;
    private readonly IFxRateCacheService _fxRateCacheService;
    private readonly IDateService _dateService;

    private readonly ITradingEngineRawSnapshotsAdapter _snapshotsRepositoryAdapter;
    private readonly IQueueValidationService _queueValidationService;
    private readonly ILog _log;
    private readonly IFinalSnapshotCalculator _finalSnapshotCalculator;
    private readonly ISnapshotRecreateFlagKeeper _snapshotRecreateFlagKeeper;
    private readonly AsyncRetryPolicy<SnapshotValidationResult> _policy;
    private readonly ITradingEngineSnapshotBuilder _snapshotBuilder;
    private readonly ITradingEngineSnapshotsRepository _repository;
    private static readonly SemaphoreSlim Lock = new(1, 1);
    public static bool IsMakingSnapshotInProgress => Lock.CurrentCount == 0;
    private readonly ISnapshotValidator _snapshotValidator;

    public SnapshotBuilderService(
        IScheduleSettingsCacheService scheduleSettingsCacheService,
        IAccountsCacheService accountsCacheService,
        IQuoteCacheService quoteCacheService,
        IFxRateCacheService fxRateCacheService,
        IDateService dateService,
        ITradingEngineRawSnapshotsAdapter snashotsRepositoryAdapter,
        IQueueValidationService queueValidationService,
        ILog log,
        IFinalSnapshotCalculator finalSnapshotCalculator,
        ISnapshotRecreateFlagKeeper snapshotRecreateFlagKeeper,
        ITradingEngineSnapshotBuilder snapshotBuilder,
        ITradingEngineSnapshotsRepository repository,
        ISnapshotValidator snapshotValidator)
    {
        _scheduleSettingsCacheService = scheduleSettingsCacheService;
        _accountsCacheService = accountsCacheService;
        _quoteCacheService = quoteCacheService;
        _fxRateCacheService = fxRateCacheService;
        _dateService = dateService;
        _snapshotsRepositoryAdapter = snashotsRepositoryAdapter;
        _queueValidationService = queueValidationService;
        _log = log;
        _finalSnapshotCalculator = finalSnapshotCalculator;
        _snapshotRecreateFlagKeeper = snapshotRecreateFlagKeeper;

        _policy = SnapshotStateValidationPolicy.BuildPolicy(log);
        _snapshotBuilder = snapshotBuilder;
        _repository = repository;
        _snapshotValidator = snapshotValidator;
    }

    /// <inheritdoc />
    public async Task<string> MakeTradingDataSnapshot(DateTime tradingDay, string correlationId, SnapshotStatus status = SnapshotStatus.Final)
    {
        if (!_scheduleSettingsCacheService.TryGetPlatformCurrentDisabledInterval(out var disabledInterval))
        {
            //TODO: remove later (if everything will work and we will never go to this branch)
            _scheduleSettingsCacheService.MarketsCacheWarmUp();

            if (!_scheduleSettingsCacheService.TryGetPlatformCurrentDisabledInterval(out disabledInterval))
            {
                throw new Exception($"Trading should be stopped for whole platform in order to make trading data snapshot.");
            }
        }

        if (disabledInterval.Start.AddDays(-1) > tradingDay.Date || disabledInterval.End < tradingDay.Date)
        {
            throw new Exception(
                $"{nameof(tradingDay)}'s Date component must be from current disabled interval's Start -1d to End: [{disabledInterval.Start.AddDays(-1)}, {disabledInterval.End}].");
        }

        if (IsMakingSnapshotInProgress)
        {
            throw new InvalidOperationException("Trading data snapshot creation is already in progress");
        }

        // We must be sure all messages have been processed by history brokers before starting current state validation.
        // If one or more queues contain not delivered messages the snapshot can not be created.
        _queueValidationService.ThrowExceptionIfQueuesNotEmpty(true);

        await Lock.WaitAsync();

        try
        {
            var validationResult = await _policy.ExecuteAsync(() => _snapshotValidator.Validate(correlationId));
            if (!validationResult.IsValid)
            {
                await _log.WriteFatalErrorAsync(nameof(SnapshotBuilderService),
                    nameof(MakeTradingDataSnapshot),
                    validationResult.ToJson(),
                    validationResult.Exception);
                throw validationResult.Exception;
            }

            var orders = validationResult.Cache.GetAllOrders();
            var positions = validationResult.Cache.GetPositions();
            var accounts = _accountsCacheService.GetAll();
            var accountsInLiquidation = await _accountsCacheService.GetAllWhereLiquidationIsRunning().ToListAsync();
            var bestFxPrices = _fxRateCacheService.GetAllQuotes();
            var bestPrices = _quoteCacheService.GetAllQuotes();

            var snapshot = await _snapshotBuilder.WithTradingDay(tradingDay)
                .WithCorrelationId(correlationId)
                .WithStatus(status)
                .WithTimestamp(_dateService.Now())
                .WithOrders(orders, validationResult.Cache.GetRelatedOrders(orders))
                .WithPositions(positions, validationResult.Cache.GetRelatedOrders(positions))
                .WithAccounts(accounts.ToImmutableArray(), accountsInLiquidation.ToImmutableArray())
                .WithFxQuotes(bestFxPrices.ToImmutableDictionary())
                .WithQuotes(bestPrices.ToImmutableDictionary())
                .Build();

            await _snapshotsRepositoryAdapter.AddAsync(snapshot);

            if (status == SnapshotStatus.Draft)
                await _snapshotRecreateFlagKeeper.Set(false);

            return $"Trading data snapshot was written to the storage. {snapshot.GetStatistics()}";
        }
        finally
        {
            Lock.Release();
        }
    }
}
