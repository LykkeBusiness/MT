// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.AccountHistory;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface IAccountHistoryRepository
    {
        Task<Dictionary<string, decimal>> GetSwapTotalPerPosition(IEnumerable<string> positionIds);
        
        Task<IReadOnlyCollection<PositionChangeAmountEntry>> GetUnrealizedPnlPerPosition(DateTime tradingDay);
    }
}