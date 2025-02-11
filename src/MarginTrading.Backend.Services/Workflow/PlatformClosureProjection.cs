// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using Common;
using Common.Log;

using JetBrains.Annotations;

using MarginTrading.Backend.Contracts.TradingSchedule;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Services.Extensions;
using MarginTrading.Backend.Services.Snapshot;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow
{
    public class PlatformClosureProjection
    {
        private readonly ISnapshotBuilderService _snapshotService;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly ILog _log;
        private readonly IDateService _dateService;

        public PlatformClosureProjection(ISnapshotBuilderService snapshotService,
            ILog log,
            IIdentityGenerator identityGenerator,
            IDateService dateService)
        {
            _snapshotService = snapshotService;
            _log = log;
            _identityGenerator = identityGenerator;
            _dateService = dateService;
        }

        [UsedImplicitly]
        public async Task Handle(MarketStateChangedEvent e)
        {
            if (e.IsNotPlatformClosureEvent())
                return;

            try
            {
                var successMessage = await CreateDraftSnapshot(e.EventTimestamp.Date);
                await LogIfSucceeded(successMessage, e);
            }
            catch (Exception ex)
            {
                var exceptionExpected = await IsExceptionExpected(ex, e);
                if (!exceptionExpected)
                {
                    throw;
                }
            }
        }

        private async Task LogIfSucceeded(string successMessage, MarketStateChangedEvent evt)
        {
            var failed = string.IsNullOrWhiteSpace(successMessage);
            if (failed) return;

            await _log.WriteInfoAsync(nameof(PlatformClosureProjection),
                nameof(LogIfSucceeded),
                evt.ToJson(),
                successMessage);
        }

        private async Task<string> CreateDraftSnapshot(DateTime tradingDay)
        {
            var result = await _snapshotService.MakeTradingDataSnapshot(tradingDay,
                _identityGenerator.GenerateGuid(),
                SnapshotStatus.Draft);
            return result;
        }

        private async Task<bool> IsExceptionExpected(Exception ex, MarketStateChangedEvent evt)
        {
            if (IsEventForPastDate(evt))
            {
                await _log.WriteWarningAsync(nameof(PlatformClosureProjection),
                    nameof(IsExceptionExpected),
                    evt.ToJson(),
                    "The event is for the past date, so the snapshot draft will not be created.", ex);
                return true;
            }

            var tradingDayFromEvent = DateOnly.FromDateTime(evt.EventTimestamp);
            await _log.WriteErrorAsync(nameof(PlatformClosureProjection),
                nameof(IsExceptionExpected),
                new { eventJson = evt.ToJson(), tradingDay = tradingDayFromEvent }.ToJson(),
                ex);

            return false;
        }

        private bool IsEventForPastDate(MarketStateChangedEvent evt)
        {
            var tradingDayFromEvent = DateOnly.FromDateTime(evt.EventTimestamp);
            return tradingDayFromEvent < _dateService.NowDateOnly();
        }
    }
}