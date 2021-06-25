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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using WinCopies.IPCService.Client;
using WinCopies.IPCService.Hosting;
using WinCopies.IPCService.NamedPipeTests.Fixtures;
using WinCopies.IPCService.Testing;

using Xunit;

namespace WinCopies.IPCService.NamedPipeTests
{
    /// <summary>
    /// Validates the IPC pipeline is working end-to-end for a variety of method types.
    /// Tests both dynamically generated IPCRequests (via DispatchProxy) and statically generated ones.
    /// Tests using full parameter types (UseSimpleTypeNameAssemblyFormatHandling == false).
    /// </summary>
    /// <seealso cref="IClassFixture{ApplicationFactory{ITestService}}" />
    public class ContractTest : IClassFixture<ApplicationFactory<ITestService>>
    {
        private readonly Mock<ITestService> _serviceMock = new Mock<ITestService>();
        private readonly IClient<ITestService> _client;

        public ContractTest(ApplicationFactory<ITestService> factory)
        {
            string pipeName = Guid.NewGuid().ToString();

            _client = factory
                .WithServiceImplementation(_ => _serviceMock.Object)
                .WithIPCHostConfiguration(hostBuilder =>
                {
                    _ = hostBuilder.AddNamedPipeEndpoint<ITestService>(options =>
                      {
                          options.PipeName = pipeName;
                          options.IncludeFailureDetailsInResponse = true;
                      });
                })
                .CreateClient((name, services) => services.AddNamedPipeClient<ITestService>(name, pipeName));
        }

        [Theory, AutoData]
        public async Task PrimitiveTypes(bool a, byte b, sbyte c, char d, decimal e, double f, float g, int h, uint i,
           long j, ulong k, short l, ushort m, int expected)
        {
            _serviceMock
                .Setup(x => x.PrimitiveTypes(a, b, c, d, e, f, g, h, i, j, k, l, m))
                .Returns(expected);

#if !DISABLE_DYNAMIC_CODE_GENERATION
            Assert.Equal(expected, await _client.InvokeAsync(x => x.PrimitiveTypes(a, b, c, d, e, f, g, h, i, j, k, l, m)));
#endif

            Assert.Equal(expected, await _client.InvokeAsync<int>(TestHelpers.CreateIPCRequest(typeof(ITestService), "PrimitiveTypes", a, b, c, d, e, f, g, h, i, j, k, l, m)));
        }

        [Theory, AutoData]
        public async Task StringType(string input, string expected)
        {
            _serviceMock
                .Setup(x => x.StringType(input))
                .Returns(expected);

#if !DISABLE_DYNAMIC_CODE_GENERATION
            string actual = await _client
                .InvokeAsync(x => x.StringType(input));

            Assert.Equal(expected, actual);
#endif

            Assert.Equal(expected, await _client.InvokeAsync<string>(TestHelpers.CreateIPCRequest(typeof(ITestService), "StringType", input)));
        }

        [Theory, AutoData]
        public async Task ComplexType(Complex input, Complex expected)
        {
            _serviceMock.Setup(x => x.ComplexType(input)).Returns(expected);

#if !DISABLE_DYNAMIC_CODE_GENERATION
            Assert.Equal(expected, await _client.InvokeAsync(x => x.ComplexType(input)));
#endif

            Assert.Equal(expected, await _client.InvokeAsync<Complex>(TestHelpers.CreateIPCRequest(typeof(ITestService), "ComplexType", input)));
        }

        [Theory, AutoData]
        public async Task ComplexTypeArray(IEnumerable<Complex> input, IEnumerable<Complex> expected)
        {
            _serviceMock
                .Setup(x => x.ComplexTypeArray(input))
                .Returns(expected);

#if !DISABLE_DYNAMIC_CODE_GENERATION
            Assert.Equal(expected, await _client.InvokeAsync(x => x.ComplexTypeArray(input)));
#endif

            Assert.Equal(expected, await _client.InvokeAsync<IEnumerable<Complex>>(TestHelpers.CreateIPCRequest(typeof(ITestService), "ComplexTypeArray", input)));
        }

