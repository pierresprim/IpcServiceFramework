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

namespace WinCopies.IPCService.Client
{
    /// <summary>
    /// Tcp clients depend on both a TcpClient object and a Stream object.
    /// This wrapper class ensures they are simultaneously disposed of.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class StreamWrapper : System.IDisposable
    {
        private IDisposable _context;
        bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamWrapper"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="context">The IDisposable context the Stream depends on.</param>
        public StreamWrapper(System.IO.Stream stream, IDisposable context = null)
        {
            Stream = stream;
            _context = context;
        }

        public System.IO.Stream Stream { get; private set; }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Stream?.Dispose();
                _context?.Dispose();
            }
        }
    }
}
