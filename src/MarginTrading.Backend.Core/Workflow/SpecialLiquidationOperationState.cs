// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Core
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SpecialLiquidationOperationState
    {
        Initiated = 0,
        Started = 1,
        PriceRequested = 2,
        PriceReceived = 3,
        ExternalOrderExecuted = 4,
        InternalOrderExecutionStarted = 5,
        InternalOrdersExecuted = 6,
        Finished = 7,
        OnTheWayToFail = 8,
        Failed = 9,
        Cancelled = 10,
    }
}