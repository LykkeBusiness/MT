using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Snapshots;

public interface IEnvironmentValidationStrategy
{
    Task<EnvironmentValidationResult> Validate(string correlationId);
}