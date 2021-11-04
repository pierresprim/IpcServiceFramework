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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using WinCopies.Collections;
using WinCopies.Collections.DotNetFix;
using WinCopies.Collections.DotNetFix.Generic;
using WinCopies.Collections.Generic;
using WinCopies.IPCService.Client;
using WinCopies.IPCService.Hosting;
using WinCopies.Linq;

using static WinCopies.ThrowHelper;
using static WinCopies.IPCService.Extensions.Properties.Resources;

namespace WinCopies.IPCService.Extensions
{
    public interface ISingleInstanceApp<TIn, TOut>
    {
        string GetPipeName();

        string GetClientName();

        /// <seealso cref="Thread(ThreadStart, int)"/>
        ThreadStart GetThreadStart(out int maxStackSize);

        Expression<Func<TIn, TOut>> GetExpression();

        Expression<Func<TIn, Task<TOut>>> GetAsyncExpression();

        CancellationToken? GetCancellationToken();
    }

    public interface ISingleInstanceApp<TIn, TOut, TApplication> : ISingleInstanceApp<TIn, TOut>
    {
        void Run(in TApplication application);
    }

    public static class Extensions
    {
        public static ConfiguredTaskAwaitable Await(this Task task) => GetOrThrowIfNull(task, nameof(task)).ConfigureAwait(false);

        public static ConfiguredTaskAwaitable<T> Await<T>(this Task<T> task) => GetOrThrowIfNull(task, nameof(task)).ConfigureAwait(false);

        [Obsolete("This method has been replaced with StartThread2.")]
        public static async Task StartThread(ThreadStart threadStart, int maxStackSize) => await threadStart.StartThread2(maxStackSize).Await();

        public static async Task StartThread2(this ThreadStart threadStart, int maxStackSize)
        {
            var t = new Thread(threadStart, maxStackSize);

            t.SetApartmentState(ApartmentState.STA);

            t.Start();
        }

        public static async Task<(Mutex mutex, bool mutexExists, NullableGeneric<TOut> serverResult)> StartInstanceAsync<TInterface, TClass, TOut>(this ISingleInstanceApp<TInterface, TOut> app) where TInterface : class where TClass : class, TInterface
        {
            string pipeName = GetOrThrowIfNull(app, nameof(app)).GetPipeName();

            (Mutex mutex, bool mutexExists, NullableGeneric<TOut> serverResult) result;

            result.mutex = new Mutex(false, pipeName);

            result.mutexExists = false;

            result.serverResult = null;

            bool rethrow = false;

            try
            {
                void _throw(in System.Exception exception)
                {
                    rethrow = true;

                    throw exception;
                }

                if (result.mutex.WaitOne(0))
                {
                    ThreadStart threadStart = app.GetThreadStart(out int maxStackSize);

                    if (threadStart == null)

                        _throw(new InvalidOperationException(GetThreadStartReturnedNull));

                    await threadStart.StartThread2(maxStackSize).Await();

                    Host.CreateDefaultBuilder()
                       .ConfigureServices(services => services.AddScoped<TInterface, TClass>())
                       .ConfigureIPCHost(builder => builder.AddNamedPipeEndpoint<TInterface>(pipeName))
                       .ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Debug)).Build().Run();
                }

                else
                {
                    result.mutexExists = true;

                    string clientName = app.GetClientName();

                    ServiceProvider serviceProvider = new ServiceCollection()
                        .AddNamedPipeClient<TInterface>(clientName, pipeName: pipeName)
                        .BuildServiceProvider();

                    IClientFactory<TInterface> clientFactory = serviceProvider.GetRequiredService<IClientFactory<TInterface>>();

                    IClient<TInterface> client = clientFactory.CreateClient(clientName);

                    object expression = app.GetExpression();

                    CancellationToken? cancellationToken = app.GetCancellationToken();

                    if (expression == null)
                    {
                        expression = app.GetAsyncExpression();

                        if (expression == null)

                            _throw(new InvalidOperationException(NoExpressionCouldBeRetrieved));

                        else
                        {
                            async Task<TOut> getValueAsync()
                            {
                                var _expression = (Expression<Func<TInterface, Task<TOut>>>)expression;

                                return await (cancellationToken.HasValue ? client.InvokeAsync(_expression, cancellationToken.Value) : client.InvokeAsync(_expression)).Await();
                            }

                            result.serverResult = new NullableGeneric<TOut>(await Await(getValueAsync()));
                        }
                    }

                    else
                    {
                        async Task<TOut> getValueAsync()
                        {
                            var _expression = (Expression<Func<TInterface, TOut>>)expression;

                            return await (cancellationToken.HasValue ? client.InvokeAsync(_expression, cancellationToken.Value) : client.InvokeAsync(_expression)).Await();
                        }

                        result.serverResult = new NullableGeneric<TOut>(await getValueAsync().Await());
                    }
                }
            }

