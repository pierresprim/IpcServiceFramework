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

using Moq;

using System.Threading.Tasks;

using WinCopies.IPCService.Client;
using WinCopies.IPCService.Hosting;
using WinCopies.IPCService.NamedPipeTests.Fixtures;
using WinCopies.IPCService.Testing;

using Xunit;


namespace WinCopies.IPCService.NamedPipeTests
{
    public class ErrorTest : IClassFixture<ApplicationFactory<ITestService>>
    {
        private readonly Mock<ITestService> _serviceMock = new Mock<ITestService>();
        private readonly ApplicationFactory<ITestService> _factory;

        public ErrorTest(ApplicationFactory<ITestService> factory) => _factory = factory.WithServiceImplementation(_ => _serviceMock.Object);

        [Theory, AutoData]
        public async Task Exception_ThrowWithoutDetails(string pipeName, string details)
        {
            _serviceMock.Setup(x => x.ThrowException())
                .Throws(new System.Exception(details));

            IClient<ITestService> client = _factory
                .WithIPCHostConfiguration(hostBuilder =>
                {
                    hostBuilder.AddNamedPipeEndpoint<ITestService>(options =>
                    {
                        options.PipeName = pipeName;
                        options.IncludeFailureDetailsInResponse = false;
                    });
                })
                .CreateClient((name, services) =>
                {
                    services.AddNamedPipeClient<ITestService>(name, (_, options) =>
                    {
                        options.PipeName = pipeName;
                    });
                });

#if !DISABLE_DYNAMIC_CODE_GENERATION
            FaultException actual = await Assert.ThrowsAsync<FaultException>(async () =>
            {
                await client.InvokeAsync(x => x.ThrowException());
            });

            Assert.Null(actual.InnerException);
#endif

            FaultException actual2 = await Assert.ThrowsAsync<FaultException>(async () =>
            {
                var request = TestHelpers.CreateIPCRequest("ThrowException");
                await client.InvokeAsync(request);
            });

            Assert.Null(actual2.InnerException);
        }

        [Theory, AutoData]
        public async Task Exception_ThrowWithDetails(string pipeName, string details)
        {
            _serviceMock.Setup(x => x.ThrowException())
                .Throws(new System.Exception(details));

            IClient<ITestService> client = _factory
                .WithIPCHostConfiguration(hostBuilder =>
                {
                    hostBuilder.AddNamedPipeEndpoint<ITestService>(options =>
                    {
                        options.PipeName = pipeName;
                        options.IncludeFailureDetailsInResponse = true;
                    });
                })
                .CreateClient((name, services) =>
                {
                    services.AddNamedPipeClient<ITestService>(name, (_, options) =>
                    {
                        options.PipeName = pipeName;
                    });
                });

#if !DISABLE_DYNAMIC_CODE_GENERATION
            FaultException actual = await Assert.ThrowsAsync<FaultException>(async () =>
            {
                await client.InvokeAsync(x => x.ThrowException());
            });

            Assert.NotNull(actual.InnerException);
            Assert.NotNull(actual.InnerException.InnerException);
            Assert.Equal(details, actual.InnerException.InnerException.Message);
#endif

            FaultException actual2 = await Assert.ThrowsAsync<FaultException>(async () =>
            {
                var request = TestHelpers.CreateIPCRequest("ThrowException");
                await client.InvokeAsync(request);
            });

            Assert.NotNull(actual2.InnerException);
            Assert.NotNull(actual2.InnerException.InnerException);
            Assert.Equal(details, actual2.InnerException.InnerException.Message);
        }

        [Theory, AutoData]
        public async Task UnserializableInput_ThrowSerializationException(string pipeName)
        {
            IClient<ITestService> client = _factory
                .WithIPCHostConfiguration(hostBuilder =>
                {
                    hostBuilder.AddNamedPipeEndpoint<ITestService>(pipeName);
                })
                .CreateClient((name, services) =>
                {
                    services.AddNamedPipeClient<ITestService>(name, pipeName);
                });

#if !DISABLE_DYNAMIC_CODE_GENERATION
            await Assert.ThrowsAnyAsync<SerializationException>(async () =>
            {
                await client.InvokeAsync(x => x.UnserializableInput(UnserializableObject.Create()));
            });
#endif

            await Assert.ThrowsAnyAsync<SerializationException>(async () =>
            {
                var request = TestHelpers.CreateIPCRequest(typeof(ITestService), "UnserializableInput", UnserializableObject.Create());
                await client.InvokeAsync(request);
            });
        }

        [Theory, AutoData]
        public async Task UnserializableOutput_ThrowFaultException(string pipeName)
        {
            _serviceMock
                .Setup(x => x.UnserializableOutput())
                .Returns(UnserializableObject.Create());

            IClient<ITestService> client = _factory
                .WithIPCHostConfiguration(hostBuilder => hostBuilder.AddNamedPipeEndpoint<ITestService>(pipeName))
                .CreateClient((name, services) => services.AddNamedPipeClient<ITestService>(name, pipeName));

#if !DISABLE_DYNAMIC_CODE_GENERATION
            Assert.Equal(Status.InternalServerError, (await Assert.ThrowsAnyAsync<FaultException>(async () => await client.InvokeAsync(x => x.UnserializableOutput()))).Status);
#endif

            Assert.Equal(Status.InternalServerError, (await Assert.ThrowsAnyAsync<FaultException>(async () => await client.InvokeAsync(TestHelpers.CreateIPCRequest("UnserializableOutput")))).Status);
        }
    }
}
