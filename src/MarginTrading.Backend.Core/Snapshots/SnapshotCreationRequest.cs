using System;

namespace MarginTrading.Backend.Core.Snapshots;

public sealed record SnapshotCreationRequest(
    Guid Id,
    EnvironmentValidationStrategyType ValidationStrategyType,
    SnapshotStatus Status,
    SnapshotInitiator Initiator,
    DateTimeOffset Timestamp,
    DateTime TradingDay,
    string CorrelationId = null);