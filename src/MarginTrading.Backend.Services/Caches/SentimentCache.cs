// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Common;
using Common.Log;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Services.Caches
{
    public interface ISentimentCache
    {
        void Subscribe(IObservable<Position> provider);
    }
    
    public class SentimentCache : ISentimentCache, IObserver<Position>
    {
        private IDisposable _unsubscriber;
        private readonly ILog _log;

        public SentimentCache(ILog log)
        {
            _log = log;
        }

        public void Subscribe(IObservable<Position> provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            _unsubscriber = provider.Subscribe(this);
        }

        public void OnCompleted()
        {
            _log.WriteInfo(nameof(OnCompleted), null, "Unsubscribing from positions");
            _unsubscriber.Dispose();
        }

        public void OnError(Exception error)
        {
            _log.WriteError(nameof(OnError), null, error);
        }

        public void OnNext(Position value)
        {
            _log.WriteInfo(nameof(OnNext), value.ToJson(), "Got position from core");
        }
    }
}