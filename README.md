# IpcServiceFramework

A .NET Core 3.1 based lightweight framework for efficient inter-process communication.
Named pipeline and TCP support out-of-the-box, extensible with other protocols.

Forked from [https://github.com/jacqueskang/IpcServiceFramework](jacqueskang/IpcServiceFramework)

## NuGet packages
| Name | Purpose | Status |
| ---- | ------- | ------ |
| WinCopies.IPCService.Client.NamedPipe | Client SDK to consume IPC service over Named pipe | [![NuGet version](https://badge.fury.io/nu/WinCopies.IPCService.Client.NamedPipe.svg)](https://badge.fury.io/nu/WinCopies.IPCService.Client.NamedPipe) |
| WinCopies.IPCService.Hosting.NamedPipe | Server SDK to run Named pipe IPC service endpoint | [![NuGet version](https://badge.fury.io/nu/WinCopies.IPCService.Hosting.NamedPipe.svg)](https://badge.fury.io/nu/WinCopies.IPCService.Hosting.NamedPipe) |
| WinCopies.IPCService.Extensions(.Windows) | SDK to build single-instance applications | [![NuGet version](https://badge.fury.io/nu/WinCopies.IPCService.Extensions.svg)](https://badge.fury.io/nu/WinCopies.IPCService.Extensions) [![NuGet version](https://badge.fury.io/nu/WinCopies.IPCService.Extensions.Windows.svg)](https://badge.fury.io/nu/WinCopies.IPCService.Extensions.Windows) |


## Usage

 1. Create an interface as service contract and package it in an assembly to be referenced by server and client applications, for example:

    ```csharp
    public interface IInterProcessService
    {
        string ReverseString(string input);
    }
    ```

 1. Implement the service in server application, for example:
 
    ```csharp
    class InterProcessService : IInterProcessService
    {
        public string ReverseString(string input)
        {
            char[] charArray = input.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }
    ```

 1. Install the following NuGet packages in server application:

    ```powershell
    > Install-Package Microsoft.Extensions.Hosting
    > Install-Package WinCopies.IPCService.Hosting.NamedPipe
    ```

 1. Register the service implementation and configure IPC endpoint(s):

    ```csharp
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddScoped<IInterProcessService, InterProcessService>();
                })
                .ConfigureIpcHost(builder =>
                {
                    // configure IPC endpoints
                    builder.AddNamedPipeEndpoint<IInterProcessService>(pipeName: "pipeinternal");
                })
                .ConfigureLogging(builder =>
                {
                    // optionally configure logging
                    builder.SetMinimumLevel(LogLevel.Information);
                });
    }
    ```

 1. Install the following NuGet package in client application:

    ```powershell
    > Install-Package WinCopies.IPCService.Client.NamedPipe
    ```

 1. Invoke the server

    ```csharp
    // register IPC clients
    ServiceProvider serviceProvider = new ServiceCollection()
        .AddNamedPipeIpcClient<IInterProcessService>("client1", pipeName: "pipeinternal")
        .BuildServiceProvider();

    // resolve IPC client factory
    IClientFactory<IInterProcessService> clientFactory = serviceProvider
        .GetRequiredService<IClientFactory<IInterProcessService>>();

    // create client
    IClient<IInterProcessService> client = clientFactory.CreateClient("client1");

    string output = await client.InvokeAsync(x => x.ReverseString(input));
    ```

## FAQs


