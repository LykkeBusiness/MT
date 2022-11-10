// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MarginTrading.Backend.Core.AccountHistory
{
    public class RestoreResult
    {
        private readonly IDictionary<string, (string, decimal)> _foundPositions;
            
        private readonly IDictionary<string, (string, decimal)> _notFoundPositions;

        public RestoreResult(RestoreStatus status,
            DateTime date,
            RestoreProgress progress,
            IDictionary<string, (string, decimal)> foundPositions = null,
            IDictionary<string, (string, decimal)> notFoundPositions = null)
        {
            Status = status;
            Date = date;
            Progress = progress;
            _foundPositions = foundPositions == null || foundPositions.Count == 0 
                ? new Dictionary<string, (string, decimal)>() 
                : new Dictionary<string, (string, decimal)>(foundPositions);
            _notFoundPositions = notFoundPositions == null || notFoundPositions.Count == 0
                ? new Dictionary<string, (string, decimal)>()
                : new Dictionary<string, (string, decimal)>(notFoundPositions);
        }

        public RestoreStatus Status { get; set; }
            
        public RestoreProgress Progress { get; set;}
            
        public DateTime Date { get; }
            
        public void Add(string positionId, string accountId, decimal amount)
        {
            _foundPositions.Add(positionId, (accountId, amount));
        }
            
        public void AddNotFound(string positionId, string accountId, decimal amount)
        {
            _notFoundPositions.Add(positionId, (accountId, amount));
        }

        public IDictionary<string, (string, decimal)> FoundPositions =>
            new Dictionary<string, (string, decimal)>(_foundPositions);
        public IDictionary<string, (string, decimal)> NotFoundPositions =>
            new Dictionary<string, (string, decimal)>(_notFoundPositions);
    }
}