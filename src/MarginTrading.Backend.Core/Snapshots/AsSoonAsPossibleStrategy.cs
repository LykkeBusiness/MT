using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Snapshots;

public class AsSoonAsPossibleStrategy(IEnvironmentValidator environmentValidator) : IEnvironmentValidationStrategy
{
    public Task<EnvironmentValidationResult> Validate(string correlationId) =>
        environmentValidator.Validate(correlationId);
}
