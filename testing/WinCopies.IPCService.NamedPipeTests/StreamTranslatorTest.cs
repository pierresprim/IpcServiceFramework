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
using WinCopies.IPCService.Testing.Fixtures;

using Xunit;

namespace WinCopies.IPCService.NamedPipeTests
{
    public class StreamTranslatorTest : IClassFixture<ApplicationFactory<ITestService>>
    {
        private readonly Mock<ITestService> _serviceMock = new Mock<ITestService>();
        private readonly ApplicationFactory<ITestService> _factory;

        public StreamTranslatorTest(ApplicationFactory<ITestService> factory) => _factory = factory;

        [Theory, AutoData]
        public async Task StreamTranslator_HappyPath(string pipeName, string input, string expected)
        {
            _serviceMock
                .Setup(x => x.StringType(input))
                .Returns(expected);

            IClient<ITestService> client = _factory
                .WithServiceImplementation(_ => _serviceMock.Object)
                .WithIPCHostConfiguration(hostBuilder => hostBuilder.AddNamedPipeEndpoint<ITestService>(options =>
{
    options.PipeName = pipeName;
    options.StreamTranslator = x => new XorStream(x);
}))
                .CreateClient((name, services) => services.AddNamedPipeClient<ITestService>(name, (_, options) =>
{
    options.PipeName = pipeName;
    options.StreamTranslator = x => new XorStream(x);
}));

#if !DISABLE_DYNAMIC_CODE_GENERATION
            Assert.Equal(expected, await client.InvokeAsync(x => x.StringType(input)));
#endif

            Assert.Equal(expected, await client.InvokeAsync<string>(TestHelpers.CreateIPCRequest(typeof(ITestService), "StringType", new object[] { input })));
        }
    }
}
