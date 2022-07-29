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
using System.Linq;
using System.Reflection;

namespace WinCopies.IPCService.Testing
{
    public static class TestHelpers
    {
        /// <summary>
        /// Creates an IPC request for the given method in the given interface.
        /// </summary>
        /// <param name="interfaceType">The Type of the interface containing the method.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="args">The arguments to the method.</param>
        /// <returns>IPCRequest object</returns>
        public static Request CreateIPCRequest(Type interfaceType, string methodName, params object[] args)
        {
            MethodInfo method = null;

            // Try to find the matching method based on name and args
            if (args.All(x => x != null))
            
                method = interfaceType.GetMethod(methodName, args.Select(x => x.GetType()).ToArray());
            
            if (method == null)
            
                method = interfaceType.GetMethod(methodName);
            
            if (method == null)
            
                throw new ArgumentException($"Could not find a valid method in {interfaceType}!");
            
            if (method.IsGenericMethod)
            
                throw new ArgumentException($"{methodName} is generic and not supported!");

            ParameterInfo[] methodParams = method.GetParameters();

            var request = new Request()
            {
                MethodName = methodName,
                Parameters = args
            };

            var parameterTypes = new Type[methodParams.Length];

            for (int i = 0; i < args.Length; i++)
            
                parameterTypes[i] = methodParams[i].ParameterType;
            
            request.ParameterTypes = parameterTypes;

            return request;
        }

        /// <summary>
        /// Creates an IPC request for the given method name which takes no parameters.
        /// </summary>
        /// <param name="methodName">Name of the method. The method should have no parameters.</param>
        /// <returns>IPCRequest object</returns>
        public static Request CreateIPCRequest(string methodName) => new Request()
        {
            MethodName = methodName,
            Parameters = Array.Empty<object>(),
            ParameterTypes = Array.Empty<Type>()
        };
    }
}
