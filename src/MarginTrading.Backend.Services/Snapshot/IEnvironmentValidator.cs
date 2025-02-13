using System.Threading.Tasks;

using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Services.Snapshot;


// todo: remove or unit with strategy interface?
public interface IEnvironmentValidator
{
    /// <summary>
    /// Checks the environment consistency before creating the snapshot
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns></returns>
    Task<EnvironmentValidationResult> Validate(string correlationId);
}
