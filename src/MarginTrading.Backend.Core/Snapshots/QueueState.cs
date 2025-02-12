using System.Collections.Generic;

namespace MarginTrading.Backend.Core.Snapshots;

public sealed record QueueState(
    IReadOnlyCollection<SnapshotCreationRequest> PendingRequests,
    SnapshotCreationRequest InFlightRequest);