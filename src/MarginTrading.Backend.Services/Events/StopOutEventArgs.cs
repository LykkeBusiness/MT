// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Events
{
    public class StopOutEventArgs
    {
        public StopOutEventArgs(MarginTradingAccount account, string correlationId = null)
        {
            if (account == null) throw new ArgumentNullException(nameof(account));
            Account = account;
            CorrelationId = correlationId;
        }

        public MarginTradingAccount Account { get; }
        public string CorrelationId { get; }
    }
}