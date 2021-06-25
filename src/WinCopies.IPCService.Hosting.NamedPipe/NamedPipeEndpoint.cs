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

using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace WinCopies.IPCService.Hosting.NamedPipe
{
    public class NamedPipeEndpointOptions : EndpointOptions
    {
        public string PipeName { get; set; }
    }

    public class NamedPipeEndpoint<TContract> : Endpoint<TContract>
        where TContract : class
    {
        private readonly NamedPipeEndpointOptions _options;

        public NamedPipeEndpoint(NamedPipeEndpointOptions options, ILogger<NamedPipeEndpoint<TContract>> logger, IServiceProvider serviceProvider) : base(options, serviceProvider, logger) => _options = options;

        protected override async Task WaitAndProcessAsync(Func<System.IO.Stream, CancellationToken, Task> process, CancellationToken cancellationToken)
        {
            if (process is null)

                throw new ArgumentNullException(nameof(process));

            // https://github.com/PowerShell/PowerShellEditorServices/blob/f45c6312a859cde4aa25ea347a345e1d35238350/src/PowerShellEditorServices.Protocol/MessageProtocol/Channel/NamedPipeServerListener.cs#L38-L67
            // Unfortunately, .NET Core does not support passing in a PipeSecurity object into the constructor for
            // NamedPipeServerStream so we are creating native Named Pipes and securing them using native APIs.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var pipeSecurity = new PipeSecurity();
                var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                var psRule = new PipeAccessRule(everyone, PipeAccessRights.FullControl, System.Security.AccessControl.AccessControlType.Allow);

                pipeSecurity.AddAccessRule(psRule);

                using (NamedPipeServerStream server = Microsoft.WindowsAPICodePack.Win32Native.NamedPipe.NamedPipe.CreateNamedPipe(_options.PipeName, (uint)_options.MaxConcurrentCalls, pipeSecurity))
                {
                    await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                    await process(server, cancellationToken).ConfigureAwait(false);
                }
            }

            // Use original logic on other platforms.
            else

                using (var server = new NamedPipeServerStream(_options.PipeName, PipeDirection.InOut, _options.MaxConcurrentCalls,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                {
                    await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                    await process(server, cancellationToken).ConfigureAwait(false);
                }
        }
    }
}
