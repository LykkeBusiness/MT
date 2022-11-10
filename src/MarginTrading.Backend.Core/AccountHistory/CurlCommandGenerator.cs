// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Core.AccountHistory
{
    public static class CurlCommandGenerator
    {
        private const string BaseUrl = "http://am-host-url-to-provide";
        private const string ReasonFmt = "BUGS-2826 compensation upon position [{0}]";
        private const string Currency = "EUR";

        public static string Generate(string operationId,
            string accountId,
            string positionId,
            decimal amount,
            DateTime tradingDay)
        {
            if (string.IsNullOrEmpty(operationId))
                throw new ArgumentNullException(nameof(operationId));
            if (string.IsNullOrEmpty(accountId))
                throw new ArgumentNullException(nameof(accountId));
            if (amount == 0)
                throw new ArgumentException("Amount should be non-zero");

            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json-patch+json" },
                { "Accept", "text/plain" }
            };

            var headersString = string.Join(" ", headers.Select(x => $"-H '{x.Key}: {x.Value}'"));

            var body = new
            {
                OperationId = operationId,
                AmountDelta = Math.Abs(amount),
                Reason = string.Format(ReasonFmt, positionId),
                AdditionalInfo = "",
                AssetPairId = Currency,
                TradingDay = tradingDay
            };

            var bodyString = $"-d '{JsonConvert.SerializeObject(body)}'";

            var operationString = amount > 0 ? "deposit" : "withdraw";
            var urlString = $"POST '{BaseUrl}/api/accounts/{accountId}/balance/{operationString}'";

            return $"curl -X {urlString} {headersString} {bodyString}";
        }
    }
}