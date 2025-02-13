using System.Threading.Tasks;

using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Services.Snapshot;

public class AsSoonAsPossibleStrategy(IEnvironmentValidator environmentValidator) : IEnvironmentValidationStrategy
{
    private readonly IEnvironmentValidator _environmentValidator = environmentValidator;

    public Task<EnvironmentValidationResult> Validate(string correlationId) =>
        _environmentValidator.Validate(correlationId);
}
