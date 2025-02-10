// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Snapshots;

using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services;

public class TradingEngineRawSnapshotsLoggingAdapter(
    ITradingEngineRawSnapshotsAdapter decoratee,
    ILogger<TradingEngineRawSnapshotsLoggingAdapter> logger) : ITradingEngineRawSnapshotsAdapter
{
    private readonly ITradingEngineRawSnapshotsAdapter _decoratee = decoratee;
    private readonly ILogger<TradingEngineRawSnapshotsLoggingAdapter> _logger = logger;

    public async Task AddAsync(TradingEngineSnapshotRaw tradingEngineSnapshot)
    {
        var statistics = tradingEngineSnapshot.GetStatistics();

        _logger.LogInformation("Starting to write trading data snapshot. {Statictics}", statistics);

        await _decoratee.AddAsync(tradingEngineSnapshot);

        _logger.LogInformation("Trading data snapshot was written to the storage. {Statictics}", statistics);
    }
}
