// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Services.Mappers;

namespace MarginTrading.Backend.Services.Snapshots;

public class TradingEngineRawSnapshotsRepository(
    ITradingEngineSnapshotsRepository tradingEngineSnapshotsRepository) : ITradingEngineRawSnapshotsRepository
{
    public Task AddAsync(TradingEngineSnapshotRaw tradingEngineRawSnapshot) =>
        tradingEngineSnapshotsRepository.AddAsync(tradingEngineRawSnapshot.Convert());
}