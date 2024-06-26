﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Contracts.Responses;
using MarginTrading.Backend.Contracts.Account;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface IAccountsApi
    {
        /// <summary>
        /// Returns all accounts stats
        /// </summary>
        [Get("/api/accounts/stats")]
        Task<List<AccountStatContract>> GetAllAccountStats();
        
        /// <summary>
        /// Returns all accounts stats, optionally paginated. Both skip and take must be set or unset.
        /// </summary>
        [Get("/api/accounts/stats/by-pages")]
        Task<PaginatedResponse<AccountStatContract>> GetAllAccountStatsByPages(
            [Query, CanBeNull] int? skip = null, [Query, CanBeNull] int? take = null);

        /// <summary>
        /// Get accounts depending on active/open orders and positions for particular assets.
        /// </summary>
        /// <returns>List of account ids</returns>
        [Post("/api/accounts")]
        Task<List<string>> GetAllAccountIdsFiltered([Body] ActiveAccountsRequest request);
        
        /// <summary>
        /// Returns stats of selected account
        /// </summary>
        [Get("/api/accounts/stats/{accountId}")]
        [Obsolete("Use GetCapitalFigures instead.")]
        Task<AccountStatContract> GetAccountStats([NotNull] string accountId);
        
        /// <summary>
        /// Returns capital-figures of selected account
        /// </summary>
        [Get("/api/accounts/capital-figures/{accountId}")]
        Task<AccountCapitalFigures> GetCapitalFigures([NotNull] string accountId);

        /// <summary>
        /// Resumes liquidation of selected account
        /// </summary>
        [Post("/api/accounts/resume-liquidation/{accountId}")]
        Task ResumeLiquidation(string accountId, string comment);
    }
}