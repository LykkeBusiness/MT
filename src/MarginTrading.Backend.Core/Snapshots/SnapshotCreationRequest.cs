using System;

namespace MarginTrading.Backend.Core.Snapshots;

public sealed record SnapshotCreationRequest(
    Guid Id,
    EnvironmentValidationStrategyType ValidationStrategyType,
    SnapshotStatus Status,
    SnapshotInitiator Initiator,
    DateTimeOffset Timestamp,
    DateTime TradingDay,
    string CorrelationId = null) : IIdentifiable
{
    public static SnapshotCreationRequest Create(
        EnvironmentValidationStrategyType validationStrategyType,
        SnapshotStatus status,
        SnapshotInitiator initiator,
        DateTimeOffset timestamp,
        DateTime tradingDay,
        string correlationId = null)
    {
        return new SnapshotCreationRequest(
            Guid.NewGuid(),
            validationStrategyType,
            status,
            initiator,
            timestamp,
            tradingDay,
            correlationId);
    }

    public static SnapshotCreationRequest CreateDraftRequest(
        EnvironmentValidationStrategyType validationStrategyType,
        SnapshotInitiator initiator,
        DateTimeOffset timestamp,
        DateTime tradingDay,
        string correlationId = null)
    {
        return Create(
            validationStrategyType,
            SnapshotStatus.Draft,
            initiator,
            timestamp,
            tradingDay,
            correlationId);
    }

    public static SnapshotCreationRequest CreateFinalRequest(
        EnvironmentValidationStrategyType validationStrategyType,
        SnapshotInitiator initiator,
        DateTimeOffset timestamp,
        DateTime tradingDay,
        string correlationId = null)
    {
        return Create(
            validationStrategyType,
            SnapshotStatus.Final,
            initiator,
            timestamp,
            tradingDay,
            correlationId);
    }
}