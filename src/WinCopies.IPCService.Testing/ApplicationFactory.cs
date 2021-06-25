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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Moq;

using System;

using WinCopies.IPCService.Client;

namespace WinCopies.IPCService.Testing
{
    public class ApplicationFactory<TContract> : System.IDisposable where TContract : class
    {
        private Func<IServiceProvider, TContract> _serviceFactory = _ => new Mock<TContract>().Object;
        private Action<Hosting.IHostBuilder> _ipcHostConfig = _ => { };
        private IHost _host = null;
        private bool _isDisposed = false;

        public ApplicationFactory<TContract> WithServiceImplementation(TContract serviceInstance)
        {
            _serviceFactory = _ => serviceInstance;

            return this;
        }

        public ApplicationFactory<TContract> WithServiceImplementation(Func<IServiceProvider, TContract> serviceFactory)
        {
            _serviceFactory = serviceFactory;

            return this;
        }

        public ApplicationFactory<TContract> WithIPCHostConfiguration(Action<Hosting.IHostBuilder> ipcHostConfig)
        {
            _ipcHostConfig = ipcHostConfig;

            return this;
        }

        public IClient<TContract> CreateClient(Action<string, IServiceCollection> clientConfig)
        {
            if (clientConfig is null)

                throw new ArgumentNullException(nameof(clientConfig));

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(x => x.TryAddScoped(_serviceFactory))
                .ConfigureIPCHost(_ipcHostConfig)
                .Build();

            _host.StartAsync().Wait();

            string clientName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            clientConfig.Invoke(clientName, services);

            return services.BuildServiceProvider()
                .GetRequiredService<IClientFactory<TContract>>()
                .CreateClient(clientName);
        }

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)

                return;

            if (disposing)

                _host?.Dispose();

            _isDisposed = true;
        }
    }
}
