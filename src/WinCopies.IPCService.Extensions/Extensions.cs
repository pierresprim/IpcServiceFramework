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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using WinCopies.IPCService.Client;
using WinCopies.IPCService.Hosting;

namespace WinCopies.IPCService.Extensions
{
    public interface ISingleInstanceApp<TIn, TOut>
    {
        string GetPipeName();

        string GetClientName();

        /// <seealso cref="Thread(ThreadStart, int)"/>
        ThreadStart GetThreadStart(out int maxStackSize);

        Expression<Func<TIn, TOut>> GetExpression();

        Expression<Func<TIn, Task<TOut>>> GetAsyncExpression();

        CancellationToken? GetCancellationToken();
    }

    public static class Extensions
    {
        public static async Task StartThread(ThreadStart threadStart, int maxStackSize)
        {
            var t = new Thread(threadStart, maxStackSize);

            t.SetApartmentState(ApartmentState.STA);

            t.Start();
        }

        public static async Task<(Mutex mutex, bool mutexExists, NullableGeneric<TOut> serverResult)> StartInstanceAsync<TInterface, TClass, TOut>(this ISingleInstanceApp<TInterface, TOut> app) where TInterface : class where TClass : class, TInterface
        {
            string pipeName = app.GetPipeName();

            (Mutex mutex, bool mutexExists, NullableGeneric<TOut> serverResult) result;

            result.mutex = new Mutex(false, pipeName);

            result.mutexExists = false;

            result.serverResult = null;

            bool rethrow = false;

            try
            {
                void _throw(in System.Exception exception)
                {
                    rethrow = true;

                    throw exception;
                }

                if (result.mutex.WaitOne(0))
                {
                    ThreadStart threadStart = app.GetThreadStart(out int maxStackSize);

                    if (threadStart == null)

                        _throw(new InvalidOperationException($"{nameof(app)}.{nameof(app.GetThreadStart)} returned null."));

                    await StartThread(threadStart, maxStackSize);

                    Host.CreateDefaultBuilder()
                       .ConfigureServices(services => services.AddScoped<TInterface, TClass>())
                       .ConfigureIPCHost(builder => builder.AddNamedPipeEndpoint<TInterface>(pipeName))
                       .ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Debug)).Build().Run();
                }

                else
                {
                    result.mutexExists = true;

                    string clientName = app.GetClientName();

                    ServiceProvider serviceProvider = new ServiceCollection()
                        .AddNamedPipeClient<TInterface>(clientName, pipeName: pipeName)
                        .BuildServiceProvider();

                    IClientFactory<TInterface> clientFactory = serviceProvider.GetRequiredService<IClientFactory<TInterface>>();

                    IClient<TInterface> client = clientFactory.CreateClient(clientName);

                    object expression = app.GetExpression();

                    CancellationToken? cancellationToken = app.GetCancellationToken();

                    if (expression == null)
                    {
                        expression = app.GetAsyncExpression();

                        if (expression == null)

                            _throw(new InvalidOperationException("No expression could be retrieved."));

                        else
                        {
                            async Task<TOut> getValueAsync()
                            {
                                var _expression = (Expression<Func<TInterface, Task<TOut>>>)expression;

                                return cancellationToken.HasValue ? await client.InvokeAsync(_expression, cancellationToken.Value) : await client.InvokeAsync(_expression);
                            }

                            result.serverResult = new NullableGeneric<TOut>(await getValueAsync());
                        }
                    }

                    else
                    {
                        async Task<TOut> getValueAsync()
                        {
                            var _expression = (Expression<Func<TInterface, TOut>>)expression;

                            return cancellationToken.HasValue ? await client.InvokeAsync(_expression, cancellationToken.Value) : await client.InvokeAsync(_expression);
                        }

                        result.serverResult = new NullableGeneric<TOut>(await getValueAsync());
                    }
                }
            }

            catch
            {
                if (rethrow)

                    throw;
            }

            return result;
        }
    }
}
