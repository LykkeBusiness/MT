﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

using MarginTrading.AccountsManagement.Contracts.Api;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Contracts.TradeMonitoring;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Caches;
using MarginTrading.Common.Extensions;

using MatchedOrderContract = MarginTrading.Backend.Contracts.Orders.MatchedOrderContract;
using OrderDirectionContract = MarginTrading.Backend.Contracts.Orders.OrderDirectionContract;
using OrderStatusContract = MarginTrading.Backend.Contracts.Orders.OrderStatusContract;

namespace MarginTrading.Backend.Services.Mappers;

public static class DomainToContractMappers
{
    public static OrderContract ConvertToContract(this Order order, IOrderReaderBase orderReader) =>
        order.ConvertToContract([.. orderReader.GetRelatedOrders(order)]);

    public static OpenPositionContract ConvertToContract(this Position position, IOrderReaderBase orderReader) =>
        position.ConvertToContract([.. orderReader.GetRelatedOrders(position)]);

    /// <summary>
    /// Convert order to a model, supposed to be used for snapshot serialization
    /// </summary>
    /// <param name="order">Order domain model</param>
    /// <param name="status">Snapshot status</param>
    /// <param name="orderReader"></param>
    /// <returns></returns>
    public static object ConvertToSnapshotContract(this Order order, IOrderReaderBase orderReader, SnapshotStatus status = SnapshotStatus.Final)
    {
        return status == SnapshotStatus.Draft
            ? (object)order
            : order.ConvertToContract(orderReader);
    }

    /// <summary>
    /// Convert position to a model, supposed to be used for snapshot serialization
    /// </summary>
    /// <param name="position">Position domain model</param>
    /// <param name="status">Snapshot status</param>
    /// <param name="orderReader"></param>
    /// <returns></returns>
    public static object ConvertToSnapshotContract(this Position position, IOrderReaderBase orderReader, SnapshotStatus status = SnapshotStatus.Final)
    {
        return status == SnapshotStatus.Draft
            ? (object)position
            : position.ConvertToContract(orderReader);
    }

    /// <summary>
    /// Convert account to a model, supposed to be used for snapshot serialization
    /// </summary>
    /// <param name="account">Account domain model</param>
    /// <param name="isInLiquidation"></param>
    /// <param name="status">Snapshot status</param>
    /// <returns></returns>
    public static object ConvertToSnapshotContract(this IMarginTradingAccount account, bool isInLiquidation, SnapshotStatus status = SnapshotStatus.Final)
    {
        return status == SnapshotStatus.Draft
            ? (object)account
            : account.ConvertToContract(isInLiquidation);
    }