            catch
            {
                if (rethrow)

                    throw;
            }

            return result;
        }
    }

    public unsafe delegate void Action(string[] args, ref ArrayBuilder<string> arrayBuilder, in int* i);

    public interface IQueueBase : ISimpleLinkedListBase
    {
        Task Run();

        string PeekAsString();

        string DequeueAsString();
    }

    public interface IQueue : Collections.DotNetFix.IQueue, IQueueBase
    {

    }

    public interface IQueue<T> : Collections.DotNetFix.Generic.IQueue<T>, IQueueBase
    {

    }

    public abstract class Queue : Collections.DotNetFix.Generic.Queue<string>, IQueue<string>
    {
        string IQueueBase.DequeueAsString() => Dequeue();

        string IQueueBase.PeekAsString() => Peek();

        Task IQueueBase.Run() => Run();

        protected abstract Task Run();
    }

    public interface IUpdater
    {
        int Run(string[] args);
    }

    public abstract class Loader
    {
        public abstract string DefaultKey { get; }

        public abstract IQueueBase DefaultQueue { get; }

        public abstract IDictionary<string, Action> GetActions();

        public abstract System.Collections.Generic.IEnumerable<IQueueBase> GetQueues();
    }

    public abstract class AppLoader<TItems, TApplication> : Loader where TItems : class
    {
        public SingleInstanceApp<TItems, TApplication> App { get; internal set; }
    }

    public static class SingleInstanceApp
    {
        public static unsafe System.Collections.Generic.IEnumerable<string> GetArray(ref ArrayBuilder<string> arrayBuilder, System.Collections.Generic.IEnumerable<string> keys, int* i, params string[] args)
        {
            if (arrayBuilder == null)

                arrayBuilder = new ArrayBuilder<string>();

            else

                arrayBuilder.Clear();

            foreach (string value in new Enumerable<string>(() => new ArrayEnumerator(args, i)).TakeWhile(arg => !keys.Contains(arg)))

                _ = arrayBuilder.AddLast(value);

            // (*i)++;

            return arrayBuilder.ToArray();
        }

        public static string GetAssemblyDirectory() => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static void StartInstance(in string fileName,
#if !NETSTANDARD && CS9
in
#endif
           System.Collections.Generic.IEnumerable<string> parameters)
        {
#if NETSTANDARD || !CS9
            string getString()
            {
                StringBuilder sb = new StringBuilder();

                foreach (string value in parameters)

                    _ = sb.Append($"\"{value}\" ");

                _ = sb.Remove(sb.Length - 1, 1);

                return sb.ToString();
            }
#endif
            System.Diagnostics.Process.Start(GetAssemblyDirectory() + $"\\{fileName}",
#if NETSTANDARD || !CS9
                getString()
#else
                parameters
#endif
                );
        }

        public static void Initialize(in IDictionary<string, IPCService.Extensions.Action> actions, params string[] args)
        {
            ArrayBuilder<string> arrayBuilder = null;
            KeyValuePair<string, IPCService.Extensions.Action> keyValuePair;

            for (int i = 0; i < args.Length;)

                if (actions.FirstOrDefaultValue(_keyValuePair => _keyValuePair.Key == args[i], out keyValuePair))

                    RunAction(ref i, ref arrayBuilder, keyValuePair, args);

            arrayBuilder?.Clear();
        }

        private unsafe delegate void Action(int* i);

        private static unsafe void RunAction(in Action action, int* i)
        {
            (*i)++;

            action(i);
        }

        private static unsafe void RunAction(ref int i, ref ArrayBuilder<string> arrayBuilder, KeyValuePair<string, IPCService.Extensions.Action> keyValuePair, params string[] args)
        {
            int _i = i;
            ArrayBuilder<string> _arrayBuilder = arrayBuilder;

            RunAction(__i => keyValuePair.Value(args, ref _arrayBuilder, __i), &_i);

            i = ++_i;
            arrayBuilder = _arrayBuilder;
        }
    }

    public abstract class SingleInstanceApp<T>
    {
        protected abstract string FileName { get; }

        #region Main Methods
        public async Task MainMutex<TItems, TClass>(ISingleInstanceApp<IUpdater, int, T> app, bool paths, IQueue<TItems> pathQueue) where TClass : class, IUpdater
        {
            (Mutex mutex, bool mutexExists, NullableGeneric<int> serverResult) = await app.StartInstanceAsync<IUpdater, TClass, int>().Await();

            using (mutex)

                if (mutexExists)

                    if (paths && pathQueue == null)

                        await Extensions.StartThread2(() => app.Run(GetDefaultApp<TItems>(null)), 0).Await();

                    else

                        Environment.Exit(serverResult == null ? 0 : serverResult.Value);
        }

        protected abstract Task MainDefault<TClass>() where TClass : class, IUpdater;

        public async Task Main<TClass>(params string[] args) where TClass : class, IUpdater
        {
            if (args.Length == 0)
            {
                await MainDefault<TClass>().Await();

                return;
            }

            Loader loader = GetLoader();

            SingleInstanceApp.Initialize(loader.GetActions(), args);

            IQueueBase defaultArgs = loader.DefaultQueue;

            System.Collections.Generic.IEnumerable<IQueueBase> queues = loader.GetQueues();

            bool runDefault = true;

            async Task run(IQueueBase queue) => await queue.Run().Await();

            foreach (IQueueBase queue in queues)

                if (queue.HasItems)
                {
                    runDefault = false;

                    if (defaultArgs.HasItems)
                    {
                        System.Collections.Generic.IEnumerable<string> pathsToEnumerable()
                        {
                            do
                            {
                                yield return loader.DefaultKey;

                                yield return defaultArgs.DequeueAsString();
                            }

                            while (defaultArgs.HasItems);
                        }

                        SingleInstanceApp.StartInstance(FileName, pathsToEnumerable());
                    }

                    await run(queue).Await();
                }

            if (runDefault)

                await run(defaultArgs).Await();
        }
        #endregion

        protected abstract Loader GetLoader();

        public abstract T GetDefaultApp<TItems>(IQueue<TItems> queue);
    }

    public abstract class SingleInstanceApp<TItems, TApplication> : SingleInstanceApp<TApplication> where TItems : class
    {
        public abstract SingleInstanceAppInstance<IQueue<TItems>, TApplication> GetDefaultSingleInstanceApp(IQueue<TItems> args);

        public async Task MainDefault<T>(IQueue<TItems> args) where T : class, IUpdater => await MainMutex<TItems, T>(GetDefaultSingleInstanceApp(args), true, args).Await();

        protected override Task MainDefault<T>() => MainDefault<T>(null);
    }

    public abstract class SingleInstanceApp2<TItems, TApplication> : SingleInstanceApp<TItems, TApplication> where TItems : class
    {
        protected sealed override Loader GetLoader()
        {
            AppLoader<TItems, TApplication> loader = GetLoaderOverride();

            loader.App = this;

            return loader;
        }

        protected abstract AppLoader<TItems, TApplication> GetLoaderOverride();
    }

    public abstract class SingleInstanceAppInstance<TObject, TApplication> : ISingleInstanceApp<IUpdater, int, TApplication> where TObject : class
    {
        private readonly string _pipeName;

        protected TObject InnerObject { get; private set; }

        protected SingleInstanceAppInstance(in string pipeName, in TObject innerObject)
        {
            _pipeName = pipeName;

            InnerObject = innerObject;
        }

        public string GetPipeName() => _pipeName;

        public abstract string GetClientName();

        public abstract void Run(in TApplication application);

        private void Run()
        {
            TApplication app = GetApp();

            InnerObject = null;

            Run(app);
        }

        public ThreadStart GetThreadStart(out int maxStackSize)
        {
            maxStackSize = 0;

            return Run;
        }

        protected abstract TApplication GetApp();

        protected abstract Expression<Func<IUpdater, int>> GetExpressionOverride();

        public Expression<Func<IUpdater, int>> GetExpression()
        {
            Expression<Func<IUpdater, int>> result = GetExpressionOverride();

            InnerObject = null;

            return result;
        }

        public Expression<Func<IUpdater, Task<int>>> GetAsyncExpression() => null;

        public CancellationToken? GetCancellationToken() => null;
    }

