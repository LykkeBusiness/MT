using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MarginTrading.Backend.Contracts.Prices;

namespace MarginTrading.Backend.Services.Snapshot;

public partial class SnapshotBuilderService : ISnapshotBuilderService
{
    /// <inheritdoc />
    public async Task Convert(
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
            await _repository.AddAsync(snapshot);
        }
        finally
        {
            Lock.Release();
        }
    }
}