﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Common.Services;
using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services.Workflow
{
    public class WithdrawalCommandsHandler
    {
        private readonly IDateService _dateService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IAccountUpdateService _accountUpdateService;
        private readonly IChaosKitty _chaosKitty;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly ILogger<WithdrawalCommandsHandler> _logger;
        private const string OperationName = "FreezeAmountForWithdrawal";

        public WithdrawalCommandsHandler(
            IDateService dateService,
            IAccountsCacheService accountsCacheService,
            IAccountUpdateService accountUpdateService,
            IChaosKitty chaosKitty,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            ILogger<WithdrawalCommandsHandler> logger)
        {
            _dateService = dateService;
            _accountsCacheService = accountsCacheService;
            _accountUpdateService = accountUpdateService;
            _chaosKitty = chaosKitty;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _logger = logger;
        }

        /// <summary>
        /// Freeze the the amount in the margin.
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(FreezeAmountForWithdrawalCommand command, IEventPublisher publisher)
        {
            var (executionInfo, _) = await _operationExecutionInfoRepository.GetOrAddAsync(
                operationName: OperationName,
                operationId: command.OperationId,
                factory: () => new OperationExecutionInfo<WithdrawalFreezeOperationData>(
                    operationName: OperationName,
                    id: command.OperationId,
                    lastModified: _dateService.Now(),
                    data: new WithdrawalFreezeOperationData
                    {
                        State = OperationState.Initiated,
                        AccountId = command.AccountId,
                        Amount = command.Amount,
                    }
                ));
            
            MarginTradingAccount account = null;
            try
            {
                account = _accountsCacheService.Get(command.AccountId);
            }
            catch
            {
                _logger.LogWarning("Freezing the amount for withdrawal has failed. Reason: Failed to get account data. " +
                    "Details: (OperationId: {OperationId}, AccountId: {AccountId}, Amount: {Amount})",
                    command.OperationId, command.AccountId, command.Amount);

                publisher.PublishEvent(new AmountForWithdrawalFreezeFailedEvent(command.OperationId, _dateService.Now(), 
                    command.AccountId, command.Amount, $"Failed to get account {command.AccountId}"));
                return;
            }

            if (executionInfo.Data.SwitchState(OperationState.Initiated, OperationState.Started))
            {
                var freeMargin = account.GetFreeMargin();

                if (freeMargin >= command.Amount)
                {
                    await _accountUpdateService.FreezeWithdrawalMargin(command.AccountId, command.OperationId,
                        command.Amount);
                    
                    _chaosKitty.Meow(command.OperationId);

                    _logger.LogInformation("The amount for withdrawal has been frozen. " +
                        "Details: (OperationId: {OperationId}, AccountId: {AccountId}, Amount: {Amount})",
                        command.OperationId, command.AccountId, command.Amount);

                    publisher.PublishEvent(new AmountForWithdrawalFrozenEvent(command.OperationId, _dateService.Now(),
                        command.AccountId, command.Amount, command.Reason));
                }
                else
                {
                    var reasonStr = $"There's not enough free margin. Available free margin is: {freeMargin}";

                    _logger.LogWarning("Freezing the amount for withdrawal has failed. Reason: {Reason}. " +
                        "Details: (OperationId: {OperationId}, AccountId: {AccountId}, Amount: {Amount})",
                        command.OperationId, command.AccountId, command.Amount, reasonStr);

                    publisher.PublishEvent(new AmountForWithdrawalFreezeFailedEvent(command.OperationId,
                        _dateService.Now(),
                        command.AccountId, command.Amount, reasonStr));
                }
                
                _chaosKitty.Meow(command.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }
        
        /// <summary>
        /// Withdrawal failed => margin must be unfrozen.
        /// </summary>
        /// <remarks>Errors are not handled => if error occurs event will be retried</remarks>
        [UsedImplicitly]
        private async Task Handle(UnfreezeMarginOnFailWithdrawalCommand command, IEventPublisher publisher)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<WithdrawalFreezeOperationData>(
                operationName: OperationName,
                id: command.OperationId
            );

            if (executionInfo == null)
                return;
            
            if (executionInfo.Data.SwitchState(OperationState.Started, OperationState.Finished))
            {
                await _accountUpdateService.UnfreezeWithdrawalMargin(executionInfo.Data.AccountId, command.OperationId);
                
                publisher.PublishEvent(new UnfreezeMarginOnFailSucceededWithdrawalEvent(command.OperationId,
                    _dateService.Now(), executionInfo.Data.AccountId, executionInfo.Data.Amount));
                
                _chaosKitty.Meow(command.OperationId);
                
                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }
    }
}