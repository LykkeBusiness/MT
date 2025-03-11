using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Snapshots;

public partial class SnapshotService
{
    /// <inheritdoc />
    public async Task ConvertToFinal(
        string correlationId,
        IEnumerable<ClosingAssetPrice> cfdQuotes,
        IEnumerable<ClosingFxRate> fxRates,
        IDraftSnapshotKeeper draftSnapshotKeeper = null)
    {
        if (Interlocked.CompareExchange(ref _isSnaphotInProgress, 1, 0) == 1)
        {
            throw new InvalidOperationException("Trading data snapshot manipulations are already in progress");
        }

        try
        {
            var snapshot = await _finalSnapshotCalculator.RunAsync(fxRates, cfdQuotes, correlationId, draftSnapshotKeeper);
            await _snapshotsRepository.AddAsync(snapshot);
        }
        finally
        {
            Interlocked.Exchange(ref _isSnaphotInProgress, 0);
        }
    }
}