// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.Quotes
{
    public enum RemoveQuoteErrorCode
    {
        None,
        QuoteNotFound,
        PositionsOpened,
        OrdersOpened
    }
}