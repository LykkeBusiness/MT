// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using AutoMapper;
using Common;
using MarginTrading.Backend.Contracts.Activities;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.RestoreTool;

public class Handler
{
    private readonly DateService _dateService = new DateService();
    private readonly ConvertService _convertService = new ConvertService();
    private readonly IIdentityGenerator _identityGenerator = new SimpleIdentityGenerator();

    private PositionHistoryEvent CreatePositionHistoryEvent(Position position,
        PositionHistoryTypeContract type,
        DealContract deal = null,
        string additionalInfo = null,
        string metadata = null)
    {
        var positionSnapshot = _convertService.Convert<Position, PositionContract>(position,
            o => o.ConfigureMap(MemberList.Destination).ForMember(x => x.TotalPnL, c => c.Ignore()));
        positionSnapshot.TotalPnL = position.GetFpl();

        return new PositionHistoryEvent
        {
            PositionSnapshot = positionSnapshot,
            Deal = deal,
            EventType = type,
            Timestamp = _dateService.Now(),
            ActivitiesMetadata = metadata,
            OrderAdditionalInfo = additionalInfo
        };
    }
    
    private PositionHistoryEvent CreatePositionHistoryEventWhenClosing(Position position,
            PositionHistoryTypeContract historyType,
            decimal chargedPnl,
            string orderAdditionalInfo,
            Order dealOrder,
            decimal dealVolume,
            PositionOpenMetadata metadata = null)
        {
            var sign = position.Volume > 0 ? 1 : -1;

            var accountBaseAssetAccuracy = AssetsConstants.DefaultAssetAccuracy;

            var fpl = Math.Round((dealOrder.ExecutionPrice.Value - position.OpenPrice) *
                                 dealOrder.FxRate * dealVolume * sign, accountBaseAssetAccuracy);
            var balanceDelta = fpl - Math.Round(chargedPnl, accountBaseAssetAccuracy);

            var dealId = historyType == PositionHistoryTypeContract.Close
                ? position.Id
                : _identityGenerator.GenerateAlphanumericId();

            var deal = new DealContract
            {
                DealId = dealId,
                PositionId = position.Id,
                Volume = dealVolume,
                Created = dealOrder.Executed.Value,
                OpenTradeId = position.OpenTradeId,
                OpenOrderType = Common.Extensions.EnumExtensions.ToType<OrderTypeContract>(position.OpenOrderType),
                OpenOrderVolume = position.OpenOrderVolume,
                OpenOrderExpectedPrice = position.ExpectedOpenPrice,
                CloseTradeId = dealOrder.Id,
                CloseOrderType = Common.Extensions.EnumExtensions.ToType<OrderTypeContract>(dealOrder.OrderType),
                CloseOrderVolume = dealOrder.Volume,
                CloseOrderExpectedPrice = dealOrder.Price,
                OpenPrice = position.OpenPrice,
                OpenFxPrice = position.OpenFxPrice,
                ClosePrice = dealOrder.ExecutionPrice.Value,
                CloseFxPrice = dealOrder.FxRate,
                Fpl = fpl,
                PnlOfTheLastDay = balanceDelta,
                AdditionalInfo = dealOrder.AdditionalInfo,
                Originator = Common.Extensions.EnumExtensions.ToType<OriginatorTypeContract>(dealOrder.Originator)
            };

            var positionContract = _convertService.Convert<Position, PositionContract>(position,
                o => o.ConfigureMap(MemberList.Destination).ForMember(x => x.TotalPnL, c => c.Ignore()));
            // get fpl
            if (position.FplData.ActualHash == 0)
            {
                position.FplData.ActualHash = 1;
            }
            
            position.FplData.CalculatedHash = position.FplData.ActualHash;
            
            if (position.FplData.AccountBaseAssetAccuracy == default)
            {
                position.FplData.AccountBaseAssetAccuracy = AssetsConstants.DefaultAssetAccuracy;
            }
            
            position.FplData.RawFpl = (position.ClosePrice - position.OpenPrice) * position.CloseFxPrice * position.Volume;

            if (position.ClosePrice == 0)
                position.UpdateClosePrice(position.OpenPrice);
            positionContract.TotalPnL = position.GetFpl();

            return new PositionHistoryEvent
            {
                PositionSnapshot = positionContract,
                Deal = deal,
                EventType = historyType,
                Timestamp = _dateService.Now(),
                ActivitiesMetadata = metadata?.ToJson(),
                OrderAdditionalInfo = orderAdditionalInfo,
            };
        }

