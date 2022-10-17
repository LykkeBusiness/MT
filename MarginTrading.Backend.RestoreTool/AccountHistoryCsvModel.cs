// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using CsvHelper.Configuration;

namespace MarginTrading.Backend.RestoreTool;

public class AccountHistoryCsvModel
{
    public int Oid { get; set; }
    public string Id { get; set; }
    public string AccountId { get; set; }
    public DateTime ChangeTimestamp { get; set; }
    public string ClientId { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal Balance { get; set; }
    public decimal WithdrawTransferLimit { get; set; }
    public string Comment { get; set; }
    public string ReasonType { get; set; }
    public string EventSourceId { get; set; }
    public string LegalEntity { get; set; }
    public string AuditLog { get; set; }
    public string Instrument { get; set; }
    public DateTime TradingDate { get; set; }
    public string CorrelationId { get; set; }
}

public class AccountHistoryCsvClassMap : ClassMap<AccountHistoryCsvModel>
{
    public AccountHistoryCsvClassMap()
    {
        Map(m => m.Oid).Name("Oid");
        Map(m => m.Id).Name("Id");
        Map(m => m.AccountId).Name("AccountId");
        Map(m => m.ChangeTimestamp).Name("ChangeTimestamp");
        Map(m => m.ClientId).Name("ClientId");
        Map(m => m.ChangeAmount).Name("ChangeAmount");
        Map(m => m.Balance).Name("Balance");
        Map(m => m.WithdrawTransferLimit).Name("WithdrawTransferLimit");
        Map(m => m.Comment).Name("Comment");
        Map(m => m.ReasonType).Name("ReasonType");
        Map(m => m.EventSourceId).Name("EventSourceId");
        Map(m => m.LegalEntity).Name("LegalEntity");
        Map(m => m.AuditLog).Name("AuditLog");
        Map(m => m.Instrument).Name("Instrument");
        Map(m => m.TradingDate).Name("TradingDate");
        Map(m => m.CorrelationId).Name("CorrelationId");
    }
}
