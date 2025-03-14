// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Contracts.Snapshots;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Extensions;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Middleware;
using MarginTrading.Common.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/service")]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    [ApiController]
    public class ServiceController : ControllerBase, IServiceApi
    {
        private readonly IOvernightMarginParameterContainer _overnightMarginParameterContainer;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly ISnapshotService _snapshotService;
        private readonly IAccountUpdateService _accountUpdateService;
        private readonly IAccountsProvider _accountsProvider;
        private readonly IDateService _dateService;
        private readonly OrdersCache _ordersCache;

        public ServiceController(
            IOvernightMarginParameterContainer overnightMarginParameterContainer,
            IIdentityGenerator identityGenerator,
            ISnapshotService snapshotService,
            IAccountUpdateService accountUpdateService,
            IAccountsProvider accountsProvider,
            IDateService dateService,
            OrdersCache ordersCache)
        {
            _overnightMarginParameterContainer = overnightMarginParameterContainer;
            _identityGenerator = identityGenerator;
            _snapshotService = snapshotService;
            _accountUpdateService = accountUpdateService;
            _accountsProvider = accountsProvider;
            _dateService = dateService;
            _ordersCache = ordersCache;
        }

        /// <summary>
        /// Save snapshot of orders, positions, account stats, best fx prices, best trading prices for current moment.
        /// Throws an error in case if trading is not stopped.
        /// </summary>
        /// <param name="tradingDay">Trading day.</param>
        /// <param name="correlationId">Correlation ID.</param>
        /// <param name="status">Snapshot target status.</param>
        /// <returns>Snapshot statistics.</returns>
        [HttpPost("make-trading-data-snapshot")]
        public Task<string> MakeTradingDataSnapshot([FromQuery] DateTime tradingDay,
            [FromQuery] string correlationId = null,
            [FromQuery] SnapshotStatusContract status = SnapshotStatusContract.Final)
        {
            if (tradingDay == default)
            {
                throw new Exception($"{nameof(tradingDay)} must be set");
            }

            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = _identityGenerator.GenerateGuid();
            }

            var domainStatus = status.ToDomain();
            if (domainStatus == null)
                throw new ArgumentOutOfRangeException(nameof(status), status, "Invalid status value");
            return _snapshotService.MakeTradingDataSnapshot(tradingDay, correlationId, domainStatus.Value);
        }

        /// <summary>
        /// Get current state of overnight margin parameter.
        /// </summary>
        [HttpGet("current-overnight-margin-parameter")]
        public Task<bool> GetOvernightMarginParameterCurrentState()
        {
            return Task.FromResult(_overnightMarginParameterContainer.GetOvernightMarginParameterState());
        }

        /// <summary>
        /// Get current margin parameter values for instruments (all / filtered by IDs).
        /// </summary>
        /// <returns>
        /// Dictionary with key = asset pair ID and value = (Dictionary with key = trading condition ID and value = multiplier)
        /// </returns>
        [HttpGet("overnight-margin-parameter")]
        public Task<Dictionary<string, Dictionary<string, decimal>>> GetOvernightMarginParameterValues(
            [FromQuery] string[] instruments = null)
        {
            var result = _overnightMarginParameterContainer.GetOvernightMarginParameterValues()
                .Where(x => instruments == null || !instruments.Any() || instruments.Contains(x.Key.Item2))
                .GroupBy(x => x.Key.Item2)
                .OrderBy(x => x.Any(p => p.Value > 1) ? 0 : 1)
                .ThenBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.ToDictionary(p => p.Key.Item1, p => p.Value));

            return Task.FromResult(result);
        }

        /// <inheritdoc />
        [HttpGet("unconfirmed-margin")]
        public Task<Dictionary<string, decimal>> GetUnconfirmedMargin([FromQuery] string accountId)
        {
            var account = _accountsProvider.GetAccountById(accountId);

            if (account == null)
                return Task.FromResult(new Dictionary<string, decimal>());

            return Task.FromResult(
                account
                    .AccountFpl
                    .UnconfirmedMarginData
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            );
        }

        /// <inheritdoc />
        [HttpPost("unconfirmed-margin")]
        public Task FreezeUnconfirmedMargin(string accountId, string operationId, decimal amount)
        {
            return _accountUpdateService.FreezeUnconfirmedMargin(accountId, operationId, amount);
        }

        /// <inheritdoc />
        [HttpDelete("unconfirmed-margin")]
        public Task UnfreezeUnconfirmedMargin([FromQuery] string accountId, [FromQuery] string operationId)
        {
            return _accountUpdateService.UnfreezeUnconfirmedMargin(accountId, operationId);
        }

        /// <summary>
        /// Put back position in Closing state to Active state.
        /// This should be used judiciously, as it can lead to inconsistencies in the system.
        /// Consider it a last resort option.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("put-back-position-in-closing-state")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult PutBackPositionInClosingState([FromBody] PutBackPositionInClosingStateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PositionId))
            {
                return BadRequest("PositionId is required");
            }

            Position position;
            try
            {
                position = _ordersCache.Positions.GetPositionById(request.PositionId);
            }
            catch (PositionNotFoundException)
            {
                return NotFound($"Position with id {request.PositionId} not found");
            }

            if (position == null)
            {
                return NotFound($"Position with id {request.PositionId} not found");
            }

            if (position.Status != PositionStatus.Closing)
            {
                return BadRequest($"Position with id {request.PositionId} is not in Closing state, it is {position.Status}");
            }

            try
            {
                position.CancelClosing(_dateService.Now());
            }
            catch (StateTransitionNotFoundException)
            {
                return BadRequest($"Couldn't cancel Closing state for position with id {request.PositionId}");
            }

            return Ok($"Position with id {request.PositionId} was successfully put back in Active state");
        }
    }
}
