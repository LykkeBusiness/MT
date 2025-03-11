using System.Threading.Tasks;

using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTradingTests.SnapshotBuilderServiceTests;

class FailingEnvironmentValidator : IEnvironmentValidator
{
    public Task<EnvironmentValidationResult> Validate(string correlationId) =>
        Task.FromResult(
            new EnvironmentValidationResult
            {
                Exception = new SnapshotValidationException("Validation failed under test", SnapshotValidationError.Unknown),
                Cache = new InMemorySnapshot([], [])
            });
}
