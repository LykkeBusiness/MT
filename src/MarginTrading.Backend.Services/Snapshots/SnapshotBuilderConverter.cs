using System;
using System.Collections.Generic;
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
        if (IsMakingSnapshotInProgress)
        {
            throw new InvalidOperationException("Trading data snapshot manipulations are already in progress");
        }

        await Lock.WaitAsync();
        try
        {
            var snapshot = await _finalSnapshotCalculator.RunAsync(fxRates, cfdQuotes, correlationId, draftSnapshotKeeper);
            await _snapshotsRepository.AddAsync(snapshot);
        }
        finally
        {
            Lock.Release();
        }
    }
}