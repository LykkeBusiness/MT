// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using MarginTrading.Backend.Core.Quotes;

namespace MarginTrading.Backend.Core.Services
{
    public interface IFxRateCacheService
    {
        InstrumentBidAskPair GetQuote(string instrument);
        Dictionary<string, InstrumentBidAskPair> GetAllQuotes();
        void SetQuote(InstrumentBidAskPair bidAskPair);
        RemoveQuoteError RemoveQuote(string assetPairId);
    }
}