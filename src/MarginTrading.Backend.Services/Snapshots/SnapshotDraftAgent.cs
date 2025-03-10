// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using MarginTrading.Backend.Contracts.Activities;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.Services;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

using StackExchange.Redis;

namespace MarginTrading.Backend.Services.Snapshots;

/// <summary>
/// Snapshot draft agent to keep track of position history changes.
/// It is important concern during EOD process.
/// Also implements cross cutting concern of persisting snapshot rebuild flag to a Redis cache.
/// Default value of snapshot rebuild flag is false.
/// </summary>
/// <param name="decoratee"></param>
public class SnapshotDraftAgent(
    IPositionHistoryHandler decoratee,
    IConnectionMultiplexer redis,
    ILogger<SnapshotDraftAgent> logger) : ISnapshotDraftAgent, IPositionHistoryHandler
{
    private readonly IPositionHistoryHandler _decoratee = decoratee;
    private readonly IDatabase _database = redis.GetDatabase();
    private readonly AsyncRetryPolicy _retryPolicy =
        Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3,
                x => TimeSpan.FromMilliseconds(x * 1000),
                    (exception, _) => logger.LogWarning("Exception: {Message}", exception?.Message));
    private const string RedisKey = "core:snapshot:should-recreate";
    private bool? _draftSnapshotRebuildRequired;
    private const bool DefaultSnapshotRebuildRequired = false;

    public async Task HandleClosePosition(Position position, DealContract deal, string additionalInfo)
    {
        await _decoratee.HandleClosePosition(position, deal, additionalInfo);
        _draftSnapshotRebuildRequired = await Save(true);
    }

    public Task HandleOpenPosition(Position position, string additionalInfo, PositionOpenMetadata metadata) =>
        _decoratee.HandleOpenPosition(position, additionalInfo, metadata);

    public async Task HandlePartialClosePosition(Position position, DealContract deal, string additionalInfo)
    {
        await _decoratee.HandlePartialClosePosition(position, deal, additionalInfo);
        _draftSnapshotRebuildRequired = await Save(true);
    }

    public async ValueTask<bool> IsDraftRebuildRequired() =>
        _draftSnapshotRebuildRequired ?? await Read() ?? DefaultSnapshotRebuildRequired;

    public async Task ResetDraftRebuildFlag()
    {
        _draftSnapshotRebuildRequired = await Save(false);
    }

    private async Task<bool> Save(bool value)
    {
        var redisUpdated = await _retryPolicy.ExecuteAsync(
            () => _database.StringSetAsync(RedisKey, value.ToString(), when: When.Always));

        WriteLog(redisUpdated, value);

        return value;
    }

    private async Task<bool?> Read()
    {
        var value = await _retryPolicy.ExecuteAsync(() => _database.StringGetAsync(RedisKey));

        return value == RedisValue.Null ? null : value == bool.TrueString;
    }

    private void WriteLog(bool redisUpdated, bool newValue)
    {
        if (redisUpdated)
        {
            logger.LogInformation("Snapshot rebuild flag successfully updated in Redis (new value: {Value}). Disregard earlier error messages if any.", newValue);
            return;
        }

        if (newValue)
        {
            logger.LogError("Failed to update snapshot rebuild flag in Redis (new value: {Value}). Consider making trading snapshot draft manually after service restart.", true);
            return;
        }

        logger.LogWarning("Failed to update snapshot rebuild flag in Redis (new value: {Value}).", false);
    }
}
