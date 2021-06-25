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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using WinCopies.IPCService.Client;
using WinCopies.IPCService.NamedPipeTests.Fixtures;
using WinCopies.IPCService.Testing;

using Xunit;


namespace WinCopies.IPCService.NamedPipeTests
{
    public class EdgeCaseTest : IClassFixture<ApplicationFactory<ITestService>>
    {
        private readonly ApplicationFactory<ITestService> _factory;

        public EdgeCaseTest(ApplicationFactory<ITestService> factory) => _factory = factory;

        [Fact]
        public async Task ServerIsOff_Timeout()
        {
            int timeout = 1000; // 1s

            IClient<ITestService> client = _factory
                .CreateClient((name, services) =>
                {
                    services.AddNamedPipeClient<ITestService>(name, (_, options) =>
                    {
                        options.PipeName = "inexisted-pipe";
                        options.ConnectionTimeout = timeout;
                    });
                });

#if !DISABLE_DYNAMIC_CODE_GENERATION
            var sw = Stopwatch.StartNew();

            await Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                string output = await client.InvokeAsync(x => x.StringType("abc"));
            });

            Assert.True(sw.ElapsedMilliseconds < timeout * 2); // makesure timeout works with marge
#endif

            var sw2 = Stopwatch.StartNew();

            await Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                string output = await client.InvokeAsync<string>(TestHelpers.CreateIPCRequest(typeof(ITestService), "StringType", new object[] { "abc" }));
            });

            Assert.True(sw2.ElapsedMilliseconds < timeout * 2); // makesure timeout works with marge
        }

        [Fact]
        public void ConnectionCancelled_Throw()
        {
            IClient<ITestService> client = _factory
                .CreateClient((name, services) => services.AddNamedPipeClient<ITestService>(name, (_, options) =>
{
    options.PipeName = "inexisted-pipe";
    options.ConnectionTimeout = Timeout.Infinite;
}));

#if !DISABLE_DYNAMIC_CODE_GENERATION
            using (var cts = new CancellationTokenSource())

                Task.WaitAll(
                    Task.Run(async () => await Assert.ThrowsAsync<OperationCanceledException>(async () => await client.InvokeAsync(x => x.ReturnVoid(), cts.Token))),
                    Task.Run(() => cts.CancelAfter(1000)));
#endif

            using (var cts = new CancellationTokenSource())
            {
                Task.WaitAll(
                    Task.Run(async () => await Assert.ThrowsAsync<OperationCanceledException>(async () =>
{
    var request = TestHelpers.CreateIPCRequest("ReturnVoid");
    await client.InvokeAsync(request, cts.Token);
})),
                    Task.Run(() => cts.CancelAfter(1000)));
            }
        }
    }
}
