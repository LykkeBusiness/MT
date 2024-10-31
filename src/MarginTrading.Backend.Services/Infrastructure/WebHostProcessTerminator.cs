// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public sealed class WebHostProcessTerminator : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        public WebHostProcessTerminator()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public void TerminateProcess()
        {
            _cancellationTokenSource?.Cancel();
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }
    }
}