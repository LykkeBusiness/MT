// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Contracts.ErrorCodes;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Exceptions
{
    public static class PublicErrorCodeMap
    {
        public const string UnknownError = "Unknown Error"; 
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
                InstrumentValidationError.NoLiquidity => ValidationErrorCodes.NoLiquidity,
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
                OrderRejectReason.NoLiquidity => ValidationErrorCodes.NoLiquidity,
                _ => UnsupportedError
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
                _ => UnknownError
            };
    }
}