        [Theory, AutoData]
        public async Task LargeComplexTypeArray(Complex input, Complex expected)
        {
            IEnumerable<Complex> largeInput = Enumerable.Repeat(input, 1000);
            IEnumerable<Complex> largeExpected = Enumerable.Repeat(expected, 100);

            _serviceMock
                .Setup(x => x.ComplexTypeArray(largeInput))
                .Returns(largeExpected);

#if !DISABLE_DYNAMIC_CODE_GENERATION
            IEnumerable<Complex> actual = await _client
                .InvokeAsync(x => x.ComplexTypeArray(largeInput));

            Assert.Equal(largeExpected, actual);
#endif

            Assert.Equal(largeExpected, await _client.InvokeAsync<IEnumerable<Complex>>(TestHelpers.CreateIPCRequest(typeof(ITestService), "ComplexTypeArray", largeInput)));
        }

        [Fact]
        public async Task ReturnVoid()
        {
#if !DISABLE_DYNAMIC_CODE_GENERATION
            await _client.InvokeAsync(x => x.ReturnVoid());
#endif

            await _client.InvokeAsync(TestHelpers.CreateIPCRequest("ReturnVoid"));
        }

        [Theory, AutoData]
        public async Task DateTime(DateTime input, DateTime expected)
        {
            _serviceMock.Setup(x => x.DateTime(input)).Returns(expected);

#if !DISABLE_DYNAMIC_CODE_GENERATION
            DateTime actual = await _client
                .InvokeAsync(x => x.DateTime(input));

            Assert.Equal(expected, actual);
#endif

            Assert.Equal(expected, await _client.InvokeAsync<DateTime>(TestHelpers.CreateIPCRequest(typeof(ITestService), "DateTime", input)));
        }

        [Theory, AutoData]
        public async Task EnumType(DateTimeStyles input, DateTimeStyles expected)
        {
            _serviceMock.Setup(x => x.EnumType(input)).Returns(expected);

#if !DISABLE_DYNAMIC_CODE_GENERATION
            DateTimeStyles actual = await _client
                .InvokeAsync(x => x.EnumType(input));

            Assert.Equal(expected, actual);
#endif

            Assert.Equal(expected, await _client.InvokeAsync<DateTimeStyles>(TestHelpers.CreateIPCRequest(typeof(ITestService), "EnumType", input)));
        }

        [Theory, AutoData]
        public async Task ByteArray(byte[] input, byte[] expected)
        {
            _serviceMock.Setup(x => x.ByteArray(input)).Returns(expected);

#if !DISABLE_DYNAMIC_CODE_GENERATION
            byte[] actual = await _client
                .InvokeAsync(x => x.ByteArray(input));

            Assert.Equal(expected, actual);
#endif

            Assert.Equal(expected, await _client.InvokeAsync<byte[]>(TestHelpers.CreateIPCRequest(typeof(ITestService), "ByteArray", input)));
        }

        [Theory, AutoData]
        public async Task GenericMethod(decimal input, decimal expected)
        {
            _serviceMock
                .Setup(x => x.GenericMethod<decimal>(input))
                .Returns(expected);

#if !DISABLE_DYNAMIC_CODE_GENERATION
            Assert.Equal(expected, await _client.InvokeAsync(x => x.GenericMethod<decimal>(input)));
#endif
        }

        [Theory, AutoData]
        public async Task AsyncMethod(int expected)
        {
            _serviceMock
                .Setup(x => x.AsyncMethod())
                .ReturnsAsync(expected);

#if !DISABLE_DYNAMIC_CODE_GENERATION
            Assert.Equal(expected, await _client.InvokeAsync(x => x.AsyncMethod()));
#endif

            Assert.Equal(expected, await _client.InvokeAsync<int>(TestHelpers.CreateIPCRequest("AsyncMethod")));
        }

        [Theory, AutoData]
        public async Task Abstraction(TestDto input, TestDto expected)
        {
            _serviceMock
                .Setup(x => x.Abstraction(It.Is<TestDto>(o => o.Value == input.Value)))
                .Returns(expected);

#if !DISABLE_DYNAMIC_CODE_GENERATION
            ITestDto actual = await _client.InvokeAsync(x => x.Abstraction(input));

            Assert.Equal(expected.Value, actual.Value);
#endif

            Assert.Equal(expected.Value, (await _client.InvokeAsync<ITestDto>(TestHelpers.CreateIPCRequest(typeof(ITestService), "Abstraction", input))).Value);
        }
    }
}
