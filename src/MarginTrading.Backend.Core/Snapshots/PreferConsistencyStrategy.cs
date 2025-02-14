using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

namespace MarginTrading.Backend.Core.Snapshots;

public class PreferConsistencyStrategy(
    IEnvironmentValidator environmentValidator,
    ILogger<PreferConsistencyStrategy> logger,
    TimeSpan? retryInterval = null) : IEnvironmentValidationStrategy
{
    private readonly AsyncRetryPolicy<EnvironmentValidationResult> _policy =
        Policy
            .HandleResult<EnvironmentValidationResult>(x => !x.IsValid)
            .WaitAndRetryAsync(3,
                x => retryInterval ?? TimeSpan.FromSeconds(x * 5),
                    (result, span) => logger.LogWarning("Exception: {Exception}", result?.Result?.Exception));

    public Task<EnvironmentValidationResult> Validate(string correlationId) =>
        _policy.ExecuteAsync(() => environmentValidator.Validate(correlationId));
}