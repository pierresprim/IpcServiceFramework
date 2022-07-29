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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using WinCopies.IPCService.IO;
using WinCopies.IPCService.Services;

using static WinCopies.IPCService.Hosting.Properties.Resources;

namespace WinCopies.IPCService.Hosting
{
    public class EndpointOptions
    {
        public int MaxConcurrentCalls { get; set; } = 4;

        public bool IncludeFailureDetailsInResponse { get; set; }

        public Func<System.IO.Stream, System.IO.Stream> StreamTranslator { get; set; }

        public IMessageSerializer Serializer { get; set; } = new DefaultMessageSerializer();

        public IValueConverter ValueConverter { get; set; } = new DefaultValueConverter();
    }

    public interface IEndpoint : System.IDisposable
    {
        Task ExecuteAsync(CancellationToken stoppingToken);
    }

    public abstract class Endpoint<TContract> : IEndpoint where TContract : class
    {
        private readonly EndpointOptions _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphore;

        protected Endpoint(EndpointOptions options, IServiceProvider serviceProvider, ILogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _semaphore = new SemaphoreSlim(options.MaxConcurrentCalls);
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _ = new TaskFactory(TaskScheduler.Default);

            while (!stoppingToken.IsCancellationRequested)
            {
                await _semaphore.WaitAsync(stoppingToken).ConfigureAwait(false);

                _ = WaitAndProcessAsync(ProcessAsync, stoppingToken).ContinueWith(task =>
                  {
                      if (task.IsFaulted)

                          _logger.LogError(task.Exception, "Error occurred");

                      return _semaphore.Release();
                  }, TaskScheduler.Default);
            }
        }

        protected abstract Task WaitAndProcessAsync(Func<System.IO.Stream, CancellationToken, Task> process, CancellationToken stoppingToken);

        private async Task ProcessAsync(System.IO.Stream server, CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)

                return;

            if (_options.StreamTranslator != null)

                server = _options.StreamTranslator(server);

