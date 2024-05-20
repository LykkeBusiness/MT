// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Services.Services;
using StackExchange.Redis;

namespace MarginTrading.Backend.Services
{
    internal class RunningLiquidationRepository : IRunningLiquidationRepository
    {
        private readonly IDatabase _database;
        
        private const string RedisKeyFmt = "core:account:{0}:current-liquidation";

        public RunningLiquidationRepository(IConnectionMultiplexer redis)
        {
            _database = redis.GetDatabase();
        }
        
        public Task<bool> TryAdd(string accountId, RunningLiquidation runningLiquidation)
        {
            var serialized = ProtoBufSerializer.Serialize(runningLiquidation);
            var key = GetKey(accountId);
            return _database.StringSetAsync(key, serialized, when: When.NotExists);
        }

        public Task<bool> TryRemove(string accountId)
        {
            var key = GetKey(accountId);
            return _database.KeyDeleteAsync(key);
        }

        public async IAsyncEnumerable<RunningLiquidation> Get(string[] accountIds)
        {
            var keys = accountIds.Select(GetKey);
            var results = await _database.StringGetAsync(keys.ToArray());

            foreach (var redisValue in results)
            {
                if (redisValue.HasValue)
                    yield return ProtoBufSerializer.Deserialize<RunningLiquidation>(redisValue);
            }
        }

        private static RedisKey GetKey(string accountId) => string.Format(RedisKeyFmt, accountId);
    }
}