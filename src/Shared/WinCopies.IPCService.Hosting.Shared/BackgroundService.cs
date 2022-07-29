/* MIT License

Copyright (c) 2018 Jacques Kang Copyright (c) 2021 Pierre Sprimont

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

using Microsoft.Extensions.Logging;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WinCopies.IPCService.Hosting
{
    public sealed class BackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IEnumerable<IEndpoint> _endpoints;
        private readonly ILogger<BackgroundService> _logger;

        public BackgroundService(IEnumerable<IEndpoint> endpoints, ILogger<BackgroundService> logger)
        {
            _endpoints = endpoints ?? throw new System.ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.WhenAll(_endpoints.Select(x => x.ExecuteAsync(stoppingToken)));

        public override void Dispose()
        {
            foreach (IEndpoint endpoint in _endpoints)

                endpoint.Dispose();

            base.Dispose();

            _logger.LogInformation(Properties.Resources.IPCBackgroundServiceDisposed);
        }
    }
}
