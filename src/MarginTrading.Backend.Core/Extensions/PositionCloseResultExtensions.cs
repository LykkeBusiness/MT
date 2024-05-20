// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Extensions
{
    public static class PositionCloseResultExtensions
    {
        public static bool IsSuccess(this PositionCloseResult result)
        {
            return result != PositionCloseResult.FailedToClose;
        }

        public static string GetTitle(this PositionCloseResult result)
        {
            return $"Position close result: {result.ToString()}";
        }
    }
}