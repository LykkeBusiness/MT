using System.Threading.Tasks;

using Common;

using MarginTrading.Backend.Core.Snapshots;

using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services.Snapshots;

internal sealed class LoggingEnvironmentValidationStrategy(
    ILogger<LoggingEnvironmentValidationStrategy> logger,
    IEnvironmentValidationStrategy decoratee)
    : IEnvironmentValidationStrategy
{
    public async Task<EnvironmentValidationResult> Validate(string correlationId)
    {
        var result = await decoratee.Validate(correlationId);
        if (!result.IsValid)
        {
            logger.LogCritical(result.Exception, result.ToJson());
        }

        return result;
    }
}