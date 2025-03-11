// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core.Repositories;

namespace MarginTrading.Backend.Services
{
    public class DraftSnapshotKeeperFactory : IDraftSnapshotKeeperFactory
    {
        private readonly ITradingEngineSnapshotsRepository _tradingEngineSnapshotsRepository;

        public DraftSnapshotKeeperFactory(ITradingEngineSnapshotsRepository tradingEngineSnapshotsRepository)
        {
            _tradingEngineSnapshotsRepository = tradingEngineSnapshotsRepository;
        }
        
        public IDraftSnapshotKeeper Create(DateTime tradingDay)
        {
            var draftSnapshotKeeper = new DraftSnapshotKeeper(_tradingEngineSnapshotsRepository);
            draftSnapshotKeeper.Init(tradingDay);
            return draftSnapshotKeeper;
        }
    }
}