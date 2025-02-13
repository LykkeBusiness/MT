// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Services.Mappers;

namespace MarginTrading.Backend.Services.Snapshot;

public class TradingEngineRawSnapshotsRepository(
    ITradingEngineSnapshotsRepository tradingEngineSnapshotsRepository) : ITradingEngineRawSnapshotsRepository
{
    private readonly ITradingEngineSnapshotsRepository _tradingEngineSnapshotsRepository = tradingEngineSnapshotsRepository;

    public Task AddAsync(TradingEngineSnapshotRaw tradingEngineRawSnapshot) =>
        _tradingEngineSnapshotsRepository.AddAsync(tradingEngineRawSnapshot.ToSnapshot());
}