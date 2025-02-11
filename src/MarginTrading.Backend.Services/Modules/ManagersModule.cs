// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;

using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Caches;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Backend.Services.Quotes;
using MarginTrading.Backend.Services.Snapshot;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services.Modules
{
    public class ManagersModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AccountManager>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<OrderCacheManager>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<TradingInstrumentsManager>()
                .AsSelf()
                .As<ITradingInstrumentsManager>()
                .SingleInstance();

            builder.RegisterType<MatchingEngineRoutesManager>()
                .AsSelf()
                .As<IStartable>()
                .As<IMatchingEngineRoutesManager>()
                .SingleInstance();

            builder.RegisterType<AssetPairsManager>()
                .AsSelf()
                .As<IStartable>()
                .As<IAssetPairsManager>()
                .SingleInstance();

            builder.RegisterType<PendingOrdersCleaningService>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<QuotesMonitor>()
                .AsSelf()
                .As<IStartable>()
                .SingleInstance();

            builder.RegisterType<TradingEngineSnapshotBuilder>()
                .As<ITradingEngineSnapshotBuilder>()
                .SingleInstance();
            builder.RegisterDecorator<AccountUpdatingTradingEngineSnapshotBuilder, ITradingEngineSnapshotBuilder>();
            builder.RegisterDecorator<TradingEngineSnapshotLoggingBuilder, ITradingEngineSnapshotBuilder>();

            builder.RegisterType<SnapshotBuilderService>()
                .As<ISnapshotBuilderService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<SnapshotValidationService>()
                .As<ISnapshotValidationService>()
                .SingleInstance();

            builder.RegisterType<EnvironmentValidator>()
                .As<IEnvironmentValidator>()
                .SingleInstance();

            builder.RegisterType<DraftSnapshotWorkflowTracker>()
                .As<IDraftSnapshotWorkflowTracker>()
                .SingleInstance();
            builder.RegisterDecorator<SynchronizedSnapshotWorkflowTracker, IDraftSnapshotWorkflowTracker>();
        }
    }
}
