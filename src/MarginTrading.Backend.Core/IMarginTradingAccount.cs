﻿using System;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core
{
    public interface IMarginTradingAccount
    {
        string Id { get; }
        string TradingConditionId { get; }
        string ClientId { get; }
        string BaseAssetId { get; }
        decimal Balance { get; }
        decimal WithdrawTransferLimit { get; }
        string LegalEntity { get; }
        [NotNull] AccountFpl AccountFpl { get; }
        bool IsDisabled { get; set; }
    }

    public class MarginTradingAccount : IMarginTradingAccount, IComparable<MarginTradingAccount>
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public decimal Balance { get; set; }
        public decimal WithdrawTransferLimit { get; set; }
        public string LegalEntity { get; set; }
        public bool IsDisabled { get; set; } // todo: use it everywhere

        public AccountFpl AccountFpl { get; private set; } = new AccountFpl();

        public static MarginTradingAccount Create(IMarginTradingAccount src, AccountFpl accountFpl = null)
        {
            return new MarginTradingAccount
            {
                Id = src.Id,
                TradingConditionId = src.TradingConditionId,
                ClientId = src.ClientId,
                BaseAssetId = src.BaseAssetId,
                Balance = src.Balance,
                WithdrawTransferLimit = src.WithdrawTransferLimit,
                AccountFpl = accountFpl ?? new AccountFpl {ActualHash = 1},
                LegalEntity = src.LegalEntity,
            };
        }

        public int CompareTo(MarginTradingAccount other)
        {
            var result = string.Compare(Id, other.Id, StringComparison.Ordinal);
            return 0 != result ? result : string.Compare(ClientId, other.ClientId, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ ClientId.GetHashCode();
        }
    }

    public enum AccountLevel
    {
        None = 0,
        MarginCall1 = 1,
        MarginCall2 = 2,
        StopOUt = 3
    }

    public static class MarginTradingAccountExtensions
    {
        private static AccountFpl GetAccountFpl(this IMarginTradingAccount account)
        {
            if (account is MarginTradingAccount accountInstance)
            {
                if (accountInstance.AccountFpl.ActualHash != accountInstance.AccountFpl.CalculatedHash)
                {
                    MtServiceLocator.AccountUpdateService.UpdateAccount(account);
                }

                return accountInstance.AccountFpl;
            }

            return new AccountFpl();
        }

        public static AccountLevel GetAccountLevel(this IMarginTradingAccount account)
        {
            var marginUsageLevel = account.GetMarginUsageLevel();

            if (marginUsageLevel <= account.GetStopOutLevel())
                return AccountLevel.StopOUt;

            if (marginUsageLevel <= account.GetMarginCall2Level())
                return AccountLevel.MarginCall2;
            
            if (marginUsageLevel <= account.GetMarginCall1Level())
                return AccountLevel.MarginCall1;

            return AccountLevel.None;
        }

        public static decimal GetMarginUsageLevel(this IMarginTradingAccount account)
        {
            var totalCapital = account.GetTotalCapital();
            
            var usedMargin = account.GetUsedMargin();

            //Anton Belkin said 100 is ok )
            if (usedMargin <= 0)
                return 100;

            return totalCapital / usedMargin;
        }

        public static decimal GetTotalCapital(this IMarginTradingAccount account)
        {
            return account.Balance + account.GetPnl() - account.GetFrozenMargin();
        }

        public static decimal GetPnl(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().PnL;
        }

        public static decimal GetUsedMargin(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().UsedMargin;
        }

        public static decimal GetFreeMargin(this IMarginTradingAccount account)
        {
            return account.GetTotalCapital() - account.GetUsedMargin();
        }

        public static decimal GetMarginInit(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().MarginInit;
        }

        public static decimal GetMarginAvailable(this IMarginTradingAccount account)
        {
            return account.GetTotalCapital() - account.GetMarginInit();
        }

        public static decimal GetFrozenMargin(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().WithdrawalFrozenMargin;
        }

        public static decimal GetMarginCall1Level(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().MarginCall1Level;
        }
        
        public static decimal GetMarginCall2Level(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().MarginCall2Level;
        }

        public static decimal GetStopOutLevel(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().StopoutLevel;
        }

        public static int GetOpenPositionsCount(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().OpenPositionsCount;
        }

        public static void CacheNeedsToBeUpdated(this IMarginTradingAccount account)
        {
            if (account is MarginTradingAccount accountInstance)
            {
                accountInstance.AccountFpl.ActualHash++;
            }
        }
    }
}

