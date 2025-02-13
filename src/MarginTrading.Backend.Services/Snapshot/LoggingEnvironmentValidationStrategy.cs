using System.Threading.Tasks;

using Common;

using MarginTrading.Backend.Core.Snapshots;

using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services.Snapshot;

public class LoggingEnvironmentValidationStrategy(
    ILogger<LoggingEnvironmentValidationStrategy> logger,
    IEnvironmentValidationStrategy decoratee)
    : IEnvironmentValidationStrategy
{
    private readonly IEnvironmentValidationStrategy _decoratee = decoratee;
    private readonly ILogger<LoggingEnvironmentValidationStrategy> _logger = logger;

    public async Task<EnvironmentValidationResult> Validate(string correlationId)
    {
        var result = await _decoratee.Validate(correlationId);
        if (!result.IsValid)
        {
            _logger.LogWarning(result.Exception, result.ToJson());
        }

        return result;
    }
}