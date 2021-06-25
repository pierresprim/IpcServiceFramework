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
using System.Runtime.Serialization;

namespace WinCopies.IPCService
{
    [DataContract]
    public class Response
    {
        public static Response Success(object data) => new Response(Status.Ok, data, null, null);

        public static Response BadRequest() => new Response(Status.BadRequest, null, null, null);

        public static Response BadRequest(string errorDetails) => new Response(Status.BadRequest, null, errorDetails, null);

        public static Response BadRequest(string errorDetails, System.Exception innerException) => new Response(Status.BadRequest, null, errorDetails, innerException);

        public static Response InternalServerError() => new Response(Status.InternalServerError, null, null, null);

        public static Response InternalServerError(string errorDetails) => new Response(Status.InternalServerError, null, errorDetails, null);

        public static Response InternalServerError(string errorDetails, System.Exception innerException) => new Response(Status.InternalServerError, null, errorDetails, innerException);

        public Response(Status status, object data, string errorMessage, System.Exception innerException)
        {
            Status = status;
            Data = data;
            ErrorMessage = errorMessage;
            InnerException = innerException;
        }

        [DataMember]
        public Status Status { get; }

        [DataMember]
        public object Data { get; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public System.Exception InnerException { get; }

        public bool Succeed() => Status == Status.Ok;

        /// <summary>
        /// Create an exception that contains error information
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If the status is doesn't represent any error</exception>
        public FaultException CreateFaultException() => Status <= Status.Ok
                ? throw new InvalidOperationException(Properties.Resources.ResponseNotContainsAnyError)
                : new FaultException(Status, ErrorMessage, InnerException);
    }
}
