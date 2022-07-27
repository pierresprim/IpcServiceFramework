# WinCopies.IPCService

## Changelog

### 07/2022 5.0

- SingleInstanceAppInstance: new abstract method: Shutdown. This method is called by the private Run() method when the result of the check it is performing now, whether Environment.ExitCode is still set to zero, returns false.
- WinCopies.IPCService.Extensions (WinCopies.IPCService.Extensions.Windows package):
	- Application: OnStartup override checks if MainWindow is not null. If it is, the method returns directly.
	- SingleInstanceAppInstance\<T>: default override for the new abstract Shutdown method of WinCopies.IPCService.Extensions.SingleInstanceAppInstance (WinCopies.IPCService.Extensions package).

### 07/2022 4.2.1

- Bug fixed in WinCopies.IPCService.Extensions.Application (WinCopies.IPCService.Extensions package): the application exited after the collection of open windows changed, even if it was still containing windows.
- WinCopies.IPCService.Extensions.Application (WinCopies.IPCService.Extensions.Windows package): OnStartup method calls its base override at the end of the method.

### 11/04/2021 4.2

Update to WinCopies.Util packages 3.16.

### 09/05/2021 4.1

Add WinCopies.IPCService.Extensions.Windows package and upgrade WinCopies.IPCService.Extensions package.

### 07/09/2021 4.0.0.1

Add WinCopies.IPCService.Extensions package.

### 06/25/2021 4.0

Re-designed to be included in the WinCopies Framework