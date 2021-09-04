using System;
using System.Windows;

using WinCopies.Collections.DotNetFix.Generic;

using static WinCopies.ThrowHelper;

namespace WinCopies.IPCService.Extensions
{
    public abstract class Application : System.Windows.Application
    {
        public bool IsClosing { get; protected set; }

        protected internal ObservableLinkedCollection<Window> _OpenWindows { get; } = new ObservableLinkedCollection<Window>();

        public IUIntCountableEnumerable<Window> OpenWindows { get; internal set; }

        public static ResourceDictionary GetResourceDictionary(in string name) => new
#if !CS9
            ResourceDictionary
#endif
            ()
        { Source = new Uri(name, UriKind.Relative) };

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _OpenWindows.CollectionChanged += OpenWindows_CollectionChanged;

            //MainWindow = new MainWindow();

            //MainWindow.Closed += MainWindow_Closed;

            OnStartup2(e);

            MainWindow.Show();
        }

        protected abstract void OnStartup2(StartupEventArgs e);

        private void OpenWindows_CollectionChanged(object sender, LinkedCollectionChangedEventArgs<Window> e) => Environment.Exit(0);

#if !NETSTANDARD
        protected static ObservableLinkedCollection<Window> GetOpenWindows(in Application app) => GetOrThrowIfNull(app, nameof(app))._OpenWindows;

        protected static void SetOpenWindows(in Application app, in IUIntCountableEnumerable<Window> enumerable) => GetOrThrowIfNull(app, nameof(app)).OpenWindows = enumerable;
#endif
    }

    namespace Windows
    {
        public abstract class SingleInstanceAppInstance<T> : SingleInstanceAppInstance<T, System.Windows.Application> where T : class
        {
            protected SingleInstanceAppInstance(in string pipeName, in T innerObject) : base(pipeName, innerObject)
            {
                // Left empty.
            }

            public sealed override void Run(in System.Windows.Application application) => GetOrThrowIfNull(application, nameof(application)).Run();
        }
    }
}
