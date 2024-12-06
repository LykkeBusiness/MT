// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class SnapshotValidationException : ValidationException<SnapshotValidationError>
    {
        public SnapshotValidationException(SnapshotValidationError errorCode) : base(errorCode)
        {
        }

        public SnapshotValidationException(string message, SnapshotValidationError errorCode) : base(message, errorCode)
        {
        }

        public SnapshotValidationException(string message, SnapshotValidationError errorCode, Exception innerException) : base(message, errorCode, innerException)
        {
        }
    }
}