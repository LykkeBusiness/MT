using System.Threading.Tasks;

using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Services;

public interface ISnapshotValidator
{
    Task<SnapshotValidationResult> Validate(string correlationId);
}
