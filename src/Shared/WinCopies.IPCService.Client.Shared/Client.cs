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
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using WinCopies.IPCService.IO;

using static WinCopies.IPCService.Client.Properties.Resources;

namespace WinCopies.IPCService.Client
{
    public abstract class Client<TInterface> : IClient<TInterface> where TInterface : class
    {
        private readonly ClientOptions _options;

        public string Name { get; }

        protected Client(string name, ClientOptions options)
        {
            Name = name;
            _options = options;
        }

#if !DISABLE_DYNAMIC_CODE_GENERATION
        private static void RunTask(in Response response)
        {
            if (!response.Succeed())

                throw response.CreateFaultException();
        }

        /// <exception cref="SerializationException">If unable to serialize request</exception>
        /// <exception cref="CommunicationException">If communication is broken</exception>
        /// <exception cref="FaultException">If error occurred in server</exception>
        public async Task InvokeAsync(Expression<Action<TInterface>> exp, CancellationToken cancellationToken = default) => RunTask(await GetResponseAsync(GetRequest(exp, DispatchProxy.Create<TInterface, Proxy>()), cancellationToken).ConfigureAwait(false));

        private T GetResult<T>(in Response response) => response.Succeed()
                ? _options.ValueConverter.TryConvert(response.Data, typeof(T), out object @return)
                ? (T)@return
                : throw new SerializationException(string.Format(Properties.Resources.UnableToConvertReturnedValue, typeof(T).Name))
                : throw response.CreateFaultException();

        /// <exception cref="SerializationException">If unable to serialize request</exception>
        /// <exception cref="CommunicationException">If communication is broken</exception>
        /// <exception cref="FaultException">If error occurred in server</exception>
        public async Task<TResult> InvokeAsync<TResult>(Expression<Func<TInterface, TResult>> exp, CancellationToken cancellationToken = default) => GetResult<TResult>(await GetResponseAsync(GetRequest(exp, DispatchProxy.Create<TInterface, Proxy<TResult>>()), cancellationToken).ConfigureAwait(false));

        /// <exception cref="SerializationException">If unable to serialize request</exception>
        /// <exception cref="CommunicationException">If communication is broken</exception>
        /// <exception cref="FaultException">If error occurred in server</exception>
        public async Task InvokeAsync(Expression<Func<TInterface, Task>> exp, CancellationToken cancellationToken = default) => RunTask(await GetResponseAsync(GetRequest(exp, DispatchProxy.Create<TInterface, Proxy<Task>>()), cancellationToken).ConfigureAwait(false));

        /// <exception cref="SerializationException">If unable to serialize request</exception>
        /// <exception cref="CommunicationException">If communication is broken</exception>
        /// <exception cref="FaultException">If error occurred in server</exception>
        public async Task<TResult> InvokeAsync<TResult>(Expression<Func<TInterface, Task<TResult>>> exp, CancellationToken cancellationToken = default) => GetResult<TResult>(await GetResponseAsync(GetRequest(exp, DispatchProxy.Create<TInterface, Proxy<Task<TResult>>>()), cancellationToken).ConfigureAwait(false));
#endif

        public async Task<TResult> InvokeAsync<TResult>(Request request, CancellationToken cancellationToken = default)
        {
            Response response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);

            return !response.Succeed()
                ? throw response.CreateFaultException()
                : _options.ValueConverter.TryConvert(response.Data, typeof(TResult), out object @return)
                ? (TResult)@return
                : throw new SerializationException(string.Format(UnableToConvertReturnedValue, typeof(TResult).Name));
        }

        public async Task InvokeAsync(Request request, CancellationToken cancellationToken = default)
        {
            Response response = await GetResponseAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.Succeed())

                throw response.CreateFaultException();
        }

#if !DISABLE_DYNAMIC_CODE_GENERATION
        private Request GetRequest(Expression exp, TInterface proxy)
        {
            void throwException(in string arg) => throw new ArgumentException(string.Format(OnlySupport, arg));

            if (!(exp is LambdaExpression lambdaExp))

                throwException("lambda expression");

            if (!(lambdaExp.Body is MethodCallExpression methodCallExp))

                throwException("calling method");

            Delegate @delegate = lambdaExp.Compile();

            _ = @delegate.DynamicInvoke(proxy);

            if (_options.UseSimpleTypeNameAssemblyFormatHandling)
            {
                RequestParameterType[] paramByName = null;
                RequestParameterType[] genericByName = null;

                System.Collections.Generic.IEnumerable<Type> parameterTypes = (proxy as Proxy).LastInvocation.Method.GetParameters().Select(p => p.ParameterType);

                if (parameterTypes.Any())
                {
                    paramByName = new RequestParameterType[parameterTypes.Count()];

                    int i = 0;

                    foreach (Type type in parameterTypes)

                        paramByName[i++] = new RequestParameterType(type);
                }

                Type[] genericTypes = (proxy as Proxy).LastInvocation.Method.GetGenericArguments();

                if (genericTypes.Length > 0)
                {
                    genericByName = new RequestParameterType[genericTypes.Length];

                    int i = 0;

                    foreach (Type type in genericTypes)

                        genericByName[i++] = new RequestParameterType(type);
                }

                return new Request
                {
                    MethodName = (proxy as Proxy).LastInvocation.Method.Name,
                    Parameters = (proxy as Proxy).LastInvocation.Arguments,

                    ParameterTypesByName = paramByName,
                    GenericArgumentsByName = genericByName
                };
            }

            else

                return new Request
                {
                    MethodName = (proxy as Proxy).LastInvocation.Method.Name,
                    Parameters = (proxy as Proxy).LastInvocation.Arguments,

                    ParameterTypes = (proxy as Proxy).LastInvocation.Method.GetParameters()
                                  .Select(p => p.ParameterType)
                                  .ToArray(),


                    GenericArguments = (proxy as Proxy).LastInvocation.Method.GetGenericArguments(),
                };
        }
#endif

        protected abstract Task<StreamWrapper> ConnectToServerAsync(CancellationToken cancellationToken);

        private async Task<Response> GetResponseAsync(Request request, CancellationToken cancellationToken)
        {
            using (StreamWrapper client = await ConnectToServerAsync(cancellationToken).ConfigureAwait(false))
            using (System.IO.Stream client2 = _options.StreamTranslator == null ? client.Stream : _options.StreamTranslator(client.Stream))
            using (var writer = new Writer(client2, _options.Serializer, leaveOpen: true))
            using (var reader = new Reader(client2, _options.Serializer, leaveOpen: true))
            {
                // send request
                await writer.WriteAsync(request, cancellationToken).ConfigureAwait(false);

                // receive response
                return await reader.ReadIPCResponseAsync(cancellationToken).ConfigureAwait(false);
            }
        }

#if !DISABLE_DYNAMIC_CODE_GENERATION
        public class Proxy : DispatchProxy
        {
            public Invocation LastInvocation { get; protected set; }

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                LastInvocation = new Invocation(targetMethod, args);

                return null;
            }

            public class Invocation
            {
                public MethodInfo Method { get; }

                public object[] Arguments { get; }

                public Invocation(MethodInfo method, object[] args)
                {
                    Method = method;
                    Arguments = args;
                }
            }
        }

        public class Proxy<TResult> : Proxy
        {
            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                LastInvocation = new Invocation(targetMethod, args);

                return default(TResult);
            }
        }
#endif
    }
}
