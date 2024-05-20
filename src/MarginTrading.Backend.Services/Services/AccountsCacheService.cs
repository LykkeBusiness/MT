// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Helpers;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Common.Services;
using MoreLinq;

namespace MarginTrading.Backend.Services
{
    public class AccountsCacheService : IAccountsCacheService
    {
        private Dictionary<string, MarginTradingAccount> _accounts = new Dictionary<string, MarginTradingAccount>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();
        private readonly IDateService _dateService;
        private readonly IRunningLiquidationRepository _runningLiquidationRepository;
        private readonly ILog _log;

        public AccountsCacheService(
            IDateService dateService,
            IRunningLiquidationRepository runningLiquidationRepository,
            ILog log)
        {
            _dateService = dateService;
            _runningLiquidationRepository = runningLiquidationRepository;
            _log = log;
        }
        
        public IReadOnlyList<MarginTradingAccount> GetAll()
        {
            return _accounts.Values.ToArray();
        }

        public IAsyncEnumerable<MarginTradingAccount> GetAllWhereLiquidationIsRunning()
        {
            var accountIds = _accounts.Select(a => a.Value.Id);

            var runningLiquidations = _runningLiquidationRepository.Get(accountIds.ToArray());

            return runningLiquidations
                .Select(i => _accounts.SingleOrDefault(a => a.Value.Id == i.AccountId).Value)
                .Where(a => a != null);
        }

        public PaginatedResponse<MarginTradingAccount> GetAllByPages(int? skip = null, int? take = null)
        {
            var accounts = _accounts.Values.OrderBy(x => x.Id).ToList();//todo think again about ordering
            var data = (!take.HasValue ? accounts : accounts.Skip(skip.Value))
                .Take(PaginationHelper.GetTake(take)).ToList();
            return new PaginatedResponse<MarginTradingAccount>(
                contents: data,
                start: skip ?? 0,
                size: data.Count,
                totalSize: accounts.Count
            );
        }

        public MarginTradingAccount Get(string accountId)
        {
            return TryGetAccount(accountId) ??
                throw new AccountNotFoundException(accountId, string.Format(MtMessages.AccountByIdNotFound, accountId));
        }

        public async Task<string> GetRunningLiquidationOperationId(string accountId)
        {
            var runningLiquidations = await _runningLiquidationRepository.Get(new[] { accountId }).ToListAsync();
            return runningLiquidations.Any() ? runningLiquidations.First().OperationId : null;
        }

        public MarginTradingAccount TryGet(string accountId) => TryGetAccount(accountId);

