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

using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace WinCopies.IPCService.Client.NamedPipe
{
    public class NamedPipeClientOptions : ClientOptions
    {
        public string PipeName { get; set; }
    }

    internal class NamedPipeClient<TInterface> : Client<TInterface> where TInterface : class
    {
        private readonly NamedPipeClientOptions _options;

        public NamedPipeClient(string name, NamedPipeClientOptions options) : base(name, options) => _options = options;

        protected override async Task<StreamWrapper> ConnectToServerAsync(CancellationToken cancellationToken)
        {
            var stream = new NamedPipeClientStream(".", _options.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await stream.ConnectAsync(_options.ConnectionTimeout, cancellationToken).ConfigureAwait(false);
            return new StreamWrapper(stream);
        }
    }
}
