using System;
using System.Threading.Tasks;

using Common.Log;

using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Snapshots;

namespace MarginTrading.Backend.Services;

public class SnapshotValidator : ISnapshotValidator
{
    private readonly ISnapshotValidationService _snapshotValidationService;
    private readonly IMarginTradingBlobRepository _blobRepository;
    private readonly ILog _log;

    public SnapshotValidator(
        ISnapshotValidationService snapshotValidationService,
        IMarginTradingBlobRepository blobRepository,
        ILog log)
    {
        _snapshotValidationService = snapshotValidationService;
        _blobRepository = blobRepository;
        _log = log;
    }

    public async Task<SnapshotValidationResult> Validate(string correlationId)
    {
        try
        {
            var validationResult = await _snapshotValidationService.ValidateCurrentStateAsync();

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
                await _log.WriteInfoAsync(nameof(SnapshotValidator), nameof(Validate),
                    "The current state of orders and positions is correct.");
            }

            return validationResult;
        }
        catch (Exception e)
        {
            var result = new SnapshotValidationResult
            {
                Exception = new SnapshotValidationException("Snapshot validation failed", SnapshotValidationError.Unknown, e),
            };

            return result;
        }
    }
}
