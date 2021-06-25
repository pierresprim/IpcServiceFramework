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

using System;

using WinCopies.IPCService.Client;
using WinCopies.IPCService.Client.NamedPipe;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NamedPipeClientServiceCollectionExtensions
    {
        public static IServiceCollection AddNamedPipeClient<TContract>(this IServiceCollection services, string name, string pipeName) where TContract : class => services.AddNamedPipeClient<TContract>(name, (_, options) => options.PipeName = pipeName);

        public static IServiceCollection AddNamedPipeClient<TContract>(this IServiceCollection services, string name, Action<IServiceProvider, NamedPipeClientOptions> configureOptions) where TContract : class
        {
            _ = services.AddClient(new ClientRegistration<TContract, NamedPipeClientOptions>(name, (_, options) => new NamedPipeClient<TContract>(name, options), configureOptions));

            return services;
        }
    }
}
