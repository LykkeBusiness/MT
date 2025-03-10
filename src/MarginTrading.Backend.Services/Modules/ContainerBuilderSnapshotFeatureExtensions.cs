// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Snapshots;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services.Modules;

internal static class ContainerBuilderSnapshotFeatureExtensions
{
    public static ContainerBuilder RegisterSnapshotRequestServices(this ContainerBuilder builder)
    {
        builder.RegisterType<InMemoryRequestQueue<SnapshotCreationRequest>>()
            .AsSelf()
            .SingleInstance();
        // add logging decorator
        builder.Register(c =>
            new LoggingSnapshotRequestQueue(
                c.Resolve<InMemoryRequestQueue<SnapshotCreationRequest>>(),
                c.Resolve<ILogger<LoggingSnapshotRequestQueue>>()))
            .As<IRequestQueue<SnapshotCreationRequest>>()
            .As<IQueueRequestProducer<SnapshotCreationRequest>>()
            .As<IQueueRequestConsumer<SnapshotCreationRequest>>()
            .SingleInstance();
        // add waitable request queue adapter
        builder.Register(c =>
            new WaitableQueueAdapter<SnapshotCreationRequest, TradingEngineSnapshotSummary>(
                c.Resolve<IRequestQueue<SnapshotCreationRequest>>()))
            .As<IWaitableRequestQueue<SnapshotCreationRequest, TradingEngineSnapshotSummary>>()
            .As<IWaitableRequestProducer<SnapshotCreationRequest, TradingEngineSnapshotSummary>>()
            .As<IWaitableRequestConsumer<SnapshotCreationRequest, TradingEngineSnapshotSummary>>()
            .SingleInstance();
        // add SnapshotRequestsMonitor as hosted service
        builder.RegisterType<SnapshotRequestsMonitor>()
            .As<IHostedService>()
            .SingleInstance();

        return builder;
    }

    public static ContainerBuilder RegisterSnapshotBuilderServices(this ContainerBuilder builder, bool logBlockedMarginCalculation)
    {
        // register TradingEngineSnapshotBuilder decorators
        builder.RegisterType<TradingEngineSnapshotBuilder>()
            .SingleInstance();
        builder.Register(ctx => new AccountUpdatingTradingEngineSnapshotBuilder(
            ctx.Resolve<TradingEngineSnapshotBuilder>()))
            .SingleInstance();
        builder.Register(ctx => new LoggingTradingEngineSnapshotBuilder(
                ctx.Resolve<AccountUpdatingTradingEngineSnapshotBuilder>(),
                logBlockedMarginCalculation,
                ctx.Resolve<ILogger<LoggingTradingEngineSnapshotBuilder>>()))
            .As<ITradingEngineSnapshotBuilder>()
            .SingleInstance();

        builder.RegisterType<SnapshotService>()
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
        builder.RegisterDecorator<SnapshotServiceDecorator, ISnapshotService>();
        builder.RegisterDecorator<LoggingSnapshotService, ISnapshotService>();
        builder.RegisterDecorator<LoggingSnapshotConverter, ISnapshotConverter>();

        builder.RegisterType<SnapshotValidator>()
            .As<ISnapshotValidator>()
            .SingleInstance();

        builder.RegisterType<EnvironmentValidator>()
            .As<IEnvironmentValidator>()
            .SingleInstance();

        // register decorated AsSoonAsPossibleStrategy 
        builder.RegisterType<AsSoonAsPossibleStrategy>();
        builder.Register(ctx => new LoggingEnvironmentValidationStrategy(
                ctx.Resolve<ILogger<LoggingEnvironmentValidationStrategy>>(),
                ctx.Resolve<AsSoonAsPossibleStrategy>()))
            .Keyed<IEnvironmentValidationStrategy>(EnvironmentValidationStrategyType.AsSoonAsPossible);

        // register decorated PreferConsistencyStrategy
        builder.RegisterType<PreferConsistencyStrategy>();
        builder.Register(ctx => new LoggingEnvironmentValidationStrategy(
                ctx.Resolve<ILogger<LoggingEnvironmentValidationStrategy>>(),
                ctx.Resolve<PreferConsistencyStrategy>()))
            .Keyed<IEnvironmentValidationStrategy>(EnvironmentValidationStrategyType.WaitPlatformConsistency);

        return builder;
    }

    public static ContainerBuilder RegisterSnapshotRecalculationServices(this ContainerBuilder builder)
    {
        builder.RegisterType<FinalSnapshotCalculator>()
                .As<IFinalSnapshotCalculator>()
                .InstancePerLifetimeScope();

        // @atarutin: DraftSnapshotKeeper implements IOrderReader interface for convenient access to positions
        // and orders but it is not required to be used for registration in DI container
        builder.RegisterType<DraftSnapshotKeeper>()
            .As<IDraftSnapshotKeeper>()
            .InstancePerLifetimeScope();

        builder.RegisterType<DraftSnapshotKeeperFactory>()
            .As<IDraftSnapshotKeeperFactory>()
            .SingleInstance();

        builder.RegisterType<FakeSnapshotService>()
            .As<IFakeSnapshotService>()
            .SingleInstance();

        return builder;
    }
}