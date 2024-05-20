// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core
{
    public readonly struct RunningLiquidation
    {
        public RunningLiquidation(string operationId, string accountId)
        {
            OperationId = operationId;
            AccountId = accountId;
        }

        public string OperationId { get; }
            
        public string AccountId { get; }
    }
}