    public static OrderContract ConvertToContract(this Order order, List<Order> relatedOrdersDomain) => new()
    {
        Id = order.Id,
        AccountId = order.AccountId,
        AssetPairId = order.AssetPairId,
        CreatedTimestamp = order.Created,
        Direction = order.Direction.ToType<OrderDirectionContract>(),
        ExecutionPrice = order.ExecutionPrice,
        FxRate = order.FxRate,
        FxAssetPairId = order.FxAssetPairId,
        FxToAssetPairDirection = order.FxToAssetPairDirection.ToType<FxToAssetPairDirectionContract>(),
        ExpectedOpenPrice = order.Price,
        ForceOpen = order.ForceOpen,
        ModifiedTimestamp = order.LastModified,
        Originator = order.Originator.ToType<OriginatorTypeContract>(),
        ParentOrderId = order.ParentOrderId,
        PositionId = order.ParentPositionId,
        RelatedOrders = order.RelatedOrders.Select(o => o.Id).ToList(),
        Status = order.Status.ToType<OrderStatusContract>(),
        FillType = order.FillType.ToType<OrderFillTypeContract>(),
        Type = order.OrderType.ToType<OrderTypeContract>(),
        ValidityTime = order.Validity,
        Volume = order.Volume,
        //------
        AccountAssetId = order.AccountAssetId,
        EquivalentAsset = order.EquivalentAsset,
        ActivatedTimestamp = order.Activated,
        CanceledTimestamp = order.Canceled,
        Code = order.Code,
        Comment = order.Comment,
        EquivalentRate = order.EquivalentRate,
        ExecutedTimestamp = order.Executed,
        ExecutionStartedTimestamp = order.ExecutionStarted,
        ExternalOrderId = order.ExternalOrderId,
        ExternalProviderId = order.ExternalProviderId,
        LegalEntity = order.LegalEntity,
        MatchedOrders = [.. order.MatchedOrders.Select(o => new MatchedOrderContract
            {
                OrderId = o.OrderId,
                Volume = o.Volume,
                Price = o.Price,
                MarketMakerId = o.MarketMakerId,
                LimitOrderLeftToMatch = o.LimitOrderLeftToMatch,
                MatchedDate = o.MatchedDate,
                IsExternal = o.IsExternal
            })],
        MatchingEngineId = order.MatchingEngineId,
        Rejected = order.Rejected,
        RejectReason = order.RejectReason.ToType<OrderRejectReasonContract>(),
        RejectReasonText = order.RejectReasonText,
        RelatedOrderInfos = [.. order.RelatedOrders.Select(roi => relatedOrdersDomain.FirstOrDefault(o => o.Id == roi.Id)?.Map()).Where(o => o != null)],
        TradingConditionId = order.TradingConditionId,
        AdditionalInfo = order.AdditionalInfo,
        PendingOrderRetriesCount = order.PendingOrderRetriesCount,
        TrailingDistance = order.TrailingDistance,
        HasOnBehalf = order.HasOnBehalf,
    };

    public static OpenPositionContract ConvertToContract(this Position position, List<Order> relatedOrdersDomain) => new()
    {
        AccountId = position.AccountId,
        AssetPairId = position.AssetPairId,
        CurrentVolume = position.Volume,
        Direction = position.Direction.ToType<PositionDirectionContract>(),
        Id = position.Id,
        OpenPrice = position.OpenPrice,
        OpenFxPrice = position.OpenFxPrice,
        ClosePrice = position.ClosePrice,
        ExpectedOpenPrice = position.ExpectedOpenPrice,
        OpenTradeId = position.OpenTradeId,
        OpenOrderType = position.OpenOrderType.ToType<OrderTypeContract>(),
        OpenOrderVolume = position.OpenOrderVolume,
        PnL = position.GetFpl(),
        UnrealizedPnl = position.GetUnrealisedFpl(),
        ChargedPnl = position.ChargedPnL,
        Margin = position.GetMarginMaintenance(),
        FxRate = position.CloseFxPrice,
        FxAssetPairId = position.FxAssetPairId,
        FxToAssetPairDirection = position.FxToAssetPairDirection.ToType<FxToAssetPairDirectionContract>(),
        RelatedOrders = [.. position.RelatedOrders.Select(o => o.Id)],
        RelatedOrderInfos = [.. position.RelatedOrders.Select(roi => relatedOrdersDomain.FirstOrDefault(o => o.Id == roi.Id)?.Map()).Where(o => o != null)],
        OpenTimestamp = position.OpenDate,
        ModifiedTimestamp = position.LastModified,
        TradeId = position.Id,
        AdditionalInfo = position.AdditionalInfo,
        Status = position.Status.ToType<PositionStatusContract>(),
        ForceOpen = position.ForceOpen,
        TradingConditionId = position.TradingConditionId,
        SwapTotal = position.SwapTotal
    };

