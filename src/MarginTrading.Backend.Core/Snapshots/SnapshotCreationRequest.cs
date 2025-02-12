using System;

namespace MarginTrading.Backend.Core.Snapshots;

public sealed record SnapshotCreationRequest(
    Guid Id,
    SnapshotStrategy
    Strategy,
    SnapshotStatus Status,
    DateTimeOffset Timestamp,
    Guid? CorrelationId = null);