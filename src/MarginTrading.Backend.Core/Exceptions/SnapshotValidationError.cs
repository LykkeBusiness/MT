// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.Exceptions
{
    public enum SnapshotValidationError
    {
        None = 0,
        Unknown = 1,
        InvalidOrderOrPositionState = 2,
    }
}