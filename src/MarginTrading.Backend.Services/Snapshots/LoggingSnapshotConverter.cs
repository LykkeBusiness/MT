// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Snapshots;

using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services.Snapshots;

internal sealed class LoggingSnapshotConverter(
    ISnapshotConverter decoratee,
    ILogger<LoggingSnapshotConverter> logger) : ISnapshotConverter
{
    public async Task ConvertToFinal(
        string correlationId,
        IEnumerable<ClosingAssetPrice> cfdQuotes,
        IEnumerable<ClosingFxRate> fxRates,
        IDraftSnapshotKeeper draftSnapshotKeeper = null)
    {
        await decoratee.ConvertToFinal(
            correlationId,
            cfdQuotes,
            fxRates,
            draftSnapshotKeeper);

        logger.LogInformation(
            "Snapshot was converted to final. CorrelationId: {CorrelationId}",
            correlationId);
    }
}
