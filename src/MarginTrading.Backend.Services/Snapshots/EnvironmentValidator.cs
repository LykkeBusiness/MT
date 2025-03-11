using System;
using System.Threading.Tasks;

using Common.Log;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Services.Snapshots;

public class EnvironmentValidator : IEnvironmentValidator
{
    private readonly ISnapshotValidator _snapshotValidator;
    private readonly IMarginTradingBlobRepository _blobRepository;
    private readonly ILog _log;

    public EnvironmentValidator(
        ISnapshotValidator snapshotValidator,
        IMarginTradingBlobRepository blobRepository,
        ILog log)
    {
        _snapshotValidator = snapshotValidator;
        _blobRepository = blobRepository;
        _log = log;
    }

    public async Task<EnvironmentValidationResult> Validate(string correlationId)
    {
        try
        {
            var validationResult = await _snapshotValidator.ValidateCurrentState();

            if (!validationResult.IsValid)
            {
                var errorMessage =
                    $"The trading data snapshot might be corrupted. The current state of orders and positions is incorrect. Check the dbo.BlobData table for more info: container {LykkeConstants.MtCoreSnapshotBlobContainer}, correlationId {correlationId}";
                var ex = new SnapshotValidationException(errorMessage,
                    SnapshotValidationError.InvalidOrderOrPositionState);
                validationResult.Exception = ex;
                await _blobRepository.WriteAsync(LykkeConstants.MtCoreSnapshotBlobContainer, correlationId, validationResult);
            }
            else
            {
                await _log.WriteInfoAsync(nameof(EnvironmentValidator), nameof(Validate),
                    "The current state of orders and positions is correct.");
            }

            return validationResult;
        }
        catch (Exception e)
        {
            var result = new EnvironmentValidationResult
            {
                Exception = new SnapshotValidationException("Snapshot validation failed", SnapshotValidationError.Unknown, e),
            };

            return result;
        }
    }
}
