using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Services.AssetPairs;

namespace MarginTradingTests.SnapshotBuilderServiceTests;

class FakeScheduleSettingsCacheService(DateTime tradingDay) : IScheduleSettingsCacheService
{
    private readonly DateTime _tradingDay = tradingDay;

    public void CacheWarmUpIncludingValidation()
    {
        throw new NotImplementedException();
    }

    public Dictionary<string, List<CompiledScheduleTimeInterval>> GetCompiledAssetPairScheduleSettings()
    {
        throw new NotImplementedException();
    }

    public InstrumentTradingStatus GetInstrumentTradingStatus(string assetPairId, TimeSpan scheduleCutOff)
    {
        throw new NotImplementedException();
    }

    public Dictionary<string, MarketState> GetMarketState()
    {
        throw new NotImplementedException();
    }

    public Dictionary<string, List<CompiledScheduleTimeInterval>> GetMarketsTradingSchedule()
    {
        throw new NotImplementedException();
    }

    public List<CompiledScheduleTimeInterval> GetMarketTradingScheduleByAssetPair(string assetPairId)
    {
        throw new NotImplementedException();
    }

    public List<CompiledScheduleTimeInterval> GetPlatformTradingSchedule()
    {
        throw new NotImplementedException();
    }

    public void HandleMarketStateChanges(DateTime currentTime)
    {
        throw new NotImplementedException();
    }

    public void MarketsCacheWarmUp()
    {
        throw new NotImplementedException();
    }

    public bool TryGetPlatformCurrentDisabledInterval(out CompiledScheduleTimeInterval disabledInterval)
    {
        disabledInterval = new CompiledScheduleTimeInterval(
            schedule: null,
            start: _tradingDay.AddDays(-1),
            end: _tradingDay.AddDays(1));
        return true;
    }

    public Task UpdateAllSettingsAsync()
    {
        throw new NotImplementedException();
    }

    public Task UpdateScheduleSettingsAsync()
    {
        throw new NotImplementedException();
    }
}
