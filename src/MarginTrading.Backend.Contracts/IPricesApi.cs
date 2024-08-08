﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.ErrorCodes;
using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Contracts.Responses;
using MarginTrading.Backend.Contracts.Snow.Prices;
using Refit;
using InitPricesBackendRequest = MarginTrading.Backend.Contracts.Prices.InitPricesBackendRequest;

namespace MarginTrading.Backend.Contracts
{
    /// <summary>                                                                                       
    /// Provides data about prices
    /// </summary>
    [PublicAPI]
    public interface IPricesApi
    {
        /// <summary>
        /// Get current best prices
        /// </summary>
        /// <remarks>
        /// Post because the query string will be too long otherwise
        /// </remarks>
        [Post("/api/prices/best")]
        Task<Dictionary<string, BestPriceContract>> GetBestAsync([Body] [NotNull] InitPricesBackendRequest request);

        /// <summary>
        /// Get current best fx prices
        /// </summary>
        /// <remarks>
        /// Post because the query string will be too long otherwise
        /// </remarks>
        [Post("/api/prices/bestFx")]
        Task<Dictionary<string, BestPriceContract>> GetBestFxAsync([Body] [NotNull] InitPricesBackendRequest request);

        /// <summary>
        /// Upload EOD quotes for the trading day EOD was missed for
        /// </summary>
        /// <returns></returns>
        [Post("/api/prices/missed")]
        Task<QuotesUploadErrorCode> UploadMissingQuotesAsync([Body] [NotNull] UploadMissingQuotesRequest request);

        /// <summary>
        /// Deletes fx rate for assetPairId from cache
        /// </summary>
        /// <returns></returns>
        [Delete("/api/prices/internal/bestFx/{assetPairId}")]
        Task<bool> RemoveFromBestFxPriceCacheInternal(string assetPairId);
    }
}