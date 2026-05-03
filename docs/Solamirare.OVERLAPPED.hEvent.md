### [Solamirare](Solamirare.md 'Solamirare').[OVERLAPPED](Solamirare.OVERLAPPED.md 'Solamirare\.OVERLAPPED')

## OVERLAPPED\.hEvent Field

事件句柄或用户自定义数据。
如果在关联 IOCP 时使用，此句柄通常设为 NULL。
也可以存放由 `CreateEvent` 创建的同步事件，当操作完成时系统会将该事件设为有信号状态。

```csharp
public void* hEvent;
```

#### Field Value
[System\.Void](https://learn.microsoft.com/en-us/dotnet/api/system.void 'System\.Void')*