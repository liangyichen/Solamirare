### [Solamirare](Solamirare.md 'Solamirare')

## AsyncStateOnPosix Struct

表示 macOS 与 Linux 共用的 Posix 异步状态结构。

```csharp
public struct AsyncStateOnPosix
```

| Fields | |
| :--- | :--- |
| [Callback](Solamirare.AsyncStateOnPosix.Callback.md 'Solamirare\.AsyncStateOnPosix\.Callback') | 异步操作完成时执行的回调函数指针。 |
| [IsFinished](Solamirare.AsyncStateOnPosix.IsFinished.md 'Solamirare\.AsyncStateOnPosix\.IsFinished') | 标记异步任务是否已完成。 |
| [ReadFd](Solamirare.AsyncStateOnPosix.ReadFd.md 'Solamirare\.AsyncStateOnPosix\.ReadFd') | 读取端文件描述符。 |
| [ShouldStop](Solamirare.AsyncStateOnPosix.ShouldStop.md 'Solamirare\.AsyncStateOnPosix\.ShouldStop') | 是否应停止当前异步流程。 |
| [State](Solamirare.AsyncStateOnPosix.State.md 'Solamirare\.AsyncStateOnPosix\.State') | 当前状态码。 |
| [UserData](Solamirare.AsyncStateOnPosix.UserData.md 'Solamirare\.AsyncStateOnPosix\.UserData') | 传递给回调函数的用户数据。 |
| [WriteFd](Solamirare.AsyncStateOnPosix.WriteFd.md 'Solamirare\.AsyncStateOnPosix\.WriteFd') | 写入端文件描述符。 |
