### [Solamirare](Solamirare.md 'Solamirare')

## PollFd Struct

用于 poll 系统调用的文件描述符结构。
<br/>
Structure for file descriptors used in the poll system call\.

```csharp
public struct PollFd
```

| Fields | |
| :--- | :--- |
| [Events](Solamirare.PollFd.Events.md 'Solamirare\.PollFd\.Events') | 请求的事件掩码。 <br/> Requested events bitmask\. |
| [Fd](Solamirare.PollFd.Fd.md 'Solamirare\.PollFd\.Fd') | 文件描述符。 <br/> The file descriptor\. |
| [Revents](Solamirare.PollFd.Revents.md 'Solamirare\.PollFd\.Revents') | 返回的事件掩码。 <br/> Returned events bitmask\. |
