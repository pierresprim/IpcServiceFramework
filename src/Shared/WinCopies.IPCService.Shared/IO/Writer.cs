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
using System.Threading;
using System.Threading.Tasks;

using WinCopies.IPCService.Services;

namespace WinCopies.IPCService.IO
{
    public class Writer : System.IDisposable
    {
        private readonly byte[] _lengthBuffer = new byte[4];
        private readonly System.IO.Stream _stream;
        private readonly IMessageSerializer _serializer;
        private readonly bool _leaveOpen;

        public Writer(System.IO.Stream stream, IMessageSerializer serializer) : this(stream, serializer, leaveOpen: false) { }

        public Writer(System.IO.Stream stream, IMessageSerializer serializer, bool leaveOpen)
        {
            _stream = stream;
            _serializer = serializer;
            _leaveOpen = leaveOpen;
        }

        public async Task WriteAsync(Request request, CancellationToken cancellationToken = default)
        {
            byte[] binary = _serializer.SerializeRequest(request);

            await WriteMessageAsync(binary, cancellationToken).ConfigureAwait(false);
        }

        public async Task WriteAsync(Response response, CancellationToken cancellationToken = default)
        {
            byte[] binary = _serializer.SerializeResponse(response);

            await WriteMessageAsync(binary, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteMessageAsync(byte[] binary, CancellationToken cancellationToken)
        {
            int length = binary.Length;

            _lengthBuffer[0] = (byte)length;
            _lengthBuffer[1] = (byte)(length >> 8);
            _lengthBuffer[2] = (byte)(length >> 16);
            _lengthBuffer[3] = (byte)(length >> 24);

            await _stream.WriteAsync(_lengthBuffer, 0, _lengthBuffer.Length, cancellationToken).ConfigureAwait(false);
            await _stream.WriteAsync(binary, 0, binary.Length, cancellationToken).ConfigureAwait(false);
        }

        #region IDisposible
        bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)

                return;

            if (disposing)

                if (!_leaveOpen)

                    _stream.Dispose();

            _disposed = true;
        }
        #endregion
    }
}
