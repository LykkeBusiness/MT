// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Common;
using Common.Log;

using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Policies;
using MarginTrading.Common.Services;

using Polly.Retry;

namespace MarginTrading.Backend.Services.Snapshot;

public partial class SnapshotBuilderService : ISnapshotBuilderService
{
    private readonly IScheduleSettingsCacheService _scheduleSettingsCacheService;
    private readonly IDateService _dateService;
    private readonly ITradingEngineRawSnapshotsAdapter _snapshotsRepositoryAdapter;
    private readonly IQueueValidationService _queueValidationService;
    private readonly ILog _log;
    private readonly IFinalSnapshotCalculator _finalSnapshotCalculator;
    private readonly ISnapshotRecreateFlagKeeper _snapshotRecreateFlagKeeper;
    private readonly AsyncRetryPolicy<EnvironmentValidationResult> _policy;
    private readonly ITradingEngineSnapshotBuilder _snapshotBuilder;
    private readonly ITradingEngineSnapshotsRepository _repository;
    private static readonly SemaphoreSlim Lock = new(1, 1);
    public static bool IsMakingSnapshotInProgress => Lock.CurrentCount == 0;
    private readonly IEnvironmentValidator _snapshotValidator;

    public SnapshotBuilderService(
        IScheduleSettingsCacheService scheduleSettingsCacheService,
        IQueueValidationService queueValidationService,
        IDateService dateService,
        ITradingEngineRawSnapshotsAdapter snashotsRepositoryAdapter,
        IFinalSnapshotCalculator finalSnapshotCalculator,
        ISnapshotRecreateFlagKeeper snapshotRecreateFlagKeeper,
        ITradingEngineSnapshotBuilder snapshotBuilder,
        ITradingEngineSnapshotsRepository repository,
        IEnvironmentValidator snapshotValidator,
        ILog log)
    {
        _scheduleSettingsCacheService = scheduleSettingsCacheService;
        _dateService = dateService;
        _snapshotsRepositoryAdapter = snashotsRepositoryAdapter;
        _queueValidationService = queueValidationService;
        _log = log;
        _finalSnapshotCalculator = finalSnapshotCalculator;
        _snapshotRecreateFlagKeeper = snapshotRecreateFlagKeeper;
        _policy = SnapshotStateValidationPolicy.BuildPolicy(log);
        _snapshotBuilder = snapshotBuilder;
        _repository = repository;
        _snapshotValidator = snapshotValidator;
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
    public async Task<string> MakeTradingDataSnapshot(DateTime tradingDay, string correlationId, SnapshotStatus status = SnapshotStatus.Final)
    {
        CheckPreconditionsOrThrow(tradingDay);

        await Lock.WaitAsync();
        try
        {
            var validationResult = await _policy.ExecuteAsync(() => _snapshotValidator.Validate(correlationId));
            if (!validationResult.IsValid)
            {
                await _log.WriteFatalErrorAsync(nameof(SnapshotBuilderService),
                    nameof(MakeTradingDataSnapshot),
                    validationResult.ToJson(),
                    validationResult.Exception);
                throw validationResult.Exception;
            }

            var snapshot = await _snapshotBuilder
                .CollectDataFrom(validationResult.Cache)
                .WithTradingDay(tradingDay)
                .WithCorrelationId(correlationId)
                .WithStatus(status)
                .WithTimestamp(_dateService.Now())
                .Build();

            await _snapshotsRepositoryAdapter.AddAsync(snapshot);

            if (status == SnapshotStatus.Draft)
                await _snapshotRecreateFlagKeeper.Set(false);

            return $"Trading data snapshot was written to the storage. {snapshot.GetStatistics()}";
        }
        finally
        {
            Lock.Release();
        }
    }
}
