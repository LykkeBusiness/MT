// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Services;
using StackExchange.Redis;

namespace MarginTrading.Backend.Services.Services
{
    public class SnapshotTrackerService : ISnapshotTrackerService
    {
        private readonly IDatabase _database;
        
        private const string RedisKey = "core:snapshot:should-recreate";
        
        public SnapshotTrackerService(IConnectionMultiplexer redis)
        {
            _database = redis.GetDatabase();
        }

        public async Task SetShouldRecreateSnapshot(bool value)
        {
            await _database
                .StringSetAsync(RedisKey, value.ToString(), when: When.Always);
        }
        
        public async Task<bool> GetShouldRecreateSnapshot()
        {
            var value = await _database
                .StringGetAsync(RedisKey);

            return value == bool.TrueString;
        }
    }
}