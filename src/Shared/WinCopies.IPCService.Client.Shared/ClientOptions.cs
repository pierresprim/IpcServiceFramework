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

using WinCopies.IPCService.Services;

namespace WinCopies.IPCService.Client
{
    public class ClientOptions
    {
        public Func<System.IO.Stream, System.IO.Stream> StreamTranslator { get; set; }

        /// <summary>
        /// The number of milliseconds to wait for the server to respond before
        /// the connection times out. Default value is 60000.
        /// </summary>
        public int ConnectionTimeout { get; set; } = 60000;

        /// <summary>
        /// Indicates the method that will be used during deserialization on the server for locating and loading assemblies.
        /// If <c>false</c>, the assembly used during deserialization must match exactly the assembly used during serialization.
        /// 
        /// If <c>true</c>, the assembly used during deserialization need not match exactly the assembly used during serialization.
        /// Specifically, the version numbers need not match.
        /// 
        /// Default is <c>false</c>.
        /// </summary>
        public bool UseSimpleTypeNameAssemblyFormatHandling { get; set; } = false;

        public IMessageSerializer Serializer { get; set; } = new DefaultMessageSerializer();

        public IValueConverter ValueConverter { get; set; } = new DefaultValueConverter();
    }
}
