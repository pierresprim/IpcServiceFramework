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

using System;
using System.Collections.Generic;
using System.Linq;

namespace WinCopies.IPCService.Client
{
    internal class ClientFactory<TContract, TClientOptions> : IClientFactory<TContract> where TContract : class where TClientOptions : ClientOptions
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumerable<ClientRegistration<TContract, TClientOptions>> _registrations;

        public ClientFactory(IServiceProvider serviceProvider, IEnumerable<ClientRegistration<TContract, TClientOptions>> registrations)
        {
            _serviceProvider = serviceProvider;
            _registrations = registrations;
        }

        public IClient<TContract> CreateClient(string name)
        {
            ClientRegistration<TContract, TClientOptions> registration = _registrations.FirstOrDefault(x => x.Name == name);

            if (registration == null)

                throw new ArgumentException($"IPC client '{name}' is not configured.", nameof(name));

            using (IServiceScope scope = _serviceProvider.CreateScope())

                return registration.CreateClient(scope.ServiceProvider);
        }
    }
}
