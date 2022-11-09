// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Lykke.Snow.Common;
using Lykke.Snow.Common.Model;
using MarginTrading.Backend.Core.AccountHistory;
using MarginTrading.Backend.Core.Repositories;
using Microsoft.Data.SqlClient;

namespace MarginTrading.SqlRepositories.Repositories
{
    public class AccountHistoryRepository : SqlRepositoryBase, IAccountHistoryRepository
    {
        private readonly StoredProcedure _getSwapTotalPerPosition =
            new StoredProcedure("getSwapTotalPerPosition", "dbo");

        public AccountHistoryRepository(string connectionString) : base(connectionString)
        {
            ExecCreateOrAlter(_getSwapTotalPerPosition.FileName);
        }
        
        public async Task<Dictionary<string, decimal>> GetSwapTotalPerPosition(IEnumerable<string> positionIds)
        {
            if (!positionIds.Any())
            {
                return new Dictionary<string, decimal>();
            }

            var positionIdCollection = new PositionIdCollection();
            positionIdCollection.AddRange(positionIds.Select(id => new PositionId {Id = id}));

            var items = await GetAllAsync(
                _getSwapTotalPerPosition.FullyQualifiedName,
                new[]
                {
                    new SqlParameter
                    {
                        ParameterName = "@positions",
                        SqlDbType = SqlDbType.Structured,
                        TypeName = "dbo.PositionListDataType",
                        Value = positionIdCollection
                    }
                }, reader => new
                {
                    PositionId = reader["PositionId"] as string,
                    SwapTotal = Convert.ToDecimal(reader["SwapTotal"])
                });
            return items.ToDictionary(x => x.PositionId, x => x.SwapTotal);
        }

        public async Task<IReadOnlyCollection<PositionChangeAmountEntry>> GetUnrealizedPnlPerPosition(DateTime tradingDay)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                var entries = await conn.QueryAsync<PositionChangeAmountEntry>(@"
select
    Oid,
    ChangeAmount,
    EventSourceId as PositionId,
    Id as OperationId
from dbo.AccountHistory
where ReasonType = 'UnrealizedDailyPnL'
  and TradingDate is not null
  and datepart(year, TradingDate) = @Year
  and datepart(month, TradingDate) = @Month
  and datepart(day, TradingDate) = @Day;
", new { Year = tradingDay.Year, Month = tradingDay.Month, Day = tradingDay.Day });

                return entries.ToList();
            }
        }
    }
}