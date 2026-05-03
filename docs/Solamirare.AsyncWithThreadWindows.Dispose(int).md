### [Solamirare](Solamirare.md 'Solamirare').[AsyncWithThreadWindows](Solamirare.AsyncWithThreadWindows.md 'Solamirare\.AsyncWithThreadWindows')

## AsyncWithThreadWindows\.Dispose\(int\) Method

释放资源并停止后台线程。
<br/>
Releases resources and stops the background thread\.

```csharp
public void Dispose(int gracefulShutdownTimeoutMilliseconds=3000);
```
#### Parameters

<a name='Solamirare.AsyncWithThreadWindows.Dispose(int).gracefulShutdownTimeoutMilliseconds'></a>

`gracefulShutdownTimeoutMilliseconds` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

优雅退出的最大等待时间（毫秒）。如果超时，线程将被强制终止。默认 3000ms。
<br/>
The maximum wait time for graceful shutdown in milliseconds\. If timed out, the thread will be forcibly terminated\. Default is 3000ms\.