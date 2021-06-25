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
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WinCopies.IPCService
{
    [DataContract]
    public class Request
    {
        [DataMember]
        public string MethodName { get; set; }

        [DataMember]
        public IEnumerable<object> Parameters { get; set; }

        [DataMember]
        public IEnumerable<Type> ParameterTypes { get; set; }

        [DataMember]
        public IEnumerable<RequestParameterType> ParameterTypesByName { get; set; }

        [DataMember]
        public IEnumerable<Type> GenericArguments { get; set; }

        [DataMember]
        public IEnumerable<RequestParameterType> GenericArgumentsByName { get; set; }
    }

    /// <summary>
    /// Used to pass ParameterTypes annd GenericArguments by "Name" instead of by an explicit Type object.
    /// This allows for ParameterTypes to resolve properly even if the assembly version isn't an exact match on the Client & Host.
    /// </summary>
    [DataContract]
    public class RequestParameterType
    {
        [DataMember]
        public string ParameterType { get; private set; }

        [DataMember]
        public string AssemblyName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestParameterType"/> class.
        /// </summary>
        public RequestParameterType()
        {
            ParameterType = null;
            AssemblyName = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestParameterType"/> class.
        /// </summary>
        /// <param name="paramType">The type of parameter.</param>
        /// <exception cref="ArgumentNullException">paramType</exception>
        public RequestParameterType(Type paramType)
        {
            ParameterType = (paramType ?? throw new ArgumentNullException(nameof(paramType))).FullName;
            AssemblyName = paramType.Assembly.GetName().Name;
        }
    }
}
