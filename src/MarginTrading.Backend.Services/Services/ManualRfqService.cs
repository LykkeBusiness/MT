// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Services
{
    public class ManualRfqService : IRfqService
    {
        private readonly ICqrsSender _cqrsSender;
        private readonly IDateService _dateService;
        private readonly SpecialLiquidationSettings _specialLiquidationSettings;
        private readonly CqrsContextNamesSettings _cqrsContextNamesSettings;
        private readonly IQuoteCacheService _quoteCacheService;

        private ConcurrentDictionary<string, GetPriceForSpecialLiquidationCommand> _requests =
            new ConcurrentDictionary<string, GetPriceForSpecialLiquidationCommand>();

        public ManualRfqService(
            ICqrsSender cqrsSender,
            IDateService dateService,
            SpecialLiquidationSettings specialLiquidationSettings,
            CqrsContextNamesSettings cqrsContextNamesSettings,
            IQuoteCacheService quoteCacheService)
        {
            _cqrsSender = cqrsSender;
            _dateService = dateService;
            _specialLiquidationSettings = specialLiquidationSettings;
            _cqrsContextNamesSettings = cqrsContextNamesSettings;
            _quoteCacheService = quoteCacheService;
        }
        
        public void SavePriceRequestForSpecialLiquidation(GetPriceForSpecialLiquidationCommand command)
        {
            _requests.TryAdd(command.OperationId, command);
        }

        public void RejectPriceRequest(string operationId, string reason)
        {
            _cqrsSender.PublishEvent(new PriceForSpecialLiquidationCalculationFailedEvent
            {
                OperationId = operationId,
                CreationTime = _dateService.Now(),
                Reason = reason
            }, _cqrsContextNamesSettings.Gavel);
            
            _requests.TryRemove(operationId, out _);
        }

        public void ApprovePriceRequest(string operationId, decimal? price)
        {
            if (!_requests.TryGetValue(operationId, out var command))
            {
                throw new Exception($"Command with operation ID {operationId} does not exist");
            }
            
            if (price == null)
            {
                var quote = _quoteCacheService.GetQuote(command.Instrument);

                price = (command.Volume > 0 ? quote.Ask : quote.Bid) * _specialLiquidationSettings.FakePriceMultiplier;
            }

            _cqrsSender.PublishEvent(new PriceForSpecialLiquidationCalculatedEvent
            {
                OperationId = operationId,
                CreationTime = _dateService.Now(),
                Price = price.Value,
            }, _cqrsContextNamesSettings.Gavel);
            
            _requests.TryRemove(operationId, out _);
        }

        public List<GetPriceForSpecialLiquidationCommand> GetAllRequest()
        {
            return _requests.Values.ToList();
        }
    }
}