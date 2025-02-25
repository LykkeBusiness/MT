// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Common;
using Common.Log;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Quotes;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Services.Quotes
{
    public class FxRateCacheService : TimerPeriod, IFxRateCacheService
    {
        private readonly ILog _log;
        private readonly IMarginTradingBlobRepository _blobRepository;
        private IDictionary<string, InstrumentBidAskPair> _quotes = new Dictionary<string, InstrumentBidAskPair>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();
        private readonly OrdersCache _ordersCache;
        private const string BlobName = "FxRates";

        public FxRateCacheService(
            ILog log,
            IMarginTradingBlobRepository blobRepository,
            MarginTradingSettings marginTradingSettings,
            OrdersCache ordersCache)
            : base(nameof(FxRateCacheService), marginTradingSettings.BlobPersistence.FxRatesDumpPeriodMilliseconds, log)
        {
            _log = log;
            _blobRepository = blobRepository;
            _ordersCache = ordersCache;

            DisableTelemetry();
        }

        public InstrumentBidAskPair GetQuote(string instrument)
        {
            _lockSlim.EnterReadLock();
            try
            {
                if (!_quotes.TryGetValue(instrument, out var quote))
                    throw new FxRateNotFoundException(instrument, string.Format(MtMessages.FxRateNotFound, instrument));

                return quote;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public Dictionary<string, InstrumentBidAskPair> GetAllQuotes()
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _quotes.ToDictionary(x => x.Key, y => y.Value);
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public void SetQuote(InstrumentBidAskPair bidAskPair)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                if (bidAskPair == null)
                {
                    return;
                }

                _quotes[bidAskPair.Instrument] = bidAskPair;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public RemoveQuoteError RemoveQuote(string assetPairId)
        {
            #region Validation

            var positions = _ordersCache.Positions.GetPositionsByFxInstrument(assetPairId).ToList();
            if (positions.Any())
            {
                return RemoveQuoteError.Failure(
                    $"Cannot delete [{assetPairId}] best FX price because there are {positions.Count} opened positions.",
                    RemoveQuoteErrorCode.PositionsOpened);
            }

            var orders = _ordersCache.Active.GetOrdersByFxInstrument(assetPairId).ToList();
            if (orders.Any())
            {
                return RemoveQuoteError.Failure(
                    $"Cannot delete [{assetPairId}] best FX price because there are {orders.Count} active orders.",
                    RemoveQuoteErrorCode.OrdersOpened);
            }

            #endregion

            _lockSlim.EnterWriteLock();
            try
            {
                if (_quotes.ContainsKey(assetPairId))
                {
                    _quotes.Remove(assetPairId);
                    return RemoveQuoteError.Success();
                }

                return RemoveQuoteError.Failure(
                    string.Format(MtMessages.QuoteNotFound, assetPairId),
                    RemoveQuoteErrorCode.QuoteNotFound);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public override void Start()
        {
            _quotes =
                _blobRepository
                    .Read<Dictionary<string, InstrumentBidAskPair>>(LykkeConstants.StateBlobContainer, BlobName)
                    ?.ToDictionary(d => d.Key, d => d.Value) ??
                new Dictionary<string, InstrumentBidAskPair>();

            base.Start();
        }

        public override Task Execute()
        {
            return DumpToRepository();
        }

        public override void Stop()
        {
            if (Working)
            {
                DumpToRepository().Wait();
            }

            base.Stop();
        }

        private async Task DumpToRepository()
        {
            try
            {
                await _blobRepository.WriteAsync(LykkeConstants.StateBlobContainer, BlobName, GetAllQuotes());
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(
                    nameof(FxRateCacheService),
                    "Save fx rates",
                    "",
                    ex);
            }
        }
    }
}