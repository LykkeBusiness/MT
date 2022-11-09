// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MarginTrading.Backend.Core.AccountHistory
{
    public class RestoreResult
    {
        private readonly IDictionary<string, decimal> _foundPositions;
            
        private readonly IDictionary<string, decimal> _notFoundPositions;

        public RestoreResult(RestoreStatus status,
            DateTime date,
            RestoreProgress progress,
            IDictionary<string, decimal> found = null,
            IDictionary<string, decimal> notFound = null)
        {
            Status = status;
            Date = date;
            Progress = progress;
            _foundPositions = found == null || found.Count == 0 
                ? new Dictionary<string, decimal>() 
                : new Dictionary<string, decimal>(found);
            _notFoundPositions = notFound == null || notFound.Count == 0
                ? new Dictionary<string, decimal>()
                : new Dictionary<string, decimal>(notFound);
        }

        public RestoreStatus Status { get; set; }
            
        public RestoreProgress Progress { get; set;}
            
        public DateTime Date { get; }
            
        public void AddProcessed(string positionId, decimal amount)
        {
            _foundPositions.Add(positionId, amount);
        }
            
        public void AddNotFound(string positionId, decimal amount)
        {
            _notFoundPositions.Add(positionId, amount);
        }

        public IDictionary<string, decimal> FoundPositions =>
            new Dictionary<string, decimal>(_foundPositions);
        public IDictionary<string, decimal> NotFoundPositions =>
            new Dictionary<string, decimal>(_notFoundPositions);
    }
}