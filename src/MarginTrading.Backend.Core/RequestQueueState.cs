using System.Collections.Generic;

namespace MarginTrading.Backend.Core;

public sealed record RequestQueueState<T>(IReadOnlyCollection<T> PendingRequests, T InFlightRequest);