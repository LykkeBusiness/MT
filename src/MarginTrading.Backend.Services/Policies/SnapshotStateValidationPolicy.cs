// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Common.Log;
using MarginTrading.Backend.Services.Infrastructure;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace MarginTrading.Backend.Services.Policies
{
    public static class SnapshotStateValidationPolicy
    {
        public static AsyncRetryPolicy BuildPolicy(ILog logger)
        {
            return Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3,
                    x => TimeSpan.FromSeconds(x * 5),
                    (exception, span) => logger.WriteWarningAsync(
                        nameof(SnapshotService),
                        nameof(SnapshotService.MakeTradingDataSnapshot),
                        $"Exception: {exception?.Message}"));
        }
    }
}