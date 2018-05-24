﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Common.Extensions;

namespace MarginTrading.Backend.Services.Quotes
{
    public class FxRateCacheService : IFxRateCacheService
    {
        private readonly ILog _log;
        private readonly Dictionary<string, InstrumentBidAskPair> _quotes;
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public FxRateCacheService(ILog log)
        {
            _log = log;
            _quotes = new Dictionary<string, InstrumentBidAskPair>();
        }

        public InstrumentBidAskPair GetQuote(string instrument)
        {
            _lockSlim.EnterReadLock();
            try
            {
                if (!_quotes.TryGetValue(instrument, out var quote))
                    throw new QuoteNotFoundException(instrument, string.Format(MtMessages.QuoteNotFound, instrument));

                return quote;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public bool TryGetQuoteById(string instrument, out InstrumentBidAskPair result)
        {
            _lockSlim.EnterReadLock();
            try
            {
                if (!_quotes.TryGetValue(instrument, out var quote))
                {
                    result = null;
                    return false;
                }

                result = quote;
                return true;
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

        public void RemoveQuote(string assetPair)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                if (_quotes.ContainsKey(assetPair))
                    _quotes.Remove(assetPair);
                else
                    throw new QuoteNotFoundException(assetPair, string.Format(MtMessages.QuoteNotFound, assetPair));
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public Task SetQuote(PumpQuoteMessage quote)
        {
            var bidAskPair = CreatePair(quote);
            SetQuote(bidAskPair);
            
            return Task.CompletedTask;
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

                if (_quotes.ContainsKey(bidAskPair.Instrument))
                {
                    _quotes[bidAskPair.Instrument] = bidAskPair;
                }
                else
                {
                    _quotes.Add(bidAskPair.Instrument, bidAskPair);
                }
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }
        
        private InstrumentBidAskPair CreatePair(PumpQuoteMessage message)
        {
            if (!ValidateOrderbook(message))
            {
                return null;
            }
            
            var ask = GetBestPrice(true, message.Asks);
            var bid = GetBestPrice(false, message.Bids);

            return ask == null || bid == null
                ? null
                : new InstrumentBidAskPair
                {
                    Instrument = message.AssetPairId,
                    Date = message.Timestamp,
                    Ask = ask.Value,
                    Bid = bid.Value
                };
        }
        
        private decimal? GetBestPrice(bool isBuy, IReadOnlyCollection<PumpVolumePriceMessage> prices)
        {
            if (!prices.Any())
                return null;
            return isBuy
                ? prices.Min(x => x.Price)
                : prices.Max(x => x.Price);
        }
        
        private bool ValidateOrderbook(PumpQuoteMessage orderbook)
        {
            try
            {
                orderbook.AssetPairId.RequiredNotNullOrWhiteSpace("orderbook.AssetPairId");
                orderbook.ExchangeName.RequiredNotNullOrWhiteSpace("orderbook.ExchangeName");
                orderbook.RequiredNotNull(nameof(orderbook));
                
                orderbook.Bids.RequiredNotNullOrEmpty("orderbook.Bids");
                orderbook.Bids.RemoveAll(e => e == null || e.Price <= 0 || e.Volume == 0);
                //ValidatePricesSorted(orderbook.Bids, false);
                
                orderbook.Asks.RequiredNotNullOrEmpty("orderbook.Asks");
                orderbook.Asks.RemoveAll(e => e == null || e.Price <= 0 || e.Volume == 0);
                //ValidatePricesSorted(orderbook.Asks, true);

                return true;
            }
            catch (Exception e)
            {
                _log.WriteError(nameof(PumpQuoteMessage), orderbook.ToJson(), e);
                return false;
            }
        }
    }
}