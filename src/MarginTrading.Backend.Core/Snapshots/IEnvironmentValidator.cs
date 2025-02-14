using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Snapshots;

public interface IEnvironmentValidator
{
    /// <summary>
    /// Checks the environment consistency before creating the snapshot
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns></returns>
    Task<EnvironmentValidationResult> Validate(string correlationId);
}
