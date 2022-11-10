// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Core.AccountHistory
{
    public class RestoreResult
    {
        private readonly IDictionary<string, (string, decimal)> _foundPositions;
        private readonly IDictionary<string, (string, decimal)> _notFoundPositions;
        private readonly IList<string> _curlCommands;

        public RestoreResult(RestoreStatus status,
            DateTime date,
            RestoreProgress progress,
            IDictionary<string, (string, decimal)> foundPositions = null,
            IDictionary<string, (string, decimal)> notFoundPositions = null)
        {
            Status = status;
            Date = date;
            Progress = progress;
            Timestamp = DateTime.UtcNow;
            _foundPositions = foundPositions == null || foundPositions.Count == 0 
                ? new Dictionary<string, (string, decimal)>() 
                : new Dictionary<string, (string, decimal)>(foundPositions);
            _notFoundPositions = notFoundPositions == null || notFoundPositions.Count == 0
                ? new Dictionary<string, (string, decimal)>()
                : new Dictionary<string, (string, decimal)>(notFoundPositions);
            _curlCommands = new List<string>();
        }

        [JsonConstructor]
        public RestoreResult(RestoreStatus status,
            DateTime date,
            RestoreProgress progress,
            IDictionary<string, (string, decimal)> foundPositions,
            IDictionary<string, (string, decimal)> notFoundPositions,
            DateTime timestamp,
            IList<string> curlCommands)
        {
            Status = status;
            Date = date;
            Progress = progress;
            Timestamp = timestamp;
            _foundPositions = foundPositions == null || foundPositions.Count == 0 
                ? new Dictionary<string, (string, decimal)>() 
                : new Dictionary<string, (string, decimal)>(foundPositions);
            _notFoundPositions = notFoundPositions == null || notFoundPositions.Count == 0
                ? new Dictionary<string, (string, decimal)>()
                : new Dictionary<string, (string, decimal)>(notFoundPositions);
            _curlCommands = curlCommands == null || curlCommands.Count == 0
                ? new List<string>()
                : new List<string>(curlCommands);
        }

        public RestoreStatus Status { get; set; }
            
        public RestoreProgress Progress { get; set;}
            
        public DateTime Date { get; }
        
        public DateTime Timestamp { get; }
            
        public void Add(string positionId, string accountId, decimal amount)
        {
            _foundPositions.Add(positionId, (accountId, amount));
        }
            
        public void AddNotFound(string positionId, string accountId, decimal amount)
        {
            _notFoundPositions.Add(positionId, (accountId, amount));

            var curlCommand =
                CurlCommandGenerator.Generate(Guid.NewGuid().ToString(), accountId, amount, DateTime.UtcNow);
            _curlCommands.Add(curlCommand);
        }

        public IDictionary<string, (string, decimal)> FoundPositions =>
            new Dictionary<string, (string, decimal)>(_foundPositions);
        public IDictionary<string, (string, decimal)> NotFoundPositions =>
            new Dictionary<string, (string, decimal)>(_notFoundPositions);
        public IList<string> CurlCommands => new List<string>(_curlCommands);
    }
}