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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using WinCopies.IPCService.Services;

using static WinCopies.IPCService.Properties.Resources;

namespace WinCopies.IPCService.IO
{
    public class Reader : System.IDisposable
    {
        private readonly System.IO.Stream _stream;
        private readonly IMessageSerializer _serializer;
        private readonly bool _leaveOpen;

        public Reader(System.IO.Stream stream, IMessageSerializer serializer) : this(stream, serializer, leaveOpen: false) { }

        public Reader(System.IO.Stream stream, IMessageSerializer serializer, bool leaveOpen)
        {
            _stream = stream;
            _serializer = serializer;
            _leaveOpen = leaveOpen;
        }

        /// <exception cref="CommunicationException"></exception>
        /// <exception cref="SerializationException"></exception>
        public async Task<Request> ReadIPCRequestAsync(CancellationToken cancellationToken = default)
        {
            byte[] binary = await ReadMessageAsync(cancellationToken).ConfigureAwait(false);

            return _serializer.DeserializeRequest(binary);
        }

        /// <exception cref="CommunicationException"></exception>
        /// <exception cref="SerializationException"></exception>
        public async Task<Response> ReadIPCResponseAsync(CancellationToken cancellationToken = default)
        {
            byte[] binary = await ReadMessageAsync(cancellationToken).ConfigureAwait(false);

            return _serializer.DeserializeResponse(binary);
        }

        private async Task<byte[]> ReadMessageAsync(CancellationToken cancellationToken)
        {
            byte[] lengthBuffer = new byte[4];

            int headerLength = await _stream
                .ReadAsync(lengthBuffer, 0, lengthBuffer.Length, cancellationToken)
                .ConfigureAwait(false);

            if (headerLength != 4)

                throw new CommunicationException($"{InvalidMessageHeaderLength}{headerLength}");

            int remainingBytes = lengthBuffer[0] | lengthBuffer[1] << 8 | lengthBuffer[2] << 16 | lengthBuffer[3] << 24;

            byte[] buffer = new byte[65536];
            int offset = 0;

            using (var ms = new MemoryStream())
            {
                while (remainingBytes > 0)
                {
                    int count = System.Math.Min(buffer.Length, remainingBytes);

                    int actualCount = await _stream
                        .ReadAsync(buffer, offset, count, cancellationToken)
                        .ConfigureAwait(false);

                    if (actualCount == 0)

                        throw new CommunicationException(StreamClosedUnexpectedly);

                    ms.Write(buffer, 0, actualCount);

                    remainingBytes -= actualCount;
                }

                return ms.ToArray();
            }
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