        private MarginTradingAccount TryGetAccount(string accountId)
        {
            _lockSlim.EnterReadLock();
            try
            {
                if (!_accounts.TryGetValue(accountId, out var result))
                {
                    _log.WriteWarning(nameof(TryGetAccount), null, $"Account with id {accountId} not found in AccountsCacheService");
                    return null;
                }

                return result;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        internal void InitAccountsCache(Dictionary<string, MarginTradingAccount> accounts)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _accounts = accounts;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public void ResetTodayStatistics()
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _accounts.Values.ForEach(x =>
                {
                    x.TodayStartBalance = x.Balance;
                    x.TodayRealizedPnL = 0;
                    x.TodayUnrealizedPnL = 0;
                    x.TodayDepositAmount = 0;
                    x.TodayWithdrawAmount = 0;
                    x.TodayCommissionAmount = 0;
                    x.TodayOtherAmount = 0;
                });
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public async Task<(bool, string)> TryAddRunningLiquidation(string accountId, string operationId)
        {
            if (TryGet(accountId) == null)
                return (false, string.Empty);

            var item = new RunningLiquidation(operationId, accountId);
            var added = await _runningLiquidationRepository.TryAdd(accountId, item);
            if (added) return (true, operationId);

            var alreadyRunningLiquidationOperationId = await GetRunningLiquidationOperationId(accountId);
            if (alreadyRunningLiquidationOperationId == null)
            {
                // potentially, it is possible in a highly concurrent environments
                return (false, string.Empty);
            }

            return (false, alreadyRunningLiquidationOperationId);
        }

        public async Task<bool> TryRemoveRunningLiquidation(
            string accountId, 
            string reason, 
            string liquidationOperationId = null)
        {
            var account = TryGet(accountId);
            if (account == null) return false;

            var runningLiquidationOperationId = await GetRunningLiquidationOperationId(accountId);
            if (runningLiquidationOperationId == null)
                return false;

            if (string.IsNullOrEmpty(liquidationOperationId) ||
                liquidationOperationId == runningLiquidationOperationId)
            {
                await _runningLiquidationRepository.TryRemove(accountId);

                _log.WriteInfo(nameof(TryRemoveRunningLiquidation), account,
                    $"Running liquidation was removed for account {accountId}. Reason: {reason}");
                return true;
            }

            _log.WriteInfo(nameof(TryRemoveRunningLiquidation), account,
                $"Running liquidation was not removed for account {accountId} " +
                $"by liquidationOperationId {liquidationOperationId} " +
                $"Running LiquidationOperationId: {runningLiquidationOperationId}.");
            
            return false;
        }

        public async Task<bool> HasRunningLiquidation(string accountId)
        {
            var liquidationOperationId = await GetRunningLiquidationOperationId(accountId);
            return liquidationOperationId != null;
        }

        public async Task<bool> TryUpdateAccountChanges(string accountId, string updatedTradingConditionId,
            decimal updatedWithdrawTransferLimit, bool isDisabled, bool isWithdrawalDisabled, DateTime eventTime, string additionalInfo)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                var account = _accounts[accountId];
                if (account.LastUpdateTime > eventTime)
                {
                    await _log.WriteInfoAsync(nameof(AccountsCacheService), nameof(TryUpdateAccountChanges), 
                        $"Account with id {account.Id} is in newer state then the event");
                    return false;
                } 
                
                account.TradingConditionId = updatedTradingConditionId;
                account.WithdrawTransferLimit = updatedWithdrawTransferLimit;
                account.IsDisabled = isDisabled;
                account.IsWithdrawalDisabled = isWithdrawalDisabled;
                account.LastUpdateTime = eventTime;
                account.AdditionalInfo = additionalInfo;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
            return true;
        }

        public async Task<bool> HandleBalanceChange(string accountId,
            decimal accountBalance, decimal changeAmount, AccountBalanceChangeReasonType reasonType, DateTime eventTime)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                var account = _accounts[accountId];

                switch (reasonType)
                {
                    case AccountBalanceChangeReasonType.RealizedPnL:
                        account.TodayRealizedPnL += changeAmount;
                        break;
                    case AccountBalanceChangeReasonType.UnrealizedDailyPnL:
                        account.TodayUnrealizedPnL += changeAmount;
                        account.TodayOtherAmount += changeAmount; // TODO: why?
                        break;
                    case AccountBalanceChangeReasonType.Deposit:
                        account.TodayDepositAmount += changeAmount;
                        break;
                    case AccountBalanceChangeReasonType.Withdraw:
                        account.TodayWithdrawAmount += changeAmount;
                        break;
                    case AccountBalanceChangeReasonType.Commission:
                        account.TodayCommissionAmount += changeAmount;
                        break;
                    case AccountBalanceChangeReasonType.TemporaryCashAdjustment:
                        account.TemporaryCapital += changeAmount;
                        account.TodayOtherAmount += changeAmount; // to maintain backwards compatibility
                        break;
                    default:
                        account.TodayOtherAmount += changeAmount;
                        break;
                }

                if (account.LastBalanceChangeTime > eventTime)
                {
                    await _log.WriteInfoAsync(nameof(AccountsCacheService), nameof(HandleBalanceChange), 
                        $"Account with id {account.Id} has balance in newer state then the event");
                    return false;
                }
                
                account.Balance = accountBalance;
                account.LastBalanceChangeTime = eventTime;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
            return true;
        }

        public bool TryAdd(MarginTradingAccount account)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                account.LastUpdateTime = _dateService.Now();
                return _accounts.TryAdd(account.Id, account);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public async Task<bool> TryRemove(string accountId)
        {
            bool removed;
            
            _lockSlim.EnterWriteLock();
            try
            {
                removed = _accounts.Remove(accountId);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }

            if (!removed) return false;

            var removedRunningLiquidation = await _runningLiquidationRepository.TryRemove(accountId);
            if (removedRunningLiquidation)
                return true;

            _log.WriteWarning(nameof(TryRemove), accountId,
                $"Running liquidation info was not removed for account {accountId}");
                
            return true;
        }

        public async Task<string> Reset(string accountId, DateTime eventTime)
        {
            string warnings;
            
            _lockSlim.EnterWriteLock();
            try
            {
                if (!_accounts.TryGetValue(accountId, out var account))
                {
                    throw new Exception($"Account {accountId} does not exist.");
                }

                warnings = account.Reset(eventTime);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }

            var removedRunningLiquidation = await _runningLiquidationRepository.TryRemove(accountId);
            if (removedRunningLiquidation)
            {
                return string.Join(", ", new[] { warnings, $"Liquidation is in progress"});
            }

            return warnings;
        }
    }
}