#if !WinCopies4
    public class ArrayEnumerator<T> : Enumerator<T>, ICountableDisposableEnumeratorInfo<T>
    {
        private System.Collections.Generic.IReadOnlyList<T> _array;
        private readonly unsafe int* _currentIndex;
        private readonly int _startIndex;
        private Func<bool> _condition;
        private System.Action _moveNext;

        protected System.Collections.Generic.IReadOnlyList<T> Array => IsDisposed ? throw GetExceptionForDispose(false) : _array;

        public int Count => IsDisposed ? throw GetExceptionForDispose(false) : _array.Count;

        protected unsafe int CurrentIndex => IsDisposed ? throw GetExceptionForDispose(false) : *_currentIndex;

        public unsafe ArrayEnumerator(in System.Collections.Generic.IReadOnlyList<T> array, in bool reverse = false, int* startIndex = null)
        {
            _array = array ?? throw GetArgumentNullException(nameof(array));

            if (startIndex != null && (*startIndex < 0 || *startIndex >= array.Count))

                throw new ArgumentOutOfRangeException(nameof(startIndex), *startIndex, $"The given index is less than zero or greater than or equal to {nameof(array.Count)}.");

            _currentIndex = startIndex;

            if (reverse)
            {
                _startIndex = startIndex == null ? _array.Count - 1 : *startIndex;
                _condition = () => *_currentIndex > 0;
                _moveNext = () => (*_currentIndex)--;
            }

            else
            {
                _startIndex = startIndex == null ? 0 : *startIndex;
                _condition = () => *_currentIndex < _array.Count - 1;
                _moveNext = () => (*_currentIndex)++;
            }
        }

        protected override unsafe T CurrentOverride => _array[*_currentIndex];

        public override bool? IsResetSupported => true;

        protected override bool MoveNextOverride()
        {
            if (_condition())
            {
                _moveNext();

                return true;
            }

            return false;
        }

        protected override unsafe void ResetCurrent() => *_currentIndex = _startIndex;

        protected override void DisposeManaged()
        {
            _array = null;
            _condition = null;
            _moveNext = null;

            Reset();
        }

        protected override void ResetOverride2() { /* Left empty. */ }
    }

    public class CustomEnumeratorProvider<TItems, TEnumerator> : System.Collections.Generic.IEnumerable<TItems> where TEnumerator : System.Collections.Generic.IEnumerator<TItems>
    {
        protected Func<TEnumerator> Func { get; }

        public CustomEnumeratorProvider(in Func<TEnumerator> func) => Func = func;

        public TEnumerator GetEnumerator() => Func();

        System.Collections.Generic.IEnumerator<TItems> System.Collections.Generic.IEnumerable<TItems>.GetEnumerator() => Func();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => Func();
    }

    public class UIntCountableProvider<TItems, TEnumerator> : CustomEnumeratorProvider<TItems, TEnumerator>, Collections.DotNetFix.Generic.IUIntCountableEnumerable<TItems> where TEnumerator : IEnumeratorInfo2<TItems>
    {
        private Func<uint> CountFunc { get; }

        uint IUIntCountable.Count => CountFunc();

        public UIntCountableProvider(in Func<TEnumerator> func, in Func<uint> countFunc) : base(func) => CountFunc = countFunc;

        protected IUIntCountableEnumerator<TItems> GetUIntCountableEnumerator() => new UIntCountableEnumeratorInfo<TItems>(GetEnumerator(), CountFunc);

        IUIntCountableEnumerator<TItems> IUIntCountableEnumerable<TItems, IUIntCountableEnumerator<TItems>>.GetEnumerator() => GetUIntCountableEnumerator();

        IUIntCountableEnumerator<TItems> Collections.Enumeration.DotNetFix.IEnumerable<IUIntCountableEnumerator<TItems>>.GetEnumerator() => GetUIntCountableEnumerator();

        IUIntCountableEnumerator<TItems> Collections.DotNetFix.Generic.IEnumerable<TItems, IUIntCountableEnumerator<TItems>>.GetEnumerator() => GetUIntCountableEnumerator();
    }
#endif

    public class ArrayEnumerator : ArrayEnumerator<string>
    {
        public unsafe ArrayEnumerator(in System.Collections.Generic.IReadOnlyList<string> array, int* startIndex = null) : base(array, false, startIndex) { }

        protected override void ResetCurrent() { }
    }
}
