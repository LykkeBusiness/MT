// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Snapshots;

using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services.Snapshot;

public class LoggingTradingEngineRawSnapshotsRepository(
    ITradingEngineRawSnapshotsRepository decoratee,
    ILogger<LoggingTradingEngineRawSnapshotsRepository> logger) : ITradingEngineRawSnapshotsRepository
{
    private readonly ITradingEngineRawSnapshotsRepository _decoratee = decoratee;
    private readonly ILogger<LoggingTradingEngineRawSnapshotsRepository> _logger = logger;

    public async Task AddAsync(TradingEngineSnapshotRaw tradingEngineRawSnapshot)
    {
        var summary = tradingEngineRawSnapshot.Summary;

        _logger.LogInformation("Starting to write trading data snapshot. {Summary}", summary);

        await _decoratee.AddAsync(tradingEngineRawSnapshot);

        _logger.LogInformation("Trading data snapshot was written to the storage. {Summary}", summary);
    }
}
