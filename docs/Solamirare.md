## Solamirare Namespace

| Classes | |
| :--- | :--- |
| [AsyncFilesIO](Solamirare.AsyncFilesIO.md 'Solamirare\.AsyncFilesIO') | 异步文件操作 |
| [AsyncFilesIOWithCallBack](Solamirare.AsyncFilesIOWithCallBack.md 'Solamirare\.AsyncFilesIOWithCallBack') | Provides platform\-dispatched asynchronous file I/O APIs that report completion via unmanaged callbacks\. |
| [Coroutine](Solamirare.Coroutine.md 'Solamirare\.Coroutine') | 跨平台协程操作入口。 |
| [CoroutineScheduler](Solamirare.CoroutineScheduler.md 'Solamirare\.CoroutineScheduler') | 跨平台协程调度器管理。   Create / Destroy 用于管理调度器生命周期。 Resume / Yield 保留原有签名供需要显式传入 scheduler 的场景使用， 日常使用推荐改用 [Resume\(void\*\)](Solamirare.Coroutine.Resume(void_).md 'Solamirare\.Coroutine\.Resume\(void\*\)') 和 [Yield\(void\*\)](Solamirare.Coroutine.Yield(void_).md 'Solamirare\.Coroutine\.Yield\(void\*\)')。 |
| [CoroutineStack](Solamirare.CoroutineStack.md 'Solamirare\.CoroutineStack') | 跨平台协程栈初始化入口。   整个进程只需调用一次 [Solamirare\.CoroutineStack\.Initialize\(System\.UIntPtr,System\.UIntPtr\)](https://learn.microsoft.com/en-us/dotnet/api/solamirare.coroutinestack.initialize#solamirare-coroutinestack-initialize(system-uintptr-system-uintptr) 'Solamirare\.CoroutineStack\.Initialize\(System\.UIntPtr,System\.UIntPtr\)')， 内部根据当前平台转发到对应的实现。 |
| [DebugHelper](Solamirare.DebugHelper.md 'Solamirare\.DebugHelper') | |
| [DictionaryMathUtils](Solamirare.DictionaryMathUtils.md 'Solamirare\.DictionaryMathUtils') | Provides helper methods for dictionary capacity and threshold calculations\. |
| [EPoolConsts](Solamirare.EPoolConsts.md 'Solamirare\.EPoolConsts') | 封装了 POSIX 标准及平台特定的底层网络系统调用常量。 |
| [FileMode](Solamirare.FileMode.md 'Solamirare\.FileMode') | 文件权限模式 |
| [Fingerprint](Solamirare.Fingerprint.md 'Solamirare\.Fingerprint') | |
| [GenericHttpClientFunctions](Solamirare.GenericHttpClientFunctions.md 'Solamirare\.GenericHttpClientFunctions') | 提供跨平台 HTTP 客户端内部使用的辅助函数。 |
| [HttpClientHelper](Solamirare.HttpClientHelper.md 'Solamirare\.HttpClientHelper') | |
| [HttpDateGenerator](Solamirare.HttpDateGenerator.md 'Solamirare\.HttpDateGenerator') | 提供 HTTP 日期头的 RFC 1123 格式生成工具。 |
| [IOCPIO](Solamirare.IOCPIO.md 'Solamirare\.IOCPIO') | 异步文件读写，等待版本（挂起等待，不占用CPU），但是主线程会阻塞， 不适合用于UI环境 |
| [IOCPIOWithCallBack](Solamirare.IOCPIOWithCallBack.md 'Solamirare\.IOCPIOWithCallBack') | 异步文件读写，回调函数版本 |
| [IoUringLibcs](Solamirare.IoUringLibcs.md 'Solamirare\.IoUringLibcs') | 封装了 Linux io\_uring 核心系统调用、操作码及相关底层常量。 |
| [IO\_URingConsts](Solamirare.IO_URingConsts.md 'Solamirare\.IO\_URingConsts') | IO\_URing 常量 |
| [IO\_UringIO](Solamirare.IO_UringIO.md 'Solamirare\.IO\_UringIO') | 异步文件读写，等待版本（挂起等待，不占用CPU），但是主线程会阻塞， 不适合用于UI环境 |
| [IO\_URingIOWithCallBack](Solamirare.IO_URingIOWithCallBack.md 'Solamirare\.IO\_URingIOWithCallBack') | 异步文件读写，回调函数版本 |
| [JsonSerializes](Solamirare.JsonSerializes.md 'Solamirare\.JsonSerializes') | 序列化类型通用库 |
| [JsonValidator](Solamirare.JsonValidator.md 'Solamirare\.JsonValidator') | 提供基于 [System\.Text\.Json\.Utf8JsonReader](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.utf8jsonreader 'System\.Text\.Json\.Utf8JsonReader') 的 JSON 合法性校验功能。 |
| [LinuxAIO](Solamirare.LinuxAIO.md 'Solamirare\.LinuxAIO') | 异步文件读写，等待版本（挂起等待，不占用CPU），但是主线程会阻塞， 不适合用于UI环境 |
| [LinuxAIOWithCallBack](Solamirare.LinuxAIOWithCallBack.md 'Solamirare\.LinuxAIOWithCallBack') | 异步文件读写，回调函数版本 |
| [LinuxAPI](Solamirare.LinuxAPI.md 'Solamirare\.LinuxAPI') | Linux 系统原生 API 封装。   维护提示：虽然 macOS 与 Linux 在部分 API 上的签名完全相同，但是考虑到两个系统的子版本以及更新频率完全不一样，             随着时间的推移，共用 POSIX API 会带来潜在隐患，所以两个系统必须独立使用自己的 API。 |
| [LinuxCoroutineStack](Solamirare.LinuxCoroutineStack.md 'Solamirare\.LinuxCoroutineStack') | Linux 协程栈内存池。   与 macOS 版本相同，在程序启动时一次性分配整块内存， 内部按固定槽大小切割，每个协程占用一个槽，通过 bitmap 管理空闲槽。 协程结束后归还槽供后续协程复用。 |
| [LinuxFilesIOSwitch](Solamirare.LinuxFilesIOSwitch.md 'Solamirare\.LinuxFilesIOSwitch') | Stores the current Linux file\-I/O backend preference\. |
| [LinuxOpenSSLWrapper](Solamirare.LinuxOpenSSLWrapper.md 'Solamirare\.LinuxOpenSSLWrapper') | 提供 Linux 平台下的 OpenSSL 动态绑定封装。 |
| [LinuxPointerChecker](Solamirare.LinuxPointerChecker.md 'Solamirare\.LinuxPointerChecker') | |
| [LinuxServerBootstrap](Solamirare.LinuxServerBootstrap.md 'Solamirare\.LinuxServerBootstrap') | Linux 服务器启动引导。 在启动时读取内核版本号，按以下策略选择服务器实现：   · 内核 \>= 6\.15 → IoUringServer（io\_uring 原生异步，性能最优）   · 内核 \<  6\.15 → EPoolServer （epoll ET 模式，兼容旧内核）  版本读取通过 uname\(2\) 系统调用完成，零 GC，无文件 I/O。 |
| [MacOSAIO](Solamirare.MacOSAIO.md 'Solamirare\.MacOSAIO') | 异步文件读写，等待版本（挂起等待，不占用CPU），但是主线程会阻塞， 不适合用于UI环境 |
| [MacOSAIOWithCallBack](Solamirare.MacOSAIOWithCallBack.md 'Solamirare\.MacOSAIOWithCallBack') | 异步文件读写，回调函数版本 |
| [MacOSAPI](Solamirare.MacOSAPI.md 'Solamirare\.MacOSAPI') | macOS 系统原生 API 封装。   维护提示：虽然 macOS 与 Linux 在部分 API 上的签名完全相同，但是考虑到两个系统的子版本以及更新频率完全不一样，             随着时间的推移，共用 POSIX API 会带来潜在隐患，所以两个系统必须独立使用自己的 API。 |
| [MacOSCoroutineStack](Solamirare.MacOSCoroutineStack.md 'Solamirare\.MacOSCoroutineStack') | 全局静态协程栈内存池。   在程序启动时通过 [Solamirare\.MacOSCoroutineStack\.Initialize\(System\.UIntPtr,System\.UIntPtr\)](https://learn.microsoft.com/en-us/dotnet/api/solamirare.macoscoroutinestack.initialize#solamirare-macoscoroutinestack-initialize(system-uintptr-system-uintptr) 'Solamirare\.MacOSCoroutineStack\.Initialize\(System\.UIntPtr,System\.UIntPtr\)') 一次性分配整块内存， 内部按固定槽大小切割，每个协程占用一个槽。 协程结束后归还槽，供后续协程复用。  线程安全：槽的分配和归还通过 SpinLock 保护，支持多线程并发创建协程。 |
| [MacOSHttpPosixApi](Solamirare.MacOSHttpPosixApi.md 'Solamirare\.MacOSHttpPosixApi') | 提供 macOS 平台 HTTP 客户端使用的 POSIX Socket 常量与结构体定义。 |
| [MacOSOpenSSLWrapper](Solamirare.MacOSOpenSSLWrapper.md 'Solamirare\.MacOSOpenSSLWrapper') | 提供 macOS 平台下的 OpenSSL 动态绑定封装。 |
| [MacOSPlatformSwitch](Solamirare.MacOSPlatformSwitch.md 'Solamirare\.MacOSPlatformSwitch') | 平台切换层，所有平台相关的操作都通过此类进行。   上层的 Coroutine 和 CoroutineScheduler 只调用此类， 不直接接触任何平台 API 或汇编函数。 |
| [MacOsPointerChecker](Solamirare.MacOsPointerChecker.md 'Solamirare\.MacOsPointerChecker') | A class to determine the memory type \(Stack, Heap, Unknown\) of a pointer on macOS\. It uses P/Invoke to call native macOS APIs \(pthread and Mach\)\. |
| [MemoryAlignmentHelper](Solamirare.MemoryAlignmentHelper.md 'Solamirare\.MemoryAlignmentHelper') | 内存对齐计算 |
| [MemoryTypeChecker](Solamirare.MemoryTypeChecker.md 'Solamirare\.MemoryTypeChecker') | 内存类型检测 |
| [NativeThread](Solamirare.NativeThread.md 'Solamirare\.NativeThread') | 跨平台原生线程封装 \(Zero GC\)   自动适配 Windows (CreateThread) 和 POSIX (pthread) 的差异。  统一约定：线程函数签名为 `delegate* unmanaged<void*, uint>`（Windows 风格）。 POSIX 平台通过内部适配器将 uint 退出码装入 void* 返回，对调用方完全透明。 |
| [NativeThreadPool](Solamirare.NativeThreadPool.md 'Solamirare\.NativeThreadPool') | 原生零 GC 线程池   使用非托管内存管理任务队列，使用原生同步原语（Mutex/Cond/Semaphore）进行线程同步。 |
| [Posix\_Functions](Solamirare.Posix_Functions.md 'Solamirare\.Posix\_Functions') | MAC, LINUX 通用 IO 功能 |
| [ServerFunctions](Solamirare.ServerFunctions.md 'Solamirare\.ServerFunctions') | 服务器通用逻辑 |
| [ServerVariables](Solamirare.ServerVariables.md 'Solamirare\.ServerVariables') | 服务器变量集合 |
| [SolamirareGlobal](Solamirare.SolamirareGlobal.md 'Solamirare\.SolamirareGlobal') | |
| [SolamirareJsonGenerator](Solamirare.SolamirareJsonGenerator.md 'Solamirare\.SolamirareJsonGenerator') | 简单 json 处理器， json 字符串类型仅限平面类型，不能有深度嵌套   合法例子： {"name":"my name", "age":100}  非法例子： {"name":"my name", "age":100, “pet”:{"name":"wowo"}} |
| [SyncFilesIO](Solamirare.SyncFilesIO.md 'Solamirare\.SyncFilesIO') | 常用的 IO 同步模式操作 |
| [UDictionaryExtension](Solamirare.UDictionaryExtension.md 'Solamirare\.UDictionaryExtension') | 字典功能扩展 |
| [UnManagedCollectionExtension](Solamirare.UnManagedCollectionExtension.md 'Solamirare\.UnManagedCollectionExtension') | 提供针对字符型 [UnManagedCollection&lt;T&gt;](Solamirare.UnManagedCollection_T_.md 'Solamirare\.UnManagedCollection\<T\>') 的扩展方法。 |
| [UnManagedMemoryHelper](Solamirare.UnManagedMemoryHelper.md 'Solamirare\.UnManagedMemoryHelper') | 创建非托管内存   Creates unmanaged memory. |
| [UnmanagedMemorySorter](Solamirare.UnmanagedMemorySorter.md 'Solamirare\.UnmanagedMemorySorter') | 排序功能 |
| [UnManagedMemory\_Extension](Solamirare.UnManagedMemory_Extension.md 'Solamirare\.UnManagedMemory\_Extension') | UnManagedMemory 扩展方法 |
| [UnManagedStringHelper](Solamirare.UnManagedStringHelper.md 'Solamirare\.UnManagedStringHelper') | |
| [ValueTypeHelper](Solamirare.ValueTypeHelper.md 'Solamirare\.ValueTypeHelper') | Provides short\-buffer search helpers for unmanaged sequences\. |
| [WindowsAPI](Solamirare.WindowsAPI.md 'Solamirare\.WindowsAPI') | |
| [WindowsCoroutineStack](Solamirare.WindowsCoroutineStack.md 'Solamirare\.WindowsCoroutineStack') | Windows 协程栈配置与并发上限管理。   Windows 平台的栈内存由 Win32 CreateFiber 按需分配，无需预分配整块内存。 此类只负责记录单个栈大小和最大并发数， 超出并发上限后所有公共方法立即返回，停止一切执行。 |
| [WindowsHttpApi](Solamirare.WindowsHttpApi.md 'Solamirare\.WindowsHttpApi') | 提供 Windows 平台 HTTP 客户端使用的 WinSock 常量、结构体与薄封装方法。 |
| [WindowsOpenSSLWrapper](Solamirare.WindowsOpenSSLWrapper.md 'Solamirare\.WindowsOpenSSLWrapper') | 提供 Windows 平台下的 OpenSSL 动态绑定封装。 |
| [WindowsPointerChecker](Solamirare.WindowsPointerChecker.md 'Solamirare\.WindowsPointerChecker') | |
| [ZeroDirectory\_Windows](Solamirare.ZeroDirectory_Windows.md 'Solamirare\.ZeroDirectory\_Windows') | |
| [ZeroIO\_Windows](Solamirare.ZeroIO_Windows.md 'Solamirare\.ZeroIO\_Windows') | |

| Structs | |
| :--- | :--- |
| [AsyncFilesIOContext](Solamirare.AsyncFilesIOContext.md 'Solamirare\.AsyncFilesIOContext') | 文件操作上下文 |
| [AsyncHttpClient](Solamirare.AsyncHttpClient.md 'Solamirare\.AsyncHttpClient') | 跨平台异步 HTTP 客户端包装器（零 GC、回调）。  自动选择与当前操作系统相适应的异步模型： \- macOS: Kqueue \+ GCD \- Windows: IOCP \+ 工作线程 \- Linux: IO\_URing \+ 后台收割线程  提供统一的同步API，使用者无需关心底层平台差异。 |
| [AsyncLinuxHttpClient](Solamirare.AsyncLinuxHttpClient.md 'Solamirare\.AsyncLinuxHttpClient') | io\_uring \+ Socket 异步 HTTP 客户端（零 GC、回调）。   使用 io_uring 的 IORING_OP_CONNECT / IORING_OP_SEND / IORING_OP_RECV 实现全异步网络操作，不阻塞调用线程。 |
| [AsyncMacOSHttpClient](Solamirare.AsyncMacOSHttpClient.md 'Solamirare\.AsyncMacOSHttpClient') | 纯原生 Kqueue \+ Socket 异步 HTTP 客户端（零 GC、回调）。 参考 MacOSHttpServer 的 Kqueue 设计风格（EVFILT\_READ/WRITE、EV\_ERROR、FIONBIO、SO\_NOSIGPIPE）。 |
| [AsyncRequestContext](Solamirare.AsyncRequestContext.md 'Solamirare\.AsyncRequestContext') | 请求上下文 |
| [AsyncStateOnPosix](Solamirare.AsyncStateOnPosix.md 'Solamirare\.AsyncStateOnPosix') | 表示 macOS 与 Linux 共用的 Posix 异步状态结构。 |
| [AsyncStateOnWindows](Solamirare.AsyncStateOnWindows.md 'Solamirare\.AsyncStateOnWindows') | Windows 平台上到异步状态机 |
| [AsyncWindowsHttpClient](Solamirare.AsyncWindowsHttpClient.md 'Solamirare\.AsyncWindowsHttpClient') | 纯原生 IOCP \+ Socket 异步 HTTP 客户端（零 GC、回调）。 使用 Windows I/O Completion Ports 实现完全非阻塞的异步操作。 |
| [AsyncWindowsHttpClient\.InternalContext](Solamirare.AsyncWindowsHttpClient.InternalContext.md 'Solamirare\.AsyncWindowsHttpClient\.InternalContext') | 内部上下文扩展，确保 OVERLAPPED 结构紧跟其后以便 IOCP 转换。 |
| [AsyncWithThreadLinux](Solamirare.AsyncWithThreadLinux.md 'Solamirare\.AsyncWithThreadLinux') | Linux 版本的零 GC 异步 |
| [AsyncWithThreadMac](Solamirare.AsyncWithThreadMac.md 'Solamirare\.AsyncWithThreadMac') | MacOS 版本的零 GC 异步 |
| [AsyncWithThreads](Solamirare.AsyncWithThreads.md 'Solamirare\.AsyncWithThreads') | 基于线程池的异步操作。 <br/> asynchronous operation wrapper based on thread pool\. |
| [AsyncWithThreadWindows](Solamirare.AsyncWithThreadWindows.md 'Solamirare\.AsyncWithThreadWindows') | Windows 版本的零 GC 异步 |
| [BaseMemoryPoolUserBlockInfo](Solamirare.BaseMemoryPoolUserBlockInfo.md 'Solamirare\.BaseMemoryPoolUserBlockInfo') | 内存块信息，用户端 |
| [CircularDeque&lt;T&gt;](Solamirare.CircularDeque_T_.md 'Solamirare\.CircularDeque\<T\>') | 双端队列，支持在两端高效地添加和移除元素。 <br/> Double\-ended queue, supports efficient addition and removal of elements at both ends\. |
| [CoroutineDebugInfo](Solamirare.CoroutineDebugInfo.md 'Solamirare\.CoroutineDebugInfo') | 协程调试信息，记录 Resume / Yield 次数及异常检测结果。   仅在编译时定义 COROUTINE_DEBUG 宏时有效。 |
| [DictionaryEnumerator&lt;TKey,TValue&gt;](Solamirare.DictionaryEnumerator_TKey,TValue_.md 'Solamirare\.DictionaryEnumerator\<TKey,TValue\>') | 通用的非托管字典迭代器，支持 Swiss Table 和 Double Hashing 布局 |
| [DictionarySlot&lt;TKey,TValue&gt;](Solamirare.DictionarySlot_TKey,TValue_.md 'Solamirare\.DictionarySlot\<TKey,TValue\>') | Dictionary 节点数据槽，存储 Key 和 Value |
| [epoll\_event](Solamirare.epoll_event.md 'Solamirare\.epoll\_event') | `epoll_event` 结构体用于在调用 `epoll_ctl` 时指定感兴趣的事件，             并在调用 `epoll_wait` 时报告发生的事件。 |
| [EPoolServer](Solamirare.EPoolServer.md 'Solamirare\.EPoolServer') | 基于 Linux epoll 机制的零 GC 单线程 HTTP 服务器。 所有 I/O 均在单一事件循环线程内完成，无锁、无线程切换开销。   A zero-GC single-threaded HTTP server based on the Linux epoll mechanism. All I/O is performed within a single event loop thread, with no locks or thread-switching overhead. |
| [hostent](Solamirare.hostent.md 'Solamirare\.hostent') | 表示主机数据库条目的结构。 用于存储主机的名称、别名、地址类型及地址列表等信息。 |
| [HTTPSeverConfig](Solamirare.HTTPSeverConfig.md 'Solamirare\.HTTPSeverConfig') | HTTP 服务器设置 |
| [IOCPContext](Solamirare.IOCPContext.md 'Solamirare\.IOCPContext') | 文件操作上下文 |
| [IOCPContextWithCallBack](Solamirare.IOCPContextWithCallBack.md 'Solamirare\.IOCPContextWithCallBack') | Holds the state for a Windows IOCP asynchronous file I/O callback request\. |
| [IoUringOptimizationOptions](Solamirare.IoUringOptimizationOptions.md 'Solamirare\.IoUringOptimizationOptions') | 运行时优化开关。 |
| [IoUringServer](Solamirare.IoUringServer.md 'Solamirare\.IoUringServer') | io\_uring\-based HTTP server\. 基于 io\_uring 的 HTTP 服务。 |
| [iovec](Solamirare.iovec.md 'Solamirare\.iovec') | |
| [IO\_URingContext](Solamirare.IO_URingContext.md 'Solamirare\.IO\_URingContext') | 文件操作上下文 |
| [IO\_URingContextWithCallBack](Solamirare.IO_URingContextWithCallBack.md 'Solamirare\.IO\_URingContextWithCallBack') | Holds the state for an io\_uring asynchronous file I/O callback request\. |
| [IO\_URingRequestContext](Solamirare.IO_URingRequestContext.md 'Solamirare\.IO\_URingRequestContext') | IO\_URing 请求上下文 |
| [JsonDocument](Solamirare.JsonDocument.md 'Solamirare\.JsonDocument') | 提供 JSON 紧凑化功能。 |
| [JsonDocument\.CompactResult](Solamirare.JsonDocument.CompactResult.md 'Solamirare\.JsonDocument\.CompactResult') | 表示 JSON 压缩操作的结果。 |
| [JsonDocument\.ParseResult](Solamirare.JsonDocument.ParseResult.md 'Solamirare\.JsonDocument\.ParseResult') | 表示字符串解析为 JSON 字符串值后的结果状态。 |
| [JsonNode](Solamirare.JsonNode.md 'Solamirare\.JsonNode') | 表示 JSON 文档中的一个节点。 |
| [JsonParsingArena](Solamirare.JsonParsingArena.md 'Solamirare\.JsonParsingArena') | JSON 解析专用内存区域分配器 \(Arena Allocator\)   用于批量管理解析过程中产生的短字符串内存，减少系统调用并提高缓存局部性。 |
| [KernelVersion](Solamirare.KernelVersion.md 'Solamirare\.KernelVersion') | 内核版本号（Major\.Minor\.Patch），值类型，零堆分配。 不实现任何接口——struct 实现接口后通过接口变量调用会装箱（boxing）产生 GC。 CompareTo 使用直接整数运算，比较运算符直接调用它，无任何间接分配。 |
| [KQueueEvent64](Solamirare.KQueueEvent64.md 'Solamirare\.KQueueEvent64') | 表示 macOS/BSD 系统中的 64 位内核事件结构 \(kevent64\_s\)。 用于 `kqueue` 机制中的事件注册、修改以及获取待处理的内核事件。 |
| [KQueueLibcs](Solamirare.KQueueLibcs.md 'Solamirare\.KQueueLibcs') | 提供 macOS KQueue 网络编程相关的常量定义。 |
| [LinuxAIOContext](Solamirare.LinuxAIOContext.md 'Solamirare\.LinuxAIOContext') | Linux AIO 内部上下文，用于追踪 IO 控制块及读取缓冲区 |
| [LinuxAIOContextWithCallBack](Solamirare.LinuxAIOContextWithCallBack.md 'Solamirare\.LinuxAIOContextWithCallBack') | 回调上下文 |
| [LinuxCoroutine](Solamirare.LinuxCoroutine.md 'Solamirare\.LinuxCoroutine') | Linux 平台协程，基于 POSIX ucontext 实现。   所有实例通过 [Solamirare\.LinuxCoroutine\.Create\(Solamirare\.LinuxCoroutineScheduler\*,,System\.Void\*\)](https://learn.microsoft.com/en-us/dotnet/api/solamirare.linuxcoroutine.create#solamirare-linuxcoroutine-create(solamirare-linuxcoroutinescheduler*--system-void*) 'Solamirare\.LinuxCoroutine\.Create\(Solamirare\.LinuxCoroutineScheduler\*,,System\.Void\*\)') 创建，通过 [Destroy\(LinuxCoroutine\*\)](Solamirare.LinuxCoroutine.Destroy(Solamirare.LinuxCoroutine_).md 'Solamirare\.LinuxCoroutine\.Destroy\(Solamirare\.LinuxCoroutine\*\)') 释放。 实例本身分配在非托管堆，不受 GC 管理。  约束：   同一个协程的 Create / Resume / Destroy 必须在同一个线程上执行。   不能在协程内部调用 Resume，只能调用 Yield。   协程入口函数必须标注 [UnmanagedCallersOnly]。 |
| [LinuxCoroutineContext](Solamirare.LinuxCoroutineContext.md 'Solamirare\.LinuxCoroutineContext') | Linux x86\-64 协程上下文，封装 ucontext\_t 结构体。   使用固定大小的 Raw 数组覆盖整个 ucontext_t， 通过偏移量访问 uc_stack 和 uc_link 字段。 Linux x86-64 上 ucontext_t 约 936 字节，多预留空间确保安全。 |
| [LinuxCoroutineScheduler](Solamirare.LinuxCoroutineScheduler.md 'Solamirare\.LinuxCoroutineScheduler') | Linux 平台协程调度器，基于 POSIX `ucontext` 实现。 调度器保存调用方上下文，[Resume\(LinuxCoroutine\*\)](Solamirare.LinuxCoroutineScheduler.Resume(Solamirare.LinuxCoroutine_).md 'Solamirare\.LinuxCoroutineScheduler\.Resume\(Solamirare\.LinuxCoroutine\*\)') 时切入协程，[Yield\(\)](Solamirare.LinuxCoroutineScheduler.Yield().md 'Solamirare\.LinuxCoroutineScheduler\.Yield\(\)') 时切回调用方。 |
| [LinuxCoroutineTrampolineArgs](Solamirare.LinuxCoroutineTrampolineArgs.md 'Solamirare\.LinuxCoroutineTrampolineArgs') | makecontext 参数传递用的 trampoline 结构体。   Linux makecontext 只支持传递 int 类型参数，无法直接传递 64 位指针。 将所有需要传递的指针打包进此结构体， makecontext 只传此结构体的指针（拆成两个 int）， trampoline 函数内部重组指针后调用真正的入口函数。 |
| [linux\_aiocb](Solamirare.linux_aiocb.md 'Solamirare\.linux\_aiocb') | 表示 Linux 异步 I/O 控制块 \(struct aiocb\)。 用于 `aio_read`、`aio_write` 等 POSIX AIO 函数。 |
| [MacOSAIOContext](Solamirare.MacOSAIOContext.md 'Solamirare\.MacOSAIOContext') | MacOS AIO 内部上下文，用于追踪 IO 控制块及读取缓冲区 |
| [MacOSAIOContextWithCallBack](Solamirare.MacOSAIOContextWithCallBack.md 'Solamirare\.MacOSAIOContextWithCallBack') | Holds the state for a macOS asynchronous file I/O callback request\. |
| [MacOSCoroutine](Solamirare.MacOSCoroutine.md 'Solamirare\.MacOSCoroutine') | Represents a macOS coroutine backed by a manually managed stack slot\. |
| [MacOSCoroutineContext](Solamirare.MacOSCoroutineContext.md 'Solamirare\.MacOSCoroutineContext') | 协程的寄存器快照，保存上下文切换时的 CPU 状态。   布局由汇编文件 coroutine_switch_arm64.S 严格对应，字段顺序和偏移量不可随意修改。 跨平台扩展时，不同平台的 CoroutineContext 可能布局不同， 但对上层代码透明，上层只持有指针，不直接访问内部字段。 |
| [MacOSCoroutineScheduler](Solamirare.MacOSCoroutineScheduler.md 'Solamirare\.MacOSCoroutineScheduler') | macOS 平台协程调度器，负责在调用方与协程上下文之间切换执行权。 |
| [MacOSHttpPosixApi\.hostent](Solamirare.MacOSHttpPosixApi.hostent.md 'Solamirare\.MacOSHttpPosixApi\.hostent') | 表示主机解析结果结构。 |
| [MacOSHttpPosixApi\.sockaddr\_in](Solamirare.MacOSHttpPosixApi.sockaddr_in.md 'Solamirare\.MacOSHttpPosixApi\.sockaddr\_in') | 表示 IPv4 套接字地址结构。 |
| [MacOSHttpPosixApi\.socklen\_t](Solamirare.MacOSHttpPosixApi.socklen_t.md 'Solamirare\.MacOSHttpPosixApi\.socklen\_t') | Socket 长度类型。 |
| [MacOSHttpPosixApi\.timeval](Solamirare.MacOSHttpPosixApi.timeval.md 'Solamirare\.MacOSHttpPosixApi\.timeval') | 表示 POSIX 时间间隔结构。 |
| [MacOSHttpServer](Solamirare.MacOSHttpServer.md 'Solamirare\.MacOSHttpServer') | 基于 macOS kqueue 机制的零 GC 单线程 HTTP 服务器。 所有 I/O 均在单一事件循环线程内完成，无锁、无线程切换开销。   A zero-GC single-threaded HTTP server based on the macOS kqueue mechanism. All I/O is performed within a single event loop thread, with no locks or thread-switching overhead. |
| [macos\_aiocb](Solamirare.macos_aiocb.md 'Solamirare\.macos\_aiocb') | 表示 macOS/Darwin 系统中的异步 I/O 控制块 \(struct aiocb\)。 用于 `aio_read`、`aio_write` 等 POSIX AIO 系统调用。 |
| [MemoryObjectPool&lt;T&gt;](Solamirare.MemoryObjectPool_T_.md 'Solamirare\.MemoryObjectPool\<T\>') | 内存对象池单元   用于管理特定非托管类型对象的内存分配与回收，支持线程安全操作。 |
| [MemoryPollAllocatedResult](Solamirare.MemoryPollAllocatedResult.md 'Solamirare\.MemoryPollAllocatedResult') | 内存分配结果 |
| [MemoryPoolFrozenNode](Solamirare.MemoryPoolFrozenNode.md 'Solamirare\.MemoryPoolFrozenNode') | 单个内存池节点，管理一批等长内存块的分配与回收。 内部以空闲索引栈（int\[\]）跟踪可用块，以位图（ulong\[\]）记录分配状态。   线程安全：Alloc / Free 均在 SpinLock 保护的临界区内完成， 消除了"原子计数器 + 非原子数组访问"模式下计数器更新与数组读写之间的竞态窗口。 |
| [MemoryPoolSchema](Solamirare.MemoryPoolSchema.md 'Solamirare\.MemoryPoolSchema') | 描述一个子内存池的规格：每个块的字节长度与块的总数量。   注意：该结构体的大小固定为 8 字节，参与内存布局计算，不可更改。 |
| [NativeTask](Solamirare.NativeTask.md 'Solamirare\.NativeTask') | |
| [NativeTaskNode](Solamirare.NativeTaskNode.md 'Solamirare\.NativeTaskNode') | |
| [NodeStackFrame](Solamirare.NodeStackFrame.md 'Solamirare\.NodeStackFrame') | 表示 JSON 解析过程中用于追踪父子关系的栈帧。 |
| [OVERLAPPED](Solamirare.OVERLAPPED.md 'Solamirare\.OVERLAPPED') | 表示 Windows 异步 I/O 操作的核心结构 \(OVERLAPPED\)。 用于在重叠 I/O 操作（如 `ReadFile`、`WriteFile`）及完成端口 \(IOCP\) 中同步数据和状态。 |
| [PollFd](Solamirare.PollFd.md 'Solamirare\.PollFd') | 用于 poll 系统调用的文件描述符结构。 <br/> Structure for file descriptors used in the poll system call\. |
| [Replace\_Result](Solamirare.Replace_Result.md 'Solamirare\.Replace\_Result') | 替换操作结果描述 |
| [SerializeResult](Solamirare.SerializeResult.md 'Solamirare\.SerializeResult') | 序列化执行结果 |
| [sockaddr\_bsd](Solamirare.sockaddr_bsd.md 'Solamirare\.sockaddr\_bsd') | 表示 IPv4 地址信息的结构体，用于 bind/accept/getpeername 等调用。   与 macOS 的 struct sockaddr_in 内存布局一致。（ Linux 与 Windows 因为内存布局不一致，必须使用 sockaddr_in ） |
| [sockaddr\_in](Solamirare.sockaddr_in.md 'Solamirare\.sockaddr\_in') | 表示 IPv4 地址信息的结构体，用于 bind/accept/getpeername 等调用。   Linux 与 Windows 共用的 sockaddr_in。（ macOS应该结构不一致，必须使用 sockaddr_bsd ） |
| [StackSegment&lt;T&gt;](Solamirare.StackSegment_T_.md 'Solamirare\.StackSegment\<T\>') | 非托管栈分段，用于存储每个外部或内部内存块的元数据 |
| [Stat](Solamirare.Stat.md 'Solamirare\.Stat') | fstat 函数的文件元数据 |
| [ThreadPoolState](Solamirare.ThreadPoolState.md 'Solamirare\.ThreadPoolState') | |
| [ThreadStartInfo](Solamirare.ThreadStartInfo.md 'Solamirare\.ThreadStartInfo') | 用于在 POSIX 平台上打包 worker 函数指针和参数的上下文结构体。   必须由调用方在栈上（或固定内存中）分配，并保证其生命周期覆盖线程启动完成前的窗口期。 推荐做法：Create 返回后立即调用 Join，或使用信号量确认线程已读取完毕再释放。 |
| [Timespec](Solamirare.Timespec.md 'Solamirare\.Timespec') | Represents a POSIX timespec value\. |
| [timeval](Solamirare.timeval.md 'Solamirare\.timeval') | 表示一个时间间隔，用于指定超时时间（例如在 `kevent` 调用中）。 |
| [UHttpConnection](Solamirare.UHttpConnection.md 'Solamirare\.UHttpConnection') | Connection对象 |
| [UHttpContext](Solamirare.UHttpContext.md 'Solamirare\.UHttpContext') | 表示一次 HTTP 请求处理过程中使用的上下文。 |
| [UHttpRequest](Solamirare.UHttpRequest.md 'Solamirare\.UHttpRequest') | 表示一个 HTTP 请求的解析结果。 所有字段均为零拷贝切片，直接引用调用方传入的原始读缓冲区，不产生任何字节复制或托管堆分配。 作为 ref struct，其生命周期严格绑定在栈帧内，不可装箱或存储到堆上。   Represents the parsed result of an HTTP request. All fields are zero-copy slices that reference the raw read buffer supplied by the caller — no byte copying or managed heap allocation occurs. As a ref struct, its lifetime is strictly bound to the enclosing stack frame; it cannot be boxed or stored on the heap. |
| [UHttpResponse](Solamirare.UHttpResponse.md 'Solamirare\.UHttpResponse') | HttpResponse |
| [UnManagedCollection&lt;T&gt;](Solamirare.UnManagedCollection_T_.md 'Solamirare\.UnManagedCollection\<T\>') | 内存段的基本描述单位   The basic description unit of a memory segment. |
| [UnManagedCollection&lt;T&gt;\.Enumerator](Solamirare.UnManagedCollection_T_.Enumerator.md 'Solamirare\.UnManagedCollection\<T\>\.Enumerator') | 迭代器   Enumerator. |
| [UnmanagedKeyCollection&lt;TKey,TValue&gt;](Solamirare.UnmanagedKeyCollection_TKey,TValue_.md 'Solamirare\.UnmanagedKeyCollection\<TKey,TValue\>') | Represents a key\-only collection view over unmanaged dictionary storage\. |
| [UnmanagedKeyEnumerator&lt;TKey,TValue&gt;](Solamirare.UnmanagedKeyEnumerator_TKey,TValue_.md 'Solamirare\.UnmanagedKeyEnumerator\<TKey,TValue\>') | Enumerates keys from unmanaged dictionary storage\. |
| [UnManagedMemory&lt;T&gt;](Solamirare.UnManagedMemory_T_.md 'Solamirare\.UnManagedMemory\<T\>') | 非托管内存集合。   支持三种内存模式：  1. 内部分配的堆内存模式。  2. 链接到外部的栈内存模式。  3. 链接到外部的堆内存模式。  注意：内部分配始终使用堆内存；栈内存模式始终链接到外部内存。 |
| [UnmanagedValueCollection&lt;TKey,TValue&gt;](Solamirare.UnmanagedValueCollection_TKey,TValue_.md 'Solamirare\.UnmanagedValueCollection\<TKey,TValue\>') | Represents a value\-only collection view over unmanaged dictionary storage\. |
| [UnmanagedValueEnumerator&lt;TKey,TValue&gt;](Solamirare.UnmanagedValueEnumerator_TKey,TValue_.md 'Solamirare\.UnmanagedValueEnumerator\<TKey,TValue\>') | Enumerates values from unmanaged dictionary storage\. |
| [UrlParts](Solamirare.UrlParts.md 'Solamirare\.UrlParts') | 表示 URL 解析后的关键组成部分。 |
| [ValueDictionary&lt;TKey,TValue&gt;](Solamirare.ValueDictionary_TKey,TValue_.md 'Solamirare\.ValueDictionary\<TKey,TValue\>') | 非托管字典   Unmanaged Dictionary |
| [ValueDictionary&lt;TKey,TValue&gt;\.Enumerator](Solamirare.ValueDictionary_TKey,TValue_.Enumerator.md 'Solamirare\.ValueDictionary\<TKey,TValue\>\.Enumerator') | Enumerates occupied slots in a [ValueDictionary&lt;TKey,TValue&gt;](Solamirare.ValueDictionary_TKey,TValue_.md 'Solamirare\.ValueDictionary\<TKey,TValue\>')\. |
| [ValueDictionary&lt;TKey,TValue&gt;\.KeyCollection](Solamirare.ValueDictionary_TKey,TValue_.KeyCollection.md 'Solamirare\.ValueDictionary\<TKey,TValue\>\.KeyCollection') | Represents a key\-only collection view for the dictionary\. |
| [ValueDictionary&lt;TKey,TValue&gt;\.KeyEnumerator](Solamirare.ValueDictionary_TKey,TValue_.KeyEnumerator.md 'Solamirare\.ValueDictionary\<TKey,TValue\>\.KeyEnumerator') | Enumerates keys in the dictionary\. |
| [ValueDictionary&lt;TKey,TValue&gt;\.ValueCollection](Solamirare.ValueDictionary_TKey,TValue_.ValueCollection.md 'Solamirare\.ValueDictionary\<TKey,TValue\>\.ValueCollection') | Represents a value\-only collection view for the dictionary\. |
| [ValueDictionary&lt;TKey,TValue&gt;\.ValueEnumerator](Solamirare.ValueDictionary_TKey,TValue_.ValueEnumerator.md 'Solamirare\.ValueDictionary\<TKey,TValue\>\.ValueEnumerator') | Enumerates values in the dictionary\. |
| [ValueFrozenStack&lt;T&gt;](Solamirare.ValueFrozenStack_T_.md 'Solamirare\.ValueFrozenStack\<T\>') | 值类型固定栈结构，长度不可变 |
| [ValueFrozenStack&lt;T&gt;\.Enumerator](Solamirare.ValueFrozenStack_T_.Enumerator.md 'Solamirare\.ValueFrozenStack\<T\>\.Enumerator') | 迭代器 |
| [ValueHttpClient](Solamirare.ValueHttpClient.md 'Solamirare\.ValueHttpClient') | 跨平台 HTTP/HTTPS 客户端（无托管内存分配）   Unified Cross-Platform HTTP Client (Zero Allocation) |
| [ValueHttpResponse](Solamirare.ValueHttpResponse.md 'Solamirare\.ValueHttpResponse') | 表示一次 HTTP 请求返回的响应数据。 |
| [ValueLinkedList&lt;T&gt;](Solamirare.ValueLinkedList_T_.md 'Solamirare\.ValueLinkedList\<T\>') | 值类型链表 |
| [ValueLinkedList&lt;T&gt;\.Enumerator](Solamirare.ValueLinkedList_T_.Enumerator.md 'Solamirare\.ValueLinkedList\<T\>\.Enumerator') | 迭代器 |
| [ValueLiskedListNode&lt;T&gt;](Solamirare.ValueLiskedListNode_T_.md 'Solamirare\.ValueLiskedListNode\<T\>') | 值类型链表节点 |
| [ValueStack&lt;T&gt;](Solamirare.ValueStack_T_.md 'Solamirare\.ValueStack\<T\>') | 可以扩展容量的栈 \(分段存储，支持外部内存增量扩容\) |
| [ValueStack&lt;T&gt;\.Enumerator](Solamirare.ValueStack_T_.Enumerator.md 'Solamirare\.ValueStack\<T\>\.Enumerator') | 迭代器 |
| [WindowsCoroutine](Solamirare.WindowsCoroutine.md 'Solamirare\.WindowsCoroutine') | Represents a Windows fiber\-backed coroutine instance\. |
| [WindowsCoroutineContext](Solamirare.WindowsCoroutineContext.md 'Solamirare\.WindowsCoroutineContext') | Windows 协程上下文，封装 Win32 Fiber 句柄。   macOS 版本需要手动保存寄存器快照，Windows 版本由 Win32 内部管理， 此处只需保存 Fiber 句柄即可完成所有切换操作。 |
| [WindowsCoroutineScheduler](Solamirare.WindowsCoroutineScheduler.md 'Solamirare\.WindowsCoroutineScheduler') | Windows 平台协程调度器，基于 Win32 Fiber API 实现。 |
| [WindowsHttpApi\.timeval](Solamirare.WindowsHttpApi.timeval.md 'Solamirare\.WindowsHttpApi\.timeval') | 表示时间间隔结构。 |
| [WindowsHttpApi\.WSAData](Solamirare.WindowsHttpApi.WSAData.md 'Solamirare\.WindowsHttpApi\.WSAData') | 表示 WinSock 初始化信息。 |
| [WSABUF](Solamirare.WSABUF.md 'Solamirare\.WSABUF') | 表示 Windows 套接字缓冲区 \(WSABUF\) 的内存布局。 在 Linux/macOS 环境下，其二进制结构通常与 `iovec` 兼容。 |

| Interfaces | |
| :--- | :--- |
| [ISyncDirectory](Solamirare.ISyncDirectory.md 'Solamirare\.ISyncDirectory') | 跨平台文件夹操作 |
| [ISyncFilesIO](Solamirare.ISyncFilesIO.md 'Solamirare\.ISyncFilesIO') | 跨平台IO操作 |

| Enums | |
| :--- | :--- |
| [AsyncHttpClientRequestState](Solamirare.AsyncHttpClientRequestState.md 'Solamirare\.AsyncHttpClientRequestState') | 异步HttpClient请求状态，由 IO\_URing、KQueue 和 IOCP 共用，某些状态仅适用于特定平台，但为了简化设计，统一使用一个枚举来表示请求的不同阶段。 |
| [ConnectionMethod](Solamirare.ConnectionMethod.md 'Solamirare\.ConnectionMethod') | 表示 HTTP 请求方法。 |
| [ConnectionProtocol](Solamirare.ConnectionProtocol.md 'Solamirare\.ConnectionProtocol') | 表示连接或应用层协议类型。 |
| [HttpContentType](Solamirare.HttpContentType.md 'Solamirare\.HttpContentType') | 表示 HTTP 请求体的内容类型。 |
| [HttpMethod](Solamirare.HttpMethod.md 'Solamirare\.HttpMethod') | 表示 HTTP 请求方法。 |
| [HttpMimeTypes](Solamirare.HttpMimeTypes.md 'Solamirare\.HttpMimeTypes') | 常用 MIME 类型枚举 |
| [HttpVersion](Solamirare.HttpVersion.md 'Solamirare\.HttpVersion') | Http协议版本 |
| [IOBackend](Solamirare.IOBackend.md 'Solamirare\.IOBackend') | Identifies the active native asynchronous I/O backend\. |
| [JsonSerializeTypes](Solamirare.JsonSerializeTypes.md 'Solamirare\.JsonSerializeTypes') | 序列化类型 |
| [LinuxFilesIO](Solamirare.LinuxFilesIO.md 'Solamirare\.LinuxFilesIO') | Selects which Linux file I/O backend should be preferred\. |
| [MemoryScaleMode](Solamirare.MemoryScaleMode.md 'Solamirare\.MemoryScaleMode') | 内存段扩容模式 |
| [MemoryType](Solamirare.MemoryType.md 'Solamirare\.MemoryType') | 标识当前指针指向的内存是属于堆分配或栈分配 |
| [MemoryTypeDefined](Solamirare.MemoryTypeDefined.md 'Solamirare\.MemoryTypeDefined') | 手动标识内存类型，属于栈或堆 |
| [OpenFlags\_Linux](Solamirare.OpenFlags_Linux.md 'Solamirare\.OpenFlags\_Linux') | Linux 上的 open 函数标志 |
| [OpenFlags\_MacOS](Solamirare.OpenFlags_MacOS.md 'Solamirare\.OpenFlags\_MacOS') | MacOS 上的 open 函数标志 |
| [SerializeResultEnum](Solamirare.SerializeResultEnum.md 'Solamirare\.SerializeResultEnum') | 序列化执行结果状态 |
