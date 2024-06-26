// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Reflection;
using MarginTrading.Backend.Contracts.ErrorCodes;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Extensions;

namespace MarginTrading.Backend.Exceptions
{
    public static class PublicErrorCodeMap
    {
        private const string UnknownError = "Unknown Error"; 
        public const string UnsupportedError = "Unsupported Error"; 
        
        /// <summary>
        /// Maps <see cref="AccountValidationError"/> to public error code.
        /// </summary>
        /// <param name="source"></param>
        /// <returns>Public error code or <see cref="UnknownError" /> if mapping is not possible.</returns>
        public static string Map(AccountValidationError source) =>
            source switch
            {
                AccountValidationError.None => string.Empty,
                AccountValidationError.AccountDoesNotExist => ValidationErrorCodes.AccountDoesNotExist,
                AccountValidationError.AccountDisabled => ValidationErrorCodes.AccountDisabled,
                AccountValidationError.AccountMismatch => ValidationErrorCodes.AccountMismatch,
                AccountValidationError.AccountEmpty => ValidationErrorCodes.AccountEmpty,
                _ => UnknownError
            };

        /// <summary>
        /// Maps <see cref="InstrumentValidationError"/> to public error code.
        /// </summary>
        /// <param name="source"></param>
        /// <returns>Public error code or <see cref="UnknownError" /> if mapping is not possible.</returns>
        public static string Map(InstrumentValidationError source) =>
            source switch
            {
                InstrumentValidationError.None => string.Empty,
                InstrumentValidationError.InstrumentTradingDisabled => ValidationErrorCodes.InstrumentTradingDisabled,
                InstrumentValidationError.TradesAreNotAvailable => ValidationErrorCodes.TradesAreNotAvailable,
                InstrumentValidationError.NoLiquidity => ValidationErrorCodes.InstrumentNoLiquidity,
                _ => UnknownError
            };

        /// <summary>
        /// Maps <see cref="OrderRejectReason"/> to public error code.
        /// </summary>
        /// <param name="source"></param>
        /// <returns>
        /// Public error code or <see cref="UnsupportedError" /> if mapping is not possible.
        /// </returns>
        public static string Map(OrderRejectReason source) =>
            source switch
            {
                OrderRejectReason.InstrumentTradingDisabled => ValidationErrorCodes.InstrumentTradingDisabled,
                OrderRejectReason.NoLiquidity => ValidationErrorCodes.InstrumentNoLiquidity,
                OrderRejectReason.MaxPositionNotionalLimit => ValidationErrorCodes.MaxPositionNotionalLimit,
                _ => ValidationErrorCodes.InvalidInstrument
            };

        /// <summary>
        /// Maps <see cref="PositionValidationError"/> to public error code.
        /// </summary>
        /// <param name="source"></param>
        /// <returns>Public error code or <see cref="UnknownError" /> if mapping is not possible.</returns>
        public static string Map(PositionValidationError source) =>
            source switch
            {
                PositionValidationError.None => string.Empty,
                PositionValidationError.PositionNotFound => ValidationErrorCodes.PositionNotFound,
                PositionValidationError.InvalidStatusWhenRunSpecialLiquidation => 
                    ValidationErrorCodes.PositionInvalidStatusSpecialLiquidation,
                _ => UnknownError
            };

        /// <summary>
        /// Maps <see cref="OrderValidationError"/> to public error code.
        /// </summary>
        /// <param name="source"></param>
        /// <returns>Public error code or <see cref="UnknownError" /> if mapping is not possible.</returns>
        public static string Map(OrderValidationError source) =>
            source switch
            {
                OrderValidationError.None => string.Empty,
                OrderValidationError.OrderNotFound => ValidationErrorCodes.OrderNotFound,
                OrderValidationError.IncorrectStatusWhenCancel => ValidationErrorCodes.OrderIncorrectStatus,
                _ => UnknownError
            };

        /// <summary>
        /// Maps <see cref="PositionGroupValidationError"/> to public error code.
        /// </summary>
        /// <param name="source"></param>
        /// <returns>Public error code or <see cref="UnknownError" /> if mapping is not possible.</returns>
        public static string Map(PositionGroupValidationError source) =>
            source switch
            {
                PositionGroupValidationError.None => string.Empty,
                PositionGroupValidationError.DirectionEmpty => ValidationErrorCodes.PositionGroupDirectionEmpty,
                PositionGroupValidationError.MultipleAccounts => ValidationErrorCodes.PositionGroupMultipleAccounts,
                PositionGroupValidationError.MultipleInstruments => 
                    ValidationErrorCodes.PositionGroupMultipleInstruments,
                PositionGroupValidationError.MultipleDirections => ValidationErrorCodes.PositionGroupMultipleDirections,
                _ => UnknownError
            };

        public static string MapFromValidationExceptionOrRaise(Exception exception)
        {
            var errorType = exception.GetValidationErrorType();
            if (errorType == null)
            {
                throw new InvalidOperationException("Validation exception does not have error type parameter");
            }
            
            var errorValue = exception.GetValidationErrorValue();
            if (errorValue == null)
            {
                throw new InvalidOperationException("Validation exception does not have error value");
            }
            
            var publicErrorCode = FindMapMethod(errorType)?.Invoke(null, new[] {errorValue});
            
            return publicErrorCode?.ToString() ?? string.Empty;
        }

        private static MethodInfo? FindMapMethod(Type mapErrorType)
        {
            var method = typeof(PublicErrorCodeMap)
                .GetMethod("Map", BindingFlags.Static | BindingFlags.Public, new[] { mapErrorType });

            return method;
        }
    }
}