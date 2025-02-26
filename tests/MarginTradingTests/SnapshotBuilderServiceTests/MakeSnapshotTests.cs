using System;

using Autofac;
using Autofac.Extras.Moq;

using FsCheck;

using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Snapshots;

using Microsoft.Extensions.Logging.Abstractions;

using NUnit.Framework;

namespace MarginTradingTests.SnapshotBuilderServiceTests;

[TestFixture]
public class MakeSnapshotTests
{
    static Action<ContainerBuilder> ConfigureContainer(DateTime tradingDay) => builder =>
    {
        builder
            .RegisterInstance(new FakeScheduleSettingsCacheService(tradingDay))
            .As<IScheduleSettingsCacheService>();

        builder
            .RegisterInstance(new AsSoonAsPossibleStrategy(new FailingEnvironmentValidator()))
            .Keyed<IEnvironmentValidationStrategy>(EnvironmentValidationStrategyType.AsSoonAsPossible);

        builder
            .RegisterInstance(new PreferConsistencyStrategy(
                new FailingEnvironmentValidator(),
                NullLogger<PreferConsistencyStrategy>.Instance,
                TimeSpan.FromMilliseconds(100)))
            .Keyed<IEnvironmentValidationStrategy>(EnvironmentValidationStrategyType.WaitPlatformConsistency);

        builder
            .RegisterInstance(new EmptySnapshotBuilder())
            .As<ITradingEngineSnapshotBuilder>();
    };

    [Test]
    public void MakeSnapshot_Always_EndsUp_WithSummary()
    {
        Prop.ForAll((
            from strategy in Gen.Elements(EnvironmentValidationStrategyType.AsSoonAsPossible, EnvironmentValidationStrategyType.WaitPlatformConsistency)
            from status in Gen.Elements(SnapshotStatus.Draft, SnapshotStatus.Final)
            from initiator in Gen.Elements(SnapshotInitiator.ServiceApi, SnapshotInitiator.EodProcess, SnapshotInitiator.PlatformClosureEvent)
            from tradingDay in Arb.Default.DateTime().Generator
            from correlationId in Arb.Default.String().Generator
            select (strategy, status, initiator, tradingDay, correlationId)).ToArbitrary(),
            t =>
            {
                using var mock = AutoMock.GetLoose(ConfigureContainer(t.tradingDay));
                var sut = mock.Create<SnapshotBuilderService>();
                var summary = sut.MakeSnapshot(t.tradingDay, t.correlationId, t.strategy, t.initiator, t.status).GetAwaiter().GetResult();
                return summary is not null;
            }).QuickCheckThrowOnFailure();
    }
}