    public static AccountCapitalFigures ToCapitalFiguresResponseContract(this IMarginTradingAccount account) => new()
    {
        Balance = account.Balance,
        LastBalanceChangeTime = account.LastBalanceChangeTime,
        TotalCapital = account.GetTotalCapital(),
        FreeMargin = account.GetFreeMargin(),
        UsedMargin = account.GetUsedMargin(),
        CurrentlyUsedMargin = account.GetCurrentlyUsedMargin(),
        InitiallyUsedMargin = account.GetInitiallyUsedMargin(),
        PnL = account.GetPnl(),
        UnrealizedDailyPnl = account.GetUnrealizedDailyPnl(),
        OpenPositionsCount = account.GetOpenPositionsCount(),
        TodayStartBalance = account.TodayStartBalance,
        TodayRealizedPnL = account.TodayRealizedPnL,
        TodayUnrealizedPnL = account.TodayUnrealizedPnL,
        TodayDepositAmount = account.TodayDepositAmount,
        TodayWithdrawAmount = account.TodayWithdrawAmount,
        TodayCommissionAmount = account.TodayCommissionAmount,
        TodayOtherAmount = account.TodayOtherAmount,
        AdditionalInfo = account.AdditionalInfo,
        AccountIsDeleted = account.IsDeleted,
        UnconfirmedMargin = account.GetUnconfirmedMargin()
    };

    public static GetDisposableCapitalRequest.AccountCapitalFigures ToCapitalFiguresRequestContract(
        this IMarginTradingAccount account) => new()
        {
            Balance = account.Balance,
            LastBalanceChangeTime = account.LastBalanceChangeTime,
            TotalCapital = account.GetTotalCapital(),
            FreeMargin = account.GetFreeMargin(),
            UsedMargin = account.GetUsedMargin(),
            CurrentlyUsedMargin = account.GetCurrentlyUsedMargin(),
            InitiallyUsedMargin = account.GetInitiallyUsedMargin(),
            PnL = account.GetPnl(),
            UnrealizedDailyPnl = account.GetUnrealizedDailyPnl(),
            OpenPositionsCount = account.GetOpenPositionsCount(),
            TodayStartBalance = account.TodayStartBalance,
            TodayRealizedPnL = account.TodayRealizedPnL,
            TodayUnrealizedPnL = account.TodayUnrealizedPnL,
            TodayDepositAmount = account.TodayDepositAmount,
            TodayWithdrawAmount = account.TodayWithdrawAmount,
            TodayCommissionAmount = account.TodayCommissionAmount,
            TodayOtherAmount = account.TodayOtherAmount,
            AdditionalInfo = account.AdditionalInfo,
            AccountIsDeleted = account.IsDeleted,
        };

    public static AccountStatContract ConvertToContract(this IMarginTradingAccount account, bool isInLiquidation) => new()
    {
        AccountId = account.Id,
        BaseAssetId = account.BaseAssetId,
        Balance = account.Balance,
        LastBalanceChangeTime = account.LastBalanceChangeTime,
        MarginCallLevel = account.GetMarginCall1Level(),
        StopOutLevel = account.GetStopOutLevel(),
        TotalCapital = account.GetTotalCapital(),
        FreeMargin = account.GetFreeMargin(),
        MarginAvailable = account.GetMarginAvailable(),
        UsedMargin = account.GetUsedMargin(),
        CurrentlyUsedMargin = account.GetCurrentlyUsedMargin(),
        InitiallyUsedMargin = account.GetInitiallyUsedMargin(),
        MarginInit = account.GetMarginInit(),
        PnL = account.GetPnl(),
        UnrealizedDailyPnl = account.GetUnrealizedDailyPnl(),
        OpenPositionsCount = account.GetOpenPositionsCount(),
        ActiveOrdersCount = account.GetActiveOrdersCount(),
        MarginUsageLevel = account.GetMarginUsageLevel(),
        LegalEntity = account.LegalEntity,
        IsInLiquidation = isInLiquidation,
        MarginNotificationLevel = account.GetAccountLevel().ToString(),
        TemporaryCapital = account.TemporaryCapital,
        UnconfirmedMargin = account.GetUnconfirmedMargin()
    };

    public static BestPriceContract ConvertToContract(this InstrumentBidAskPair arg) => new()
    {
        Ask = arg.Ask,
        Bid = arg.Bid,
        Id = arg.Instrument,
        Timestamp = arg.Date,
    };
}