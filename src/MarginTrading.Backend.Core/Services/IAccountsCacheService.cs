// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Core.Exceptions;

namespace MarginTrading.Backend.Core
{
    public interface IAccountsCacheService
    {
        /// <summary>
        /// For every account, resets the statistics for the current day
        /// </summary>
        void ResetTodayStatistics();
        
        /// <summary>
        /// Gets the account by id
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        /// <exception cref="AccountNotFoundException">When account is not found</exception>
        [NotNull]
        MarginTradingAccount Get(string accountId);
        
        /// <summary>
        /// Gets the account by id, or null if not found
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [CanBeNull]
        MarginTradingAccount TryGet(string accountId);

        /// <summary>
        /// Get running liquidation operation id for the account. Fetches data from the cache.
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [ItemCanBeNull]
        Task<string> GetRunningLiquidationOperationId(string accountId);
        
        /// <summary>
        /// Lists all accounts in cache
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<MarginTradingAccount> GetAll();
        
        /// <summary>
        /// Lists all the accounts with currently running liquidation workflow
        /// </summary>
        /// <returns></returns>
        IAsyncEnumerable<MarginTradingAccount> GetAllWhereLiquidationIsRunning();
        
        /// <summary>
        /// List all the accounts in cache by pages
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        PaginatedResponse<MarginTradingAccount> GetAllByPages(int? skip = null, int? take = null);

        /// <summary>
        /// Tries to add account to cache. Returns false if account already exists thus not added.
        /// </summary>
        /// <param name="account"></param>
        /// <returns>True if account was added, false if account already exists</returns>
        bool TryAdd(MarginTradingAccount account);
        
        /// <summary>
        /// Tries to remove account from cache. Returns false if account cannot be removed.
        /// Under the hood removes the liquidation state if it exists.
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        Task<bool> TryRemove(string accountId);
        
        /// <summary>
        /// Resets the account statistics
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="eventTime"></param>
        /// <returns>List of warnings if any</returns>
        Task<string> Reset(string accountId, DateTime eventTime);

        /// <summary>
        /// Tries to apply changes to the account. Returns false if account
        /// is last updated later than the provided event time.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="updatedTradingConditionId"></param>
        /// <param name="updatedWithdrawTransferLimit"></param>
        /// <param name="isDisabled"></param>
        /// <param name="isWithdrawalDisabled"></param>
        /// <param name="eventTime"></param>
        /// <param name="additionalInfo"></param>
        /// <returns></returns>
        Task<bool> TryUpdateAccountChanges(
            string accountId, 
            string updatedTradingConditionId,
            decimal updatedWithdrawTransferLimit, 
            bool isDisabled, 
            bool isWithdrawalDisabled, 
            DateTime eventTime,
            string additionalInfo);
        
        /// <summary>
        /// Tries to apply balance changes to the account. Returns false if account
        /// is last updated later than the provided event time.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="accountBalance"></param>
        /// <param name="changeAmount"></param>
        /// <param name="reasonType"></param>
        /// <param name="eventTime"></param>
        /// <returns></returns>
        Task<bool> HandleBalanceChange(
            string accountId,
            decimal accountBalance, 
            decimal changeAmount, 
            AccountBalanceChangeReasonType reasonType, 
            DateTime eventTime);
        
        /// <summary>
        /// Tries to put liquidation information into distributed cache
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="operationId"></param>
        /// <returns>Tuple of success flag and the luquidation operation id</returns>
        Task<(bool, string)> TryAddSharedLiquidationState(string accountId, string operationId);
        
        /// <summary>
        /// Tries to remove liquidation information from distributed cache
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="reason"></param>
        /// <param name="liquidationOperationId"></param>
        /// <returns></returns>
        Task<bool> TryRemoveSharedLiquidationState(string accountId, string reason, string liquidationOperationId = null);

        /// <summary>
        /// Checks if the account has running liquidation by fetching data from distributed cache
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        Task<bool> HasRunningLiquidation(string accountId);
    }
}