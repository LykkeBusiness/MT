﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using Common.Log;

using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.ErrorCodes;
using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Contracts.Responses;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Quotes;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Filters;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Mappers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    /// <inheritdoc cref="IPricesApi" />
    /// <summary>                                                                                       
    /// Prices management
    /// </summary>
    [Authorize]
    [Route("api/prices")]
    [ApiController]
    public class PricesController : ControllerBase, IPricesApi
    {
        private readonly IQuoteCacheService _quoteCacheService;
        private readonly IFxRateCacheService _fxRateCacheService;
        private readonly ISnapshotBuilderService _snapshotService;
        private readonly IDraftSnapshotKeeper _draftSnapshotKeeper;
        private readonly ILog _log;

        public PricesController(
            IQuoteCacheService quoteCacheService,
            IFxRateCacheService fxRateCacheService,
            ISnapshotBuilderService snapshotService,
            ILog log,
            IDraftSnapshotKeeper draftSnapshotKeeper)
        {
            _quoteCacheService = quoteCacheService;
            _fxRateCacheService = fxRateCacheService;
            _snapshotService = snapshotService;
            _log = log;
            _draftSnapshotKeeper = draftSnapshotKeeper;
        }

        /// <summary>
        /// Get current best prices
        /// </summary>
        /// <remarks>
        /// Post because the query string will be too long otherwise
        /// </remarks>
        [Route("best")]
        [HttpPost]
        public Task<Dictionary<string, BestPriceContract>> GetBestAsync(
            [FromBody] InitPricesBackendRequest request)
        {
            IEnumerable<KeyValuePair<string, InstrumentBidAskPair>> allQuotes = _quoteCacheService.GetAllQuotes();

            if (request.AssetIds != null && request.AssetIds.Any())
                allQuotes = allQuotes.Where(q => request.AssetIds.Contains(q.Key));

            return Task.FromResult(allQuotes.ToDictionary(q => q.Key, q => q.Value.ConvertToContract()));
        }

        /// <summary>
        /// Get current fx best prices
        /// </summary>
        /// <remarks>
        /// Post because the query string will be too long otherwise
        /// </remarks>
        [Route("bestFx")]
        [HttpPost]
        public Task<Dictionary<string, BestPriceContract>> GetBestFxAsync(
            [FromBody] InitPricesBackendRequest request)
        {
            IEnumerable<KeyValuePair<string, InstrumentBidAskPair>> allQuotes = _fxRateCacheService.GetAllQuotes();

            if (request.AssetIds != null && request.AssetIds.Any())
                allQuotes = allQuotes.Where(q => request.AssetIds.Contains(q.Key));

            return Task.FromResult(allQuotes.ToDictionary(q => q.Key, q => q.Value.ConvertToContract()));
        }

        /// <summary>
        /// Upload EOD quotes for the trading day EOD was missed for
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("missed")]
        public async Task<QuotesUploadErrorCode> UploadMissingQuotesAsync([FromBody] UploadMissingQuotesRequest request)
        {
            if (!DateTime.TryParse(request.TradingDay, out var tradingDay))
            {
                await _log.WriteWarningAsync(nameof(PricesController),
                    nameof(UploadMissingQuotesAsync),
                    request.TradingDay,
                    "Couldn't parse trading day");

                return QuotesUploadErrorCode.InvalidTradingDay;
            }

            var draftExists = await _draftSnapshotKeeper
                .Init(tradingDay)
                .ExistsAsync();

            if (!draftExists)
                return QuotesUploadErrorCode.NoDraft;

            try
            {
                await _snapshotService.Convert(
                    request.CorrelationId,
                    request.Cfd,
                    request.Forex);
            }
            catch (InvalidOperationException e)
            {
                await _log.WriteErrorAsync(nameof(PricesController), nameof(UploadMissingQuotesAsync), null, e);
                return QuotesUploadErrorCode.AlreadyInProgress;
            }
            catch (EmptyPriceUploadException e)
            {
                await _log.WriteErrorAsync(nameof(PricesController), nameof(UploadMissingQuotesAsync), null, e);
                return QuotesUploadErrorCode.EmptyQuotes;
            }

            return QuotesUploadErrorCode.None;
        }

        /// <summary>
        /// Remove quote from internal cache 
        /// </summary>
        /// <param name="assetPairId"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("best/{assetPairId}")]
        public MtBackendResponse<bool> RemoveFromBestPriceCache(string assetPairId)
        {
            var decodedAssetPairId = HttpUtility.UrlDecode(assetPairId);

            var result = _quoteCacheService.RemoveQuote(decodedAssetPairId);

            return result == RemoveQuoteErrorCode.None
                ? MtBackendResponse<bool>.Ok(true)
                : MtBackendResponse<bool>.Error(result.Message);
        }

        /// <summary>
        /// Remove fx quote from internal cache 
        /// </summary>
        /// <param name="assetPairId"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("bestFx/{assetPairId}")]
        public MtBackendResponse<bool> RemoveFromBestFxPriceCache(string assetPairId)
        {
            var decodedAssetPairId = HttpUtility.UrlDecode(assetPairId);

            var result = _fxRateCacheService.RemoveQuote(decodedAssetPairId);

            return result == RemoveQuoteErrorCode.None
                ? MtBackendResponse<bool>.Ok(true)
                : MtBackendResponse<bool>.Error(result.Message);
        }

        [ServiceFilter(typeof(DevelopmentEnvironmentFilter))]
        [HttpDelete]
        [Route("internal/bestFx/{assetPairId}")]
        public Task<bool> RemoveFromBestFxPriceCacheInternal(string assetPairId)
        {
            var decodedAssetPairId = HttpUtility.UrlDecode(assetPairId);

            var result = _fxRateCacheService.RemoveQuote(decodedAssetPairId);

            return Task.FromResult(result.ErrorCode == RemoveQuoteErrorCode.None);
        }
    }
}