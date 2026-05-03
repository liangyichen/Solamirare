### [Solamirare](Solamirare.md 'Solamirare').[OVERLAPPED](Solamirare.OVERLAPPED.md 'Solamirare\.OVERLAPPED')

## OVERLAPPED\.Offset Field

文件偏移量的低 32 位。
指定开始 I/O 操作的文件位置。对于不支持寻址的设备（如管道或通信设备），此值必须为 0。

```csharp
public uint Offset;
```

#### Field Value
[System\.UInt32](https://learn.microsoft.com/en-us/dotnet/api/system.uint32 'System\.UInt32')