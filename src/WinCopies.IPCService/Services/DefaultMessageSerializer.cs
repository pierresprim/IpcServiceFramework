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

using Newtonsoft.Json;

using System;
using System.Text;

using WinCopies.Util;

namespace WinCopies.IPCService.Services
{
    public class DefaultMessageSerializer : IMessageSerializer
    {
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };

        public Request DeserializeRequest(byte[] binary) => Deserialize<Request>(binary);

        public Response DeserializeResponse(byte[] binary) => Deserialize<Response>(binary);

        public byte[] SerializeRequest(Request request) => Serialize(request);

        public byte[] SerializeResponse(Response response) => Serialize(response);

        private static bool CatchException(in System.Exception ex, in bool checkArgumentException) => ex.Is(false,
            typeof(JsonSerializationException),
            typeof(EncoderFallbackException)) ||
            (checkArgumentException && ex is ArgumentException);

        private T Deserialize<T>(byte[] binary)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(binary), _settings);
            }

            catch (System.Exception ex) when (CatchException(ex, true))
            {
                throw new SerializationException("Failed to deserialize IPC message", ex);
            }
        }

        private byte[] Serialize(object obj)
        {
            try
            {
                return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj, _settings));
            }

            catch (System.Exception ex) when (CatchException(ex, false))
            {
                throw new SerializationException("Failed to serialize IPC message", ex);
            }
        }
    }
}
