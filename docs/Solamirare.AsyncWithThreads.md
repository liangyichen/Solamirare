### [Solamirare](Solamirare.md 'Solamirare')

## AsyncWithThreads Struct

基于线程池的异步操作。
<br/>
asynchronous operation wrapper based on thread pool\.

```csharp
public struct AsyncWithThreads
```

| Constructors | |
| :--- | :--- |
| [AsyncWithThreads\(\)](Solamirare.AsyncWithThreads.AsyncWithThreads().md 'Solamirare\.AsyncWithThreads\.AsyncWithThreads\(\)') | 初始化异步操作实例，从线程池获取线程。 <br/> Initializes the async operation instance, acquiring a thread from the thread pool\. |

| Methods | |
| :--- | :--- |
| [Wait\(\)](Solamirare.AsyncWithThreads.Wait().md 'Solamirare\.AsyncWithThreads\.Wait\(\)') | 等待当前任务完成，把线程归还到线程池。 |
