// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Services;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace MarginTrading.Backend.Services.Services
{
    public class SnapshotTrackerService : ISnapshotTrackerService
    {
        private readonly IDatabase _database;
        private readonly AsyncRetryPolicy _retryPolicy;
        
        private const string RedisKey = "core:snapshot:should-recreate";
        
        public SnapshotTrackerService(IConnectionMultiplexer redis, ILogger<SnapshotTrackerService> logger)
        {
            _database = redis.GetDatabase();
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3,
                    x => TimeSpan.FromMilliseconds(x * 1000),
                    (exception, span) => logger.LogWarning("Exception: {Message}", exception?.Message));
        }

        public async Task SetShouldRecreateSnapshot(bool value)
        {
            await _retryPolicy.ExecuteAsync(() => _database
                .StringSetAsync(RedisKey, value.ToString(), when: When.Always)); 
        }
        
        public async Task<bool> GetShouldRecreateSnapshot()
        {
            var value = await _retryPolicy.ExecuteAsync(() => 
              _database.StringGetAsync(RedisKey));

            return value == bool.TrueString;
        }
    }
}