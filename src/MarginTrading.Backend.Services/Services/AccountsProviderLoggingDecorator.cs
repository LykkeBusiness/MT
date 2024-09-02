// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Services;

namespace MarginTrading.Backend.Services.Services
{
    internal sealed class AccountsProviderLoggingDecorator : IAccountsProvider
    {
        private readonly IAccountsProvider _decoratee;
        private readonly ILog _logger;

        public AccountsProviderLoggingDecorator(IAccountsProvider decoratee, ILog logger)
        {
            _decoratee = decoratee;
            _logger = logger;
        }

        public MarginTradingAccount GetAccountById(string accountId)
        {
            var result = _decoratee.GetAccountById(accountId);
            if (result == null)
            {
                _logger.WriteWarningAsync(nameof(AccountsProviderLoggingDecorator),
                    nameof(GetAccountById),
                    accountId,
                    "Account not found");
            }
            else
            {
                _logger.WriteInfoAsync(nameof(AccountsProviderLoggingDecorator),
                    nameof(GetAccountById),
                    new {AccountId = result.Id, AccountLevel = result.GetAccountLevel()}.ToJson(),
                    "Account found");
            }
            
            return result;
        }

        public async Task<bool> TryRemoveRunningLiquidation(string accountId, string reason, string liquidationOperationId = null)
        {
            var result = await _decoratee.TryRemoveRunningLiquidation(accountId, reason, liquidationOperationId);
            if (!result)
            {
                await _logger.WriteWarningAsync(nameof(AccountsProviderLoggingDecorator),
                    nameof(TryRemoveRunningLiquidation),
                    new {accountId, reason, liquidationOperationId}.ToJson(),
                    "Failed to remove running liquidation state");
            }
            
            return result;
        }

        public Task<MarginTradingAccount> GetActiveOrDeleted(string accountId)
        {
            return _decoratee.GetActiveOrDeleted(accountId);
        }

        public Task<decimal?> GetDisposableCapital(string accountId)
        {
            return _decoratee.GetDisposableCapital(accountId);
        }
    }
}