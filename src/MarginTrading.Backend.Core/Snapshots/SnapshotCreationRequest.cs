using System;

namespace MarginTrading.Backend.Core.Snapshots;

public sealed record SnapshotCreationRequest(
    Guid Id,
    EnvironmentValidationStrategyType ValidationStrategyType,
    SnapshotStatus Status,
    DateTimeOffset Timestamp,
    DateTime TradingDay,
    string CorrelationId = null);