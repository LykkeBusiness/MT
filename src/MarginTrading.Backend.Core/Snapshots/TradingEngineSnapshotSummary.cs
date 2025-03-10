// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Snapshots;

public record TradingEngineSnapshotSummary(
    DateTime TradingDay,
    int OrdersCount,
    int PositionsCount,
    int AccountsCount,
    int BestFxPricesCount,
    int BestTradingPricesCount)
{
    public static TradingEngineSnapshotSummary Empty => new(
        default, 0, 0, 0, 0, 0);

    public override string ToString() =>
        $"TradingDay: {TradingDay:yyyy-MM-dd}, Orders: {OrdersCount}, positions: {PositionsCount}, accounts: {AccountsCount}, best FX prices: {BestFxPricesCount}, best trading prices: {BestTradingPricesCount}.";

    public static implicit operator string(TradingEngineSnapshotSummary summary) => summary.ToString();
}
