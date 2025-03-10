// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

using MarginTrading.Backend.Contracts.Prices;

namespace MarginTrading.Backend.Core.Snapshots;

public interface ISnapshotConverter
{
    /// <summary>
    /// Make final trading snapshot by converting draft snapshot
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="cfdQuotes"></param>
    /// <param name="fxRates"></param>
    /// <param name="draftSnapshotKeeper"></param>
    /// <returns></returns>
    // so far the only reason features are combined under the same service is that they are sharing lock
    Task ConvertToFinal(
        string correlationId,
        IEnumerable<ClosingAssetPrice> cfdQuotes,
        IEnumerable<ClosingFxRate> fxRates,
        IDraftSnapshotKeeper draftSnapshotKeeper = null);
}
