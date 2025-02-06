// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using Common.Log;

using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Services.Infrastructure;

using Polly;
using Polly.Retry;

namespace MarginTrading.Backend.Services.Policies
{
    public static class SnapshotStateValidationPolicy
    {
        public static AsyncRetryPolicy<SnapshotValidationResult> BuildPolicy(ILog logger)
        {
            return Policy
                .HandleResult<SnapshotValidationResult>(x => !x.IsValid)
                .WaitAndRetryAsync(3,
                    x => TimeSpan.FromSeconds(x * 5),
                    (result, span) => logger?.WriteWarningAsync(
                                          nameof(SnapshotBuilder),
                                          nameof(SnapshotBuilder.MakeTradingDataSnapshot),
                                          $"Exception: {result?.Result?.Exception}",
                                          result?.Result?.Exception)
                                      // in case logger is not provided
                                      ?? Task.CompletedTask);
        }
    }
}