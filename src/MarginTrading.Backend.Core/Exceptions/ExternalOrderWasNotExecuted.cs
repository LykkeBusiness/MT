// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Contracts.ExchangeConnector;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class ExternalOrderWasNotExecuted : Exception
    {
        public ExternalOrderWasNotExecuted(ExecutionReport executionResult) : base($"External order was not executed. Status: {executionResult.ExecutionStatus}. Failure: {executionResult.FailureType}")
        {
            
        }
    }
}