            using (var writer = new Writer(server, _options.Serializer, leaveOpen: true))
            using (var reader = new Reader(server, _options.Serializer, leaveOpen: true))
            using (System.IDisposable loggingScope = _logger.BeginScope(new Dictionary<string, object> { { "threadId", Thread.CurrentThread.ManagedThreadId } }))
            {
                try
                {
                    Request request;

                    try
                    {
                        _logger.LogDebug($"Client connected, reading request...");

                        request = await reader.ReadIPCRequestAsync(stoppingToken).ConfigureAwait(false);
                    }

                    catch (SerializationException ex)
                    {
                        throw new FaultException(Status.BadRequest, FailedToDeserializeRequest, ex);
                    }

                    stoppingToken.ThrowIfCancellationRequested();

                    Response response;

                    try
                    {
                        _logger.LogDebug($"Request received, invoking '{request.MethodName}'...");

                        using (IServiceScope scope = _serviceProvider.CreateScope())

                            response = await GetResponseAsync(request, scope).ConfigureAwait(false);
                    }

                    catch (System.Exception ex) when (!(ex is Exception))
                    {
                        throw new FaultException(Status.InternalServerError, "Unexpected exception raised from user code", ex);
                    }

                    stoppingToken.ThrowIfCancellationRequested();

                    try
                    {
                        _logger.LogDebug($"Sending response...");

                        await writer.WriteAsync(response, stoppingToken).ConfigureAwait(false);
                    }

                    catch (SerializationException ex)
                    {
                        throw new FaultException(Status.InternalServerError, FailedToSerializeResponse, ex);
                    }

                    _logger.LogDebug($"Process finished.");
                }

                catch (CommunicationException ex)
                {
                    _logger.LogError(ex, "Communication error occurred.");
                    // if communication error occurred, client will probably not receive any response 
                }

                catch (OperationCanceledException ex)
                {
                    _logger.LogWarning(ex, "IPC request process cancelled");

                    Response response = _options.IncludeFailureDetailsInResponse
                        ? Response.InternalServerError("IPC request process cancelled")
                        : Response.InternalServerError();

                    await writer.WriteAsync(response, stoppingToken).ConfigureAwait(false);
                }

                catch (FaultException ex)
                {
                    _logger.LogError(ex, "Failed to process IPC request.");

                    Response response;

                    switch (ex.Status)
                    {
                        case Status.BadRequest:

                            response = _options.IncludeFailureDetailsInResponse
                                ? Response.BadRequest(ex.Message, ex.InnerException)
                                : Response.BadRequest();

                            break;

                        default:

                            response = _options.IncludeFailureDetailsInResponse
                                ? Response.InternalServerError(ex.Message, ex.InnerException)
                                : Response.InternalServerError();

                            break;
                    }

                    await writer.WriteAsync(response, stoppingToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<Response> GetResponseAsync(Request request, IServiceScope scope)
        {
            object service = scope.ServiceProvider.GetService<TContract>();

            if (service == null)

                throw new FaultException(Status.BadRequest, string.Format(NoImplementationOfInterfaceFound, typeof(TContract).FullName));

            MethodInfo method = GetUnambiguousMethod(request, service);

            if (method == null)

                throw new FaultException(Status.BadRequest, string.Format(MethodNotFoundInInterface, request.MethodName, typeof(TContract).FullName));

            ParameterInfo[] paramInfos = method.GetParameters();

            object[] requestParameters = request.Parameters?.ToArray() ?? Array.Empty<object>();

            if (paramInfos.Length != requestParameters.Length)

                throw new FaultException(Status.BadRequest, string.Format(MethodExpectsLessOrMoreParameters, request.MethodName, paramInfos.Length));

            Type[] genericArguments = method.GetGenericArguments();
            Type[] requestGenericArguments;

            if (request.GenericArguments != null)

                // Generic arguments passed by Type
                requestGenericArguments = request.GenericArguments.ToArray();

            else if (request.GenericArgumentsByName != null)
            {
                // Generic arguments passed by name
                requestGenericArguments = new Type[request.GenericArgumentsByName.Count()];

                int i = 0;

                foreach (RequestParameterType pair in request.GenericArgumentsByName)

                    requestGenericArguments[i++] = Assembly.Load(pair.AssemblyName).GetType(pair.ParameterType);
            }

            else

                requestGenericArguments = Array.Empty<Type>();

            if (genericArguments.Length != requestGenericArguments.Length)

                throw new FaultException(Status.BadRequest, GenericArgumentsMismatch);

            object[] args = new object[paramInfos.Length];

            for (int i = 0; i < args.Length; i++)
            {
                object origValue = requestParameters[i];

                Type destType = paramInfos[i].ParameterType;

                if (destType.IsGenericParameter)

                    destType = requestGenericArguments[destType.GenericParameterPosition];

                args[i] = _options.ValueConverter.TryConvert(origValue, destType, out object arg)
                    ? arg
                    : throw new FaultException(Status.BadRequest, string.Format(CannotConvertValueOfParameterFromTypeAToTypeB, paramInfos[i].Name, origValue, origValue.GetType().Name, destType.Name));
            }

            if (method.IsGenericMethod)

                method = method.MakeGenericMethod(requestGenericArguments);

            object @return = method.Invoke(service, args);

            if (@return is Task task)
            {
                await task.ConfigureAwait(false);

                return Response.Success(@return.GetType().GetProperty("Result")?.GetValue(@return));
            }

            else

                return Response.Success(@return);
        }

        private static MethodInfo GetUnambiguousMethod(Request request, object service)
        {
            if (request is null)

                throw new ArgumentNullException(nameof(request));

            if (service is null)

                throw new ArgumentNullException(nameof(service));

            MethodInfo method = null;     // disambiguate - can't just call as before with generics - MethodInfo method = service.GetType().GetMethod(request.MethodName);

            Type[] types = service.GetType().GetInterfaces();

            IEnumerable<MethodInfo> allMethods = types.SelectMany(t => t.GetMethods());

            var serviceMethods = allMethods.Where(t => t.Name == request.MethodName).ToList();

            // Check if we were passed Type objects or IPCRequestParameterType objects
            if ((request.ParameterTypes != null) && (request.ParameterTypesByName != null))

                throw new FaultException(Status.BadRequest, string.Format(OnlyOneOfParameterShouldBeSet, "ParameterTypes", "ParameterTypesByName"));

            if ((request.GenericArguments != null) && (request.GenericArgumentsByName != null))

                throw new FaultException(Status.BadRequest, string.Format(OnlyOneOfParameterShouldBeSet, "GenericArguments", "GenericArgumentsByName"));

            object[] requestParameters = request.Parameters?.ToArray() ?? Array.Empty<object>();

            Type[] requestGenericArguments;

            if (request.GenericArguments != null)

                // Generic arguments passed by Type
                requestGenericArguments = request.GenericArguments.ToArray();

            else if (request.GenericArgumentsByName != null)
            {
                // Generic arguments passed by name
                requestGenericArguments = new Type[request.GenericArgumentsByName.Count()];

                int i = 0;

                foreach (RequestParameterType pair in request.GenericArgumentsByName)

                    requestGenericArguments[i++] = Assembly.Load(pair.AssemblyName).GetType(pair.ParameterType);
            }

            else

                requestGenericArguments = Array.Empty<Type>();

            Type[] requestParameterTypes;

            if (request.ParameterTypes != null)

                // Parameter types passed by Type
                requestParameterTypes = request.ParameterTypes.ToArray();

            else if (request.ParameterTypesByName != null)
            {
                // Parameter types passed by name
                requestParameterTypes = new Type[request.ParameterTypesByName.Count()];

                int i = 0;

                foreach (RequestParameterType pair in request.ParameterTypesByName)

                    requestParameterTypes[i++] = Assembly.Load(pair.AssemblyName).GetType(pair.ParameterType);
            }

            else

                requestParameterTypes = Array.Empty<Type>();

            foreach (MethodInfo serviceMethod in serviceMethods)
            {
                ParameterInfo[] serviceMethodParameters = serviceMethod.GetParameters();

                int parameterTypeMatches = 0;

                if (serviceMethodParameters.Length == requestParameters.Length && serviceMethod.GetGenericArguments().Length == requestGenericArguments.Length)
                {
                    for (int parameterIndex = 0; parameterIndex < serviceMethodParameters.Length; parameterIndex++)
                    {
                        Type serviceParameterType = serviceMethodParameters[parameterIndex].ParameterType.IsGenericParameter ?
                                            requestGenericArguments[serviceMethodParameters[parameterIndex].ParameterType.GenericParameterPosition] :
                                            serviceMethodParameters[parameterIndex].ParameterType;

                        if (serviceParameterType == requestParameterTypes[parameterIndex])

                            parameterTypeMatches++;

                        else

                            break;
                    }

                    if (parameterTypeMatches == serviceMethodParameters.Length)
                    {
                        method = serviceMethod;        // signatures match so assign

                        break;
                    }
                }
            }

            return method;
        }

        #region IDisposable
        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)

                return;

            if (disposing)

                _semaphore.Dispose();

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
