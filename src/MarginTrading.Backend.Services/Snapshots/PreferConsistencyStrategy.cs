using System;
using System.Threading.Tasks;

using MarginTrading.Backend.Core.Snapshots;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

namespace MarginTrading.Backend.Services.Snapshots;

public class PreferConsistencyStrategy(
    IEnvironmentValidator environmentValidator,
    ILogger<PreferConsistencyStrategy> logger) : IEnvironmentValidationStrategy
{
    private readonly IEnvironmentValidator _environmentValidator = environmentValidator;
    private readonly AsyncRetryPolicy<EnvironmentValidationResult> _policy =
        Policy
            .HandleResult<EnvironmentValidationResult>(x => !x.IsValid)
            .WaitAndRetryAsync(3,
                x => TimeSpan.FromSeconds(x * 5),
                    (result, span) => logger.LogWarning("Exception: {Exception}", result?.Result?.Exception));
    public Task<EnvironmentValidationResult> Validate(string correlationId) =>
        _policy.ExecuteAsync(() => _environmentValidator.Validate(correlationId));
}