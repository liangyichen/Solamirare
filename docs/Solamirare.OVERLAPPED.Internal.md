### [Solamirare](Solamirare.md 'Solamirare').[OVERLAPPED](Solamirare.OVERLAPPED.md 'Solamirare\.OVERLAPPED')

## OVERLAPPED\.Internal Field

系统保留字段。
用于保存与系统相关的状态。在 I/O 操作开始前应初始化为 0。
异步操作完成后，此字段包含 I/O 处理的具体状态代码（如 `STATUS_PENDING`）。

```csharp
public ulong Internal;
```

#### Field Value
[System\.UInt64](https://learn.microsoft.com/en-us/dotnet/api/system.uint64 'System\.UInt64')