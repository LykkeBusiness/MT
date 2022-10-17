// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MarginTrading.Backend.RestoreTool
{
    public class CustomDecimalConverter : DecimalConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text) 
                || text.Trim().ToLower().Equals("null") 
                || text.Trim().ToLower().Equals("n/a"))
            {
                return 0m;
            }
            
            return base.ConvertFromString(text, row, memberMapData);
        }
    }

    public class CustomDatetimeConverter : DateTimeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text) 
                || text.Trim().ToLower().Equals("null") 
                || text.Trim().ToLower().Equals("n/a"))
            {
                return null;
            }
            
            return base.ConvertFromString(text, row, memberMapData);
        }
    }

    public class PositionCsvClassMap : ClassMap<PositionCsvModel>
    {
        public PositionCsvClassMap()
        {
            Map(m => m.OID).Name("OID");
            Map(m => m.Id).Name("Id");
            Map(m => m.DealId).Name("DealId");
            Map(m => m.Code).Name("Code");
            Map(m => m.AssetPairId).Name("AssetPairId");
            Map(m => m.Direction).Name("Direction");
            Map(m => m.Volume).Name("Volume");
            Map(m => m.AccountId).Name("AccountId");
            Map(m => m.TradingConditionId).Name("TradingConditionId");
            Map(m => m.AccountAssetId).Name("AccountAssetId");
            Map(m => m.ExpectedOpenPrice).Name("ExpectedOpenPrice").TypeConverter<CustomDecimalConverter>();
            Map(m => m.OpenMatchingEngineId).Name("OpenMatchingEngineId");
            Map(m => m.OpenDate).Name("OpenDate");
            Map(m => m.OpenTradeId).Name("OpenTradeId");
            Map(m => m.OpenPrice).Name("OpenPrice");
            Map(m => m.OpenFxPrice).Name("OpenFxPrice");
            Map(m => m.EquivalentAsset).Name("EquivalentAsset");
            Map(m => m.OpenPriceEquivalent).Name("OpenPriceEquivalent");
            Map(m => m.RelatedOrders).Name("RelatedOrders");
            Map(m => m.LegalEntity).Name("LegalEntity");
            Map(m => m.OpenOriginator).Name("OpenOriginator");
            Map(m => m.ExternalProviderId).Name("ExternalProviderId");
            Map(m => m.SwapCommissionRate).Name("SwapCommissionRate");
            Map(m => m.OpenCommissionRate).Name("OpenCommissionRate");
            Map(m => m.CloseCommissionRate).Name("CloseCommissionRate");
            Map(m => m.CommissionLot).Name("CommissionLot");
            Map(m => m.CloseMatchingEngineId).Name("CloseMatchingEngineId");
            Map(m => m.ClosePrice).Name("ClosePrice");
            Map(m => m.CloseFxPrice).Name("CloseFxPrice");
            Map(m => m.ClosePriceEquivalent).Name("ClosePriceEquivalent");
            Map(m => m.StartClosingDate).Name("StartClosingDate");
            Map(m => m.CloseDate).Name("CloseDate");
            Map(m => m.CloseOriginator).Name("CloseOriginator");
            Map(m => m.CloseReason).Name("CloseReason");
            Map(m => m.CloseComment).Name("CloseComment");
            Map(m => m.CloseTrades).Name("CloseTrades");
            Map(m => m.LastModified).Name("LastModified").TypeConverter<CustomDatetimeConverter>();
            Map(m => m.TotalPnL).Name("TotalPnL");
            Map(m => m.ChargedPnl).Name("ChargedPnl");
            Map(m => m.HistoryType).Name("HistoryType");
            Map(m => m.DealInfo).Name("DealInfo");
            Map(m => m.HistoryTimestamp).Name("HistoryTimestamp");
            Map(m => m.OpenOrderType).Name("OpenOrderType");
            Map(m => m.OpenOrderVolume).Name("OpenOrderVolume");
            Map(m => m.FxAssetPairId).Name("FxAssetPairId");
            Map(m => m.FxToAssetPairDirection).Name("FxToAssetPairDirection");
            Map(m => m.AdditionalInfo).Name("AdditionalInfo");
            Map(m => m.ForceOpen).Name("ForceOpen");
            Map(m => m.CorrelationId).Name("CorrelationId");
        }
    }
}
