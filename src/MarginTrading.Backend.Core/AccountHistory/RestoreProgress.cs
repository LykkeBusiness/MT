// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.AccountHistory
{
    public class RestoreProgress
    {
        public RestoreProgress(int total, int processed)
        {
            Total = total;
            Processed = processed;
        }

        public int Total { get; }
        public int Processed { get; }
    }
}