    public PositionHistoryEvent ConsumeEvent(OrderExecutedEventArgs ea)
    {
        var csvPositions = CsvReaders.ReadPositionsFromCsv();

        var order = ea.Order;

        if (order.ForceOpen)
        {
            // build open position from order
            var position = new Position(order.Id, order.Code, order.AssetPairId, order.Volume, order.AccountId,
                order.TradingConditionId, order.AccountAssetId, order.Price, order.MatchingEngineId,
                order.Executed.Value, order.Id, order.OrderType, order.Volume, order.ExecutionPrice.Value,
                order.FxRate,
                order.EquivalentAsset, order.EquivalentRate, order.RelatedOrders, order.LegalEntity,
                order.Originator,
                order.ExternalProviderId, order.FxAssetPairId, order.FxToAssetPairDirection, order.AdditionalInfo,
                order.ForceOpen);

            // handle open position history case
            var metadata = new PositionOpenMetadata {ExistingPositionIncreased = false};
            return CreatePositionHistoryEvent(position, PositionHistoryTypeContract.Open, null,
                order.AdditionalInfo, metadata.ToJson());
        }
        
        var csvAccountsHistory = CsvReaders.ReadAccountHistoryFromCsv();

        if (order.PositionsToBeClosed.Any())
        {
            if (order.PositionsToBeClosed.Count > 1)
            {
                throw new Exception("Multiple positions to be closed is not supported");
            }

            var positionId = order.PositionsToBeClosed.First();
            var positionCsv = csvPositions.Single(p => p.Id == positionId);
            // we have only one position with related orders in csv
            var relatedOrdersList = positionId == "83RRIOVDHK"
                ? new List<RelatedOrderInfo>
                    { new RelatedOrderInfo { Id = "DX5KIYMBF9", Type = OrderType.TakeProfit } }
                : new List<RelatedOrderInfo>();
            var position = new Position(positionCsv.Id, positionCsv.Code, positionCsv.AssetPairId,
                positionCsv.Volume, positionCsv.AccountId,
                positionCsv.TradingConditionId, positionCsv.AccountAssetId, positionCsv.OpenPrice,
                positionCsv.OpenMatchingEngineId,
                positionCsv.OpenDate, positionCsv.OpenTradeId, positionCsv.OpenOrderType,
                positionCsv.OpenOrderVolume, positionCsv.OpenPrice, positionCsv.OpenFxPrice,
                positionCsv.EquivalentAsset, positionCsv.OpenPriceEquivalent, relatedOrdersList,
                positionCsv.LegalEntity, positionCsv.OpenOriginator,
                positionCsv.ExternalProviderId, positionCsv.FxAssetPairId, positionCsv.FxToAssetPairDirection,
                positionCsv.AdditionalInfo, positionCsv.ForceOpen);


            var accountHistoryEntry =
                csvAccountsHistory.Single(ah => ah.EventSourceId == position.Id || ah.Comment.Contains(position.Id));

            position.SetChargedPnL("any-operation-id", accountHistoryEntry.ChangeAmount);
            position.SetCommissionRates(positionCsv.SwapCommissionRate, positionCsv.OpenCommissionRate,
                positionCsv.CloseCommissionRate, positionCsv.CommissionLot);

            if (!DateTime.TryParse(positionCsv.StartClosingDate, out var startClosingDate))
                startClosingDate = order.ExecutionStarted.Value;
            if (!Enum.TryParse(positionCsv.CloseReason, out PositionCloseReason closeReason))
                closeReason = PositionCloseReason.None;
            if (!Enum.TryParse(positionCsv.CloseOriginator, out OriginatorType closeOriginator))
                closeOriginator = OriginatorType.System;
            position.StartClosing(startClosingDate, closeReason, closeOriginator, positionCsv.CloseComment);
            
            position.Close(order.Executed.Value, order.MatchingEngineId, order.ExecutionPrice.Value,
                order.EquivalentRate, order.FxRate, order.Originator, order.OrderType.GetCloseReason(),
                order.Comment, order.Id);

            // handle close position history case
            return CreatePositionHistoryEventWhenClosing(position, PositionHistoryTypeContract.Close,
                position.ChargedPnL, order.AdditionalInfo, order, Math.Abs(position.Volume));
        }

        // handle partial closure history case
        throw new NotImplementedException("Partial closure not implemented yet");
    }
}