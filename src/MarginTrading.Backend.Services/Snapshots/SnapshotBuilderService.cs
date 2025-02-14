// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Autofac;

using Common.Log;

using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Snapshots;

public partial class SnapshotBuilderService : ISnapshotBuilderService
{
    private readonly IScheduleSettingsCacheService _scheduleSettingsCacheService;
    private readonly IDateService _dateService;
    private readonly ITradingEngineRawSnapshotsRepository _rawSnapshotsRepository;
    private readonly ITradingEngineSnapshotsRepository _snapshotsRepository;
    private readonly IQueueValidationService _queueValidationService;
    private readonly ILog _log;
    private readonly IFinalSnapshotCalculator _finalSnapshotCalculator;
    private readonly ITradingEngineSnapshotBuilder _snapshotBuilder;
    private readonly IComponentContext _context;
    private readonly ISnapshotBuilderDraftRebuildAgent _snapshotDraftRebuildAgent;
    private static readonly SemaphoreSlim Lock = new(1, 1);
    public static bool IsMakingSnapshotInProgress => Lock.CurrentCount == 0;

    public SnapshotBuilderService(
        IScheduleSettingsCacheService scheduleSettingsCacheService,
        IQueueValidationService queueValidationService,
        IDateService dateService,
        ITradingEngineRawSnapshotsRepository snashotsRepository,
        IFinalSnapshotCalculator finalSnapshotCalculator,
        ITradingEngineSnapshotBuilder snapshotBuilder,
        ITradingEngineSnapshotsRepository repository,
        IComponentContext context,
        ISnapshotBuilderDraftRebuildAgent snapshotDraftRebuildAgent,
        ILog log)
    {
        _scheduleSettingsCacheService = scheduleSettingsCacheService;
        _dateService = dateService;
        _rawSnapshotsRepository = snashotsRepository;
        _queueValidationService = queueValidationService;
        _finalSnapshotCalculator = finalSnapshotCalculator;
        _snapshotBuilder = snapshotBuilder;
        _snapshotsRepository = repository;
        _context = context;
        _snapshotDraftRebuildAgent = snapshotDraftRebuildAgent;
        _log = log;
    }

    private void CheckPreconditionsOrThrow(DateTime tradingDay)
    {
        if (!_scheduleSettingsCacheService.TryGetPlatformCurrentDisabledInterval(out var disabledInterval))
        {
            //TODO: remove later (if everything will work and we will never go to this branch)
            _scheduleSettingsCacheService.MarketsCacheWarmUp();

            if (!_scheduleSettingsCacheService.TryGetPlatformCurrentDisabledInterval(out disabledInterval))
            {
                throw new Exception($"Trading should be stopped for whole platform in order to make trading data snapshot.");
            }
        }

        if (disabledInterval.Start.AddDays(-1) > tradingDay.Date || disabledInterval.End < tradingDay.Date)
        {
            throw new Exception(
                $"{nameof(tradingDay)}'s Date component must be from current disabled interval's Start -1d to End: [{disabledInterval.Start.AddDays(-1)}, {disabledInterval.End}].");
        }

        if (IsMakingSnapshotInProgress)
        {
            throw new InvalidOperationException("Trading data snapshot creation is already in progress");
        }

        // We must be sure all messages have been processed by history brokers before starting current state validation.
        // If one or more queues contain not delivered messages the snapshot can not be created.
        _queueValidationService.ThrowExceptionIfQueuesNotEmpty(true);
    }

    /// <inheritdoc />
    public async Task<TradingEngineSnapshotSummary> MakeSnapshot(
        DateTime tradingDay,
        string correlationId,
        EnvironmentValidationStrategyType strategyType,
        SnapshotStatus status = SnapshotStatus.Final)
    {
        CheckPreconditionsOrThrow(tradingDay);

        await Lock.WaitAsync();
        try
        {
            var envValidationResult = await ValidateEnvironment(strategyType, correlationId);

            var snapshot = await _snapshotBuilder
                .CollectDataFrom(envValidationResult.Cache)
                .WithTradingDay(tradingDay)
                .WithCorrelationId(correlationId)
                .WithStatus(status)
                .WithTimestamp(_dateService.Now())
                .Build();

            await _rawSnapshotsRepository.AddAsync(snapshot);

            if (status == SnapshotStatus.Draft)
                await _snapshotDraftRebuildAgent.ResetDraftRebuildFlag();

            return snapshot.Summary;
        }
        finally
        {
            Lock.Release();
        }
    }

    private Task<EnvironmentValidationResult> ValidateEnvironment(
        EnvironmentValidationStrategyType strategyType,
        string correlationId) =>
        _context.ResolveKeyed<IEnvironmentValidationStrategy>(strategyType).Validate(correlationId);
}
