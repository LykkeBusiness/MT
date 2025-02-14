using System.Collections.Generic;

namespace MarginTrading.Backend.Core.Snapshots;

public sealed record SnapshotQueueState(
    IReadOnlyCollection<SnapshotCreationRequest> PendingRequests,
    SnapshotCreationRequest InFlightRequest);