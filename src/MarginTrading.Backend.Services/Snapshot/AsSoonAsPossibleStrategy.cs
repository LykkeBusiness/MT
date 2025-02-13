using System.Threading.Tasks;

using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Services.Snapshot;

public class AsSoonAsPossibleStrategy(IEnvironmentValidator environmentValidator) : IEnvironmentValidationStrategy
{
    public Task<EnvironmentValidationResult> Validate(string correlationId) =>
        environmentValidator.Validate(correlationId);
}
