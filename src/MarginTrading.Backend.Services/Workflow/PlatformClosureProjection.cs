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
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow
{
    public class PlatformClosureProjection
    {
        private readonly ISnapshotService _snapshotService;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly ILog _log;
        private readonly IDateService _dateService;

        public PlatformClosureProjection(ISnapshotService snapshotService,
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
            
            var successMessage = string.Empty; 
            try
            {
                successMessage = await CreateDraftSnapshot(e.EventTimestamp.Date);
            }
            catch (Exception ex)
            {
                if (!await IsExceptionExpected(ex, e))
                {
                    throw;
                }
            }

            if (!string.IsNullOrWhiteSpace(successMessage))
            {
                await _log.WriteInfoAsync(nameof(PlatformClosureProjection),
                    nameof(Handle),
                    e.ToJson(),
                    successMessage);
            }
        }

        private Task<string> CreateDraftSnapshot(DateTime tradingDay)
        {
            return _snapshotService.MakeTradingDataSnapshot(tradingDay,
                _identityGenerator.GenerateGuid(),
                SnapshotStatus.Draft);
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
                new {eventJson = evt.ToJson(), tradingDay = tradingDayFromEvent}.ToJson(),
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