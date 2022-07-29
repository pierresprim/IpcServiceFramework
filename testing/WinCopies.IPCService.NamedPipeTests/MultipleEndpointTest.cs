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

using AutoFixture.Xunit2;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Moq;

using System.Threading.Tasks;

using WinCopies.IPCService.Client;
using WinCopies.IPCService.Hosting;
using WinCopies.IPCService.NamedPipeTests.Fixtures;
using WinCopies.IPCService.Testing;

using Xunit;

namespace WinCopies.IPCService.NamedPipeTests
{
    public class MultipleEndpointTest
    {
        [Theory, AutoData]
        public async Task MultipleEndpoints(
            Mock<ITestService> service1,
            Mock<ITestService2> service2)
        {
            IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices(services => services
                        .AddScoped(x => service1.Object)
                        .AddScoped(x => service2.Object))
                .ConfigureIPCHost(builder => builder
                        .AddNamedPipeEndpoint<ITestService>("pipe1")
                        .AddNamedPipeEndpoint<ITestService2>("pipe2"))
                .Build();

            await host.StartAsync();

            ServiceProvider clientServiceProvider = new ServiceCollection()
                .AddNamedPipeClient<ITestService>("client1", "pipe1")
                .AddNamedPipeClient<ITestService2>("client2", "pipe2")
                .BuildServiceProvider();

            IClient<ITestService> client1 = clientServiceProvider
                .GetRequiredService<IClientFactory<ITestService>>()
                .CreateClient("client1");

#if !DISABLE_DYNAMIC_CODE_GENERATION
            await client1.InvokeAsync(x => x.ReturnVoid());
            service1.Verify(x => x.ReturnVoid(), Times.Once);
#endif

            await client1.InvokeAsync(TestHelpers.CreateIPCRequest("ReturnVoid"));
#if !DISABLE_DYNAMIC_CODE_GENERATION
            service1.Verify(x => x.ReturnVoid(), Times.Exactly(2));
#else
            service1.Verify(x => x.ReturnVoid(), Times.Once);
#endif

            IClient<ITestService2> client2 = clientServiceProvider
                .GetRequiredService<IClientFactory<ITestService2>>()
                .CreateClient("client2");

#if !DISABLE_DYNAMIC_CODE_GENERATION
            await client2.InvokeAsync(x => x.SomeMethod());
            service2.Verify(x => x.SomeMethod(), Times.Once);
#endif

            await client2.InvokeAsync(TestHelpers.CreateIPCRequest("SomeMethod"));

#if !DISABLE_DYNAMIC_CODE_GENERATION
            service2.Verify(x => x.SomeMethod(), Times.Exactly(2));
#else
            service2.Verify(x => x.SomeMethod(), Times.Once);
#endif
        }
    }
}
