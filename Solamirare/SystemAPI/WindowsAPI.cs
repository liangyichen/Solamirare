namespace Solamirare;

internal unsafe partial class WindowsAPI
{
    // ────────────────────────────────────────────────────────────────────────────
    //  文件操作
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 创建或打开一个文件、设备、管道等对象，返回其句柄。
    /// <para><c>dwDesiredAccess</c>：GENERIC_READ=0x80000000，GENERIC_WRITE=0x40000000。</para>
    /// <para><c>dwCreationDisposition</c>：CREATE_NEW=1，CREATE_ALWAYS=2，OPEN_EXISTING=3，OPEN_ALWAYS=4，TRUNCATE_EXISTING=5。</para>
    /// <para><c>dwFlagsAndAttributes</c>：FILE_FLAG_OVERLAPPED=0x40000000 表示启用异步 I/O，FILE_ATTRIBUTE_NORMAL=0x80 表示普通文件。</para>
    /// </summary>
    /// <param name="lpFileName">文件路径（UTF-16 字符串）。</param>
    /// <param name="dwDesiredAccess">访问权限标志。</param>
    /// <param name="dwShareMode">共享模式，0 表示独占。</param>
    /// <param name="lpSecurityAttributes">安全属性，传 null 使用默认值。</param>
    /// <param name="dwCreationDisposition">文件创建或打开方式。</param>
    /// <param name="dwFlagsAndAttributes">文件属性与标志位。</param>
    /// <param name="hTemplateFile">模板文件句柄，通常传 null。</param>
    /// <returns>成功返回文件句柄，失败返回 INVALID_HANDLE_VALUE（即 (void*)-1）。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static unsafe partial void* CreateFileW(
        char* lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        void* lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        void* hTemplateFile);

    /// <summary>
    /// 从文件句柄异步读取数据（OVERLAPPED 版本）。
    /// <para>传入非 null 的 <c>lpOverlapped</c> 时为异步模式，函数可能立即返回 false 并设置错误码 ERROR_IO_PENDING(997)，
    /// 此为正常现象，需通过 <see cref="GetOverlappedResult"/> 或 IOCP 等待实际完成。</para>
    /// </summary>
    /// <param name="hFile">由 <see cref="CreateFileW"/> 返回的文件句柄。</param>
    /// <param name="lpBuffer">接收数据的缓冲区指针。</param>
    /// <param name="nNumberOfBytesToRead">期望读取的字节数。</param>
    /// <param name="lpNumberOfBytesRead">实际读取的字节数（异步模式下可为 null）。</param>
    /// <param name="lpOverlapped">重叠结构指针，同步模式传 null，异步模式传已初始化的结构体。</param>
    /// <returns>同步成功返回 true；异步模式下操作挂起时返回 false，错误码为 997（ERROR_IO_PENDING）。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool ReadFile(void* hFile, byte* lpBuffer, uint nNumberOfBytesToRead, uint* lpNumberOfBytesRead, OVERLAPPED* lpOverlapped);

    /// <summary>
    /// 从文件句柄同步读取数据（IntPtr 版本，不支持异步 OVERLAPPED）。
    /// </summary>
    /// <param name="hFile">文件句柄（以 char* 传入）。</param>
    /// <param name="lpBuffer">接收数据的缓冲区指针。</param>
    /// <param name="nNumberOfBytesToRead">期望读取的字节数。</param>
    /// <param name="lpNumberOfBytesRead">实际读取的字节数。</param>
    /// <param name="lpOverlapped">重叠结构，同步模式传 IntPtr.Zero。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool ReadFile(char* hFile, byte* lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

    /// <summary>
    /// 向文件句柄异步写入数据（OVERLAPPED 版本）。
    /// <para>传入非 null 的 <c>lpOverlapped</c> 时为异步模式，函数可能立即返回 false 并设置错误码 ERROR_IO_PENDING(997)，
    /// 此为正常现象，需通过 <see cref="GetOverlappedResult"/> 或 IOCP 等待实际完成。</para>
    /// </summary>
    /// <param name="hFile">由 <see cref="CreateFileW"/> 返回的文件句柄。</param>
    /// <param name="lpBuffer">要写入数据的缓冲区指针。</param>
    /// <param name="nNumberOfBytesToWrite">要写入的字节数。</param>
    /// <param name="lpNumberOfBytesWritten">实际写入的字节数（异步模式下可为 null）。</param>
    /// <param name="lpOverlapped">重叠结构指针，同步模式传 null，异步模式传已初始化的结构体。</param>
    /// <returns>同步成功返回 true；异步模式下操作挂起时返回 false，错误码为 997（ERROR_IO_PENDING）。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool WriteFile(void* hFile, byte* lpBuffer, uint nNumberOfBytesToWrite, uint* lpNumberOfBytesWritten, OVERLAPPED* lpOverlapped);

    /// <summary>
    /// 向文件句柄同步写入数据（void* OVERLAPPED 版本）。
    /// </summary>
    /// <param name="hFile">文件句柄（以 char* 传入）。</param>
    /// <param name="lpBuffer">要写入数据的缓冲区指针。</param>
    /// <param name="nNumberOfBytesToWrite">要写入的字节数。</param>
    /// <param name="lpNumberOfBytesWritten">实际写入的字节数。</param>
    /// <param name="lpOverlapped">重叠结构，同步模式传 null。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool WriteFile(
        char* hFile,
        byte* lpBuffer,
        uint nNumberOfBytesToWrite,
        out uint lpNumberOfBytesWritten,
        void* lpOverlapped);

    /// <summary>
    /// 获取异步 I/O 操作的结果，阻塞或轮询等待 OVERLAPPED 操作完成。
    /// </summary>
    /// <param name="hFile">发起异步操作时使用的文件句柄。</param>
    /// <param name="lpOverlapped">发起操作时传入的 OVERLAPPED 结构体指针。</param>
    /// <param name="lpNumberOfBytesTransferred">实际传输的字节数。</param>
    /// <param name="bWait">true 表示阻塞等待完成；false 表示立即返回（未完成则返回 false）。</param>
    /// <returns>操作成功完成返回 true，否则返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool GetOverlappedResult(void* hFile, OVERLAPPED* lpOverlapped, uint* lpNumberOfBytesTransferred, [MarshalAs(UnmanagedType.Bool)] bool bWait);

    /// <summary>
    /// 获取文件大小（OVERLAPPED 指针版本）。
    /// </summary>
    /// <param name="hFile">文件句柄。</param>
    /// <param name="lpFileSize">接收文件大小的 64 位整数指针。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool GetFileSizeEx(void* hFile, long* lpFileSize);

    /// <summary>
    /// 获取文件大小（out 参数版本）。
    /// </summary>
    /// <param name="hFile">文件句柄。</param>
    /// <param name="lpFileSize">接收文件大小的 64 位整数。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool GetFileSizeEx(void* hFile, out long lpFileSize);

    /// <summary>
    /// 将文件的内核缓冲区强制刷新写入物理磁盘，确保数据持久化。
    /// </summary>
    /// <param name="hFile">文件句柄（以 char* 传入）。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool FlushFileBuffers(char* hFile);

    /// <summary>
    /// 删除指定路径的文件（IntPtr 版本）。
    /// </summary>
    /// <param name="lpFileName">要删除的文件路径。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool DeleteFileW(IntPtr lpFileName);

    /// <summary>
    /// 删除指定路径的文件（char* 版本）。
    /// </summary>
    /// <param name="lpFileName">要删除的文件路径（UTF-16 字符串）。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool DeleteFileW(char* lpFileName);

    // ────────────────────────────────────────────────────────────────────────────
    //  目录操作
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 开始枚举目录中的文件，返回第一个匹配项的句柄。
    /// <para><c>lpFileName</c> 支持通配符，如 <c>C:\folder\*</c>。</para>
    /// </summary>
    /// <param name="lpFileName">搜索路径，可包含通配符 * 或 ?。</param>
    /// <param name="lpFindFileData">接收第一个匹配文件信息的 WIN32_FIND_DATA 结构体。</param>
    /// <returns>成功返回搜索句柄，失败返回 INVALID_HANDLE_VALUE。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static unsafe partial IntPtr FindFirstFileW(char* lpFileName, out ZeroDirectory_Windows.WIN32_FIND_DATA lpFindFileData);

    /// <summary>
    /// 继续枚举目录中的下一个文件，需配合 <see cref="FindFirstFileW"/> 使用。
    /// </summary>
    /// <param name="hFindFile">由 <see cref="FindFirstFileW"/> 返回的搜索句柄。</param>
    /// <param name="lpFindFileData">接收下一个匹配文件信息的 WIN32_FIND_DATA 结构体。</param>
    /// <returns>找到下一个文件返回 true，枚举结束返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool FindNextFileW(IntPtr hFindFile, out ZeroDirectory_Windows.WIN32_FIND_DATA lpFindFileData);

    /// <summary>
    /// 关闭由 <see cref="FindFirstFileW"/> 打开的文件搜索句柄，释放相关资源。
    /// </summary>
    /// <param name="hFindFile">要关闭的搜索句柄。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool FindClose(IntPtr hFindFile);

    /// <summary>
    /// 获取指定路径的文件属性。
    /// <para>返回值为 INVALID_FILE_ATTRIBUTES（0xFFFFFFFF）时表示路径不存在或出错。</para>
    /// <para>常用属性位：FILE_ATTRIBUTE_DIRECTORY=0x10，FILE_ATTRIBUTE_READONLY=0x01。</para>
    /// </summary>
    /// <param name="lpFileName">文件或目录路径。</param>
    /// <returns>文件属性标志位，失败时返回 0xFFFFFFFF。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static unsafe partial uint GetFileAttributesW(char* lpFileName);

    /// <summary>
    /// 创建一个新目录。
    /// </summary>
    /// <param name="lpPathName">要创建的目录路径。</param>
    /// <param name="lpSecurityAttributes">安全属性，传 IntPtr.Zero 使用默认值。</param>
    /// <returns>成功返回 true，失败返回 false（目录已存在时也返回 false）。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool CreateDirectoryW(char* lpPathName, IntPtr lpSecurityAttributes);

    /// <summary>
    /// 获取当前工作目录的绝对路径。
    /// </summary>
    /// <param name="nBufferLength">缓冲区容量（字符数，含终止符）。</param>
    /// <param name="lpBuffer">接收路径字符串的缓冲区。</param>
    /// <returns>成功返回写入的字符数（不含终止符），失败返回 0。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static unsafe partial uint GetCurrentDirectoryW(uint nBufferLength, char* lpBuffer);

    // ────────────────────────────────────────────────────────────────────────────
    //  句柄管理
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 关闭一个打开的内核对象句柄，释放相关资源（void* 版本）。
    /// <para>适用于文件、线程、事件、信号量等所有内核对象句柄。</para>
    /// </summary>
    /// <param name="hObject">要关闭的对象句柄。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(void* hObject);

    /// <summary>
    /// 关闭一个打开的内核对象句柄（char* 版本）。
    /// </summary>
    /// <param name="hObject">要关闭的对象句柄（以 char* 传入）。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool CloseHandle(char* hObject);

    /// <summary>
    /// 关闭一个打开的内核对象句柄（IntPtr 版本）。
    /// </summary>
    /// <param name="hObject">要关闭的对象句柄。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(IntPtr hObject);

    // ────────────────────────────────────────────────────────────────────────────
    //  I/O 完成端口（IOCP）
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 创建一个新的 I/O 完成端口，或将文件句柄与现有完成端口关联（void* 版本）。
    /// <para>传入 INVALID_HANDLE_VALUE 并将 ExistingCompletionPort 设为 null 时，创建新的 IOCP。</para>
    /// <para>传入有效文件句柄时，将该句柄与指定 IOCP 关联，后续该文件的异步 I/O 完成通知将投递到此端口。</para>
    /// </summary>
    /// <param name="FileHandle">要关联的文件句柄，创建新端口时传 INVALID_HANDLE_VALUE。</param>
    /// <param name="ExistingCompletionPort">现有 IOCP 句柄，创建新端口时传 null。</param>
    /// <param name="CompletionKey">与文件句柄关联的完成键，用于在出队时识别来源。</param>
    /// <param name="NumberOfConcurrentThreads">允许并发处理完成包的线程数，0 表示与 CPU 核心数相同。</param>
    /// <returns>成功返回 IOCP 句柄，失败返回 null。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static unsafe partial void* CreateIoCompletionPort(void* FileHandle, void* ExistingCompletionPort, nuint CompletionKey, uint NumberOfConcurrentThreads);

    /// <summary>
    /// 创建一个新的 I/O 完成端口，或将文件句柄与现有完成端口关联（nint 版本）。
    /// <para>传入 INVALID_HANDLE_VALUE 并将 ExistingCompletionPort 设为 0 时，创建新的 IOCP。</para>
    /// </summary>
    /// <param name="FileHandle">要关联的文件句柄。</param>
    /// <param name="ExistingCompletionPort">现有 IOCP 句柄，创建新端口时传 0。</param>
    /// <param name="CompletionKey">完成键，用于在出队时识别来源。</param>
    /// <param name="NumberOfConcurrentThreads">允许并发处理完成包的线程数。</param>
    /// <returns>成功返回 IOCP 句柄，失败返回 0。</returns>
    [LibraryImport("kernel32.dll")]
    public static unsafe partial nint CreateIoCompletionPort(
        nint FileHandle,
        nint ExistingCompletionPort,
        nuint CompletionKey,
        uint NumberOfConcurrentThreads);

    /// <summary>
    /// 从 IOCP 出队一个已完成的 I/O 操作（void* 版本）。
    /// <para>调用线程将阻塞，直到有完成通知到来或超时。</para>
    /// </summary>
    /// <param name="CompletionPort">IOCP 句柄。</param>
    /// <param name="lpNumberOfBytesTransferred">本次 I/O 实际传输的字节数。</param>
    /// <param name="lpCompletionKey">关联到该操作文件句柄的完成键。</param>
    /// <param name="lpOverlapped">指向触发本次完成通知的 OVERLAPPED 结构体的指针。</param>
    /// <param name="dwMilliseconds">等待超时毫秒数，INFINITE=0xFFFFFFFF 表示永久等待。</param>
    /// <returns>成功出队并完成返回 true，超时或出错返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool GetQueuedCompletionStatus(void* CompletionPort, uint* lpNumberOfBytesTransferred, nuint* lpCompletionKey, OVERLAPPED** lpOverlapped, uint dwMilliseconds);

    /// <summary>
    /// 从 IOCP 出队一个已完成的 I/O 操作（nint 版本）。
    /// <para>调用线程将阻塞，直到有完成通知到来或超时。</para>
    /// </summary>
    /// <param name="CompletionPort">IOCP 句柄。</param>
    /// <param name="lpNumberOfBytesTransferred">本次 I/O 实际传输的字节数。</param>
    /// <param name="lpCompletionKey">关联到该操作文件句柄的完成键。</param>
    /// <param name="lpOverlapped">指向触发本次完成通知的 OVERLAPPED 结构体的指针。</param>
    /// <param name="dwMilliseconds">等待超时毫秒数。</param>
    /// <returns>成功出队并完成返回 true，超时或出错返回 false。</returns>
    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool GetQueuedCompletionStatus(
        nint CompletionPort,
        uint* lpNumberOfBytesTransferred,
        nuint* lpCompletionKey,
        OVERLAPPED** lpOverlapped,
        uint dwMilliseconds);

    /// <summary>
    /// 向 IOCP 手动投递一个完成包（void* 版本）。
    /// <para>常用于向工作线程发送自定义信号，如退出哨兵（传入特殊的 CompletionKey 或 null OVERLAPPED）。</para>
    /// </summary>
    /// <param name="CompletionPort">IOCP 句柄。</param>
    /// <param name="dwNumberOfBytesTransferred">要报告的传输字节数，自定义信号通常传 0。</param>
    /// <param name="CompletionKey">自定义完成键。</param>
    /// <param name="lpOverlapped">OVERLAPPED 结构体指针，自定义信号通常传 null。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool PostQueuedCompletionStatus(void* CompletionPort, uint dwNumberOfBytesTransferred, nuint CompletionKey, OVERLAPPED* lpOverlapped);

    /// <summary>
    /// 向 IOCP 手动投递一个完成包（nint 版本）。
    /// </summary>
    /// <param name="CompletionPort">IOCP 句柄。</param>
    /// <param name="dwNumberOfBytesTransferred">要报告的传输字节数。</param>
    /// <param name="dwCompletionKey">自定义完成键。</param>
    /// <param name="lpOverlapped">OVERLAPPED 结构体指针。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool PostQueuedCompletionStatus(
        nint CompletionPort,
        uint dwNumberOfBytesTransferred,
        nuint dwCompletionKey,
        OVERLAPPED* lpOverlapped);

    // ────────────────────────────────────────────────────────────────────────────
    //  事件对象
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 创建或打开一个命名或匿名事件对象（void* 版本）。
    /// <para>事件对象用于线程间同步：手动重置事件需显式调用 ResetEvent；自动重置事件在一个等待线程被释放后自动变为无信号状态。</para>
    /// </summary>
    /// <param name="lpEventAttributes">安全属性，传 null 使用默认值。</param>
    /// <param name="bManualReset">true 为手动重置事件，false 为自动重置事件。</param>
    /// <param name="bInitialState">true 表示初始状态为有信号，false 为无信号。</param>
    /// <param name="lpName">事件名称（命名事件），匿名事件传 null。</param>
    /// <returns>成功返回事件句柄，失败返回 null。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static unsafe partial void* CreateEventW(void* lpEventAttributes, [MarshalAs(UnmanagedType.Bool)] bool bManualReset, [MarshalAs(UnmanagedType.Bool)] bool bInitialState, char* lpName);

    /// <summary>
    /// 创建或打开一个命名或匿名事件对象（IntPtr 版本）。
    /// </summary>
    /// <param name="lpEventAttributes">安全属性，传 IntPtr.Zero 使用默认值。</param>
    /// <param name="bManualReset">true 为手动重置事件，false 为自动重置事件。</param>
    /// <param name="bInitialState">true 表示初始状态为有信号，false 为无信号。</param>
    /// <param name="lpName">事件名称，匿名事件传 IntPtr.Zero。</param>
    /// <returns>成功返回事件句柄，失败返回 IntPtr.Zero。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial IntPtr CreateEventW(IntPtr lpEventAttributes, [MarshalAs(UnmanagedType.Bool)] bool bManualReset, [MarshalAs(UnmanagedType.Bool)] bool bInitialState, IntPtr lpName);

    /// <summary>
    /// 将事件对象设置为有信号状态，唤醒等待该事件的线程。
    /// </summary>
    /// <param name="hEvent">事件句柄。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetEvent(IntPtr hEvent);


    /// <summary>
    /// 将事件对象重置为无信号状态。
    /// 用于 ManualResetEvent 在下一次 Wait 前清除上一次的信号残留。
    /// </summary>
    /// <param name="hEvent">事件对象句柄。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", EntryPoint = "ResetEvent")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ResetEvent(nint hEvent);
    
    // ────────────────────────────────────────────────────────────────────────────
    //  线程管理
    // ────────────────────────────────────────────────────────────────────────────


    /// <summary>
    /// 将当前线程转换为 Fiber，返回代表该线程的 Fiber 句柄。
    /// 必须在调用 SwitchToFiber 之前执行，每个线程只需调用一次。
    /// </summary>
    /// <param name="lpParameter">传递给 Fiber 的参数，通常传 null。</param>
    /// <returns>成功返回当前线程对应的 Fiber 句柄，失败返回 null。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint ConvertThreadToFiber(nint lpParameter);

    /// <summary>
    /// 将当前 Fiber 还原为普通线程。
    /// 仅当线程此前由 <see cref="ConvertThreadToFiber"/> 转换而来时才能调用。
    /// </summary>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ConvertFiberToThread();

    /// <summary>
    /// 创建一个新的 Fiber。
    /// Fiber 创建后处于挂起状态，需通过 SwitchToFiber 才会开始执行。
    /// </summary>
    /// <param name="dwStackSize">栈大小（字节），传 0 使用系统默认值。</param>
    /// <param name="lpStartAddress">Fiber 入口函数指针。</param>
    /// <param name="lpParameter">传递给入口函数的参数。</param>
    /// <returns>成功返回 Fiber 句柄，失败返回 null。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint CreateFiber(
        nuint dwStackSize,
        delegate* unmanaged[Stdcall]<void*, void> lpStartAddress,
        void* lpParameter);

    /// <summary>
    /// 切换执行权到指定 Fiber。
    /// 调用后当前 Fiber 挂起，目标 Fiber 从上次暂停的位置继续执行。
    /// </summary>
    /// <param name="lpFiber">目标 Fiber 的句柄。</param>
    [LibraryImport("kernel32.dll")]
    public static partial void SwitchToFiber(nint lpFiber);

    /// <summary>
    /// 删除一个 Fiber，释放其栈内存和内核资源。
    /// 不能删除当前正在执行的 Fiber。
    /// </summary>
    /// <param name="lpFiber">要删除的 Fiber 句柄。</param>
    [LibraryImport("kernel32.dll")]
    public static partial void DeleteFiber(nint lpFiber);



    /// <summary>
    /// 创建一个新线程（void* 返回句柄版本）。
    /// </summary>
    /// <param name="lpThreadAttributes">安全属性，传 null 使用默认值。</param>
    /// <param name="dwStackSize">初始栈大小（字节），传 0 使用默认值（通常 1MB）。</param>
    /// <param name="lpStartAddress">线程入口函数指针（LPTHREAD_START_ROUTINE）。</param>
    /// <param name="lpParameter">传递给线程函数的参数指针。</param>
    /// <param name="dwCreationFlags">创建标志，0 表示立即运行，CREATE_SUSPENDED=4 表示挂起创建。</param>
    /// <param name="lpThreadId">输出参数：接收新线程的 ID。</param>
    /// <returns>成功返回新线程的句柄，失败返回 null。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial void* CreateThread(
        void* lpThreadAttributes,
        nuint dwStackSize,
        delegate* unmanaged<void*, uint> lpStartAddress,
        void* lpParameter,
        uint dwCreationFlags,
        out uint lpThreadId);

    /// <summary>
    /// 创建一个新线程（IntPtr 返回句柄版本，Stdcall 调用约定）。
    /// </summary>
    /// <param name="lpThreadAttributes">安全属性，传 null 使用默认值。</param>
    /// <param name="dwStackSize">初始栈大小，传 0 使用默认值。</param>
    /// <param name="lpStartAddress">线程入口函数指针。</param>
    /// <param name="lpParameter">传递给线程函数的参数指针。</param>
    /// <param name="dwCreationFlags">创建标志。</param>
    /// <param name="lpThreadId">接收新线程 ID 的指针。</param>
    /// <returns>成功返回新线程的句柄，失败返回 IntPtr.Zero。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial IntPtr CreateThread(
        void* lpThreadAttributes,
        nuint dwStackSize,
        delegate* unmanaged[Stdcall]<void*, uint> lpStartAddress,
        void* lpParameter,
        uint dwCreationFlags,
        uint* lpThreadId);

    /// <summary>
    /// 等待指定的内核对象变为有信号状态（void* 句柄版本）。
    /// <para>常用于等待线程结束、事件触发、信号量可用等场景。</para>
    /// </summary>
    /// <param name="hHandle">要等待的内核对象句柄。</param>
    /// <param name="dwMilliseconds">超时毫秒数，INFINITE=0xFFFFFFFF 表示永久等待。</param>
    /// <returns>WAIT_OBJECT_0=0 表示对象有信号，WAIT_TIMEOUT=0x102 表示超时，WAIT_FAILED=0xFFFFFFFF 表示出错。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial uint WaitForSingleObject(void* hHandle, uint dwMilliseconds);

    /// <summary>
    /// 等待指定的内核对象变为有信号状态（IntPtr 句柄版本）。
    /// </summary>
    /// <param name="hHandle">要等待的内核对象句柄。</param>
    /// <param name="dwMilliseconds">超时毫秒数。</param>
    /// <returns>等待结果代码。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    /// <summary>
    /// 获取当前线程的唯一标识符。
    /// </summary>
    /// <returns>当前线程 ID。</returns>
    [LibraryImport("kernel32.dll")]
    public static partial uint GetCurrentThreadId();

    /// <summary>
    /// 获取当前线程的栈边界，返回栈的低地址和高地址。
    /// <para>可用于检测栈使用量或防止栈溢出。</para>
    /// </summary>
    /// <param name="lowLimit">栈的低地址边界（栈底，即可用栈空间的最低地址）。</param>
    /// <param name="highLimit">栈的高地址边界（栈顶，即栈的起始位置）。</param>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial void GetCurrentThreadStackLimits(out IntPtr lowLimit, out IntPtr highLimit);

    /// <summary>
    /// 终止当前线程的执行，释放线程资源。
    /// </summary>
    /// <param name="dwExitCode">线程退出代码，可由其他线程通过 GetExitCodeThread 获取。</param>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial void ExitThread(uint dwExitCode);

    /// <summary>
    /// 强制终止指定线程（void* 版本）。
    /// <para>警告：强制终止线程可能导致资源泄漏，仅在无法正常退出时使用。</para>
    /// </summary>
    /// <param name="hThread">目标线程句柄。</param>
    /// <param name="dwExitCode">线程退出代码。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool TerminateThread(void* hThread, uint dwExitCode);

    /// <summary>
    /// 强制终止指定线程（IntPtr 版本）。
    /// <para>警告：强制终止线程可能导致资源泄漏，仅在无法正常退出时使用。</para>
    /// </summary>
    /// <param name="hThread">目标线程句柄。</param>
    /// <param name="dwExitCode">线程退出代码。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool TerminateThread(IntPtr hThread, uint dwExitCode);

    /// <summary>
    /// 挂起当前线程，等待指定毫秒数后继续执行。
    /// </summary>
    /// <param name="dwMilliseconds">休眠时间（毫秒）。</param>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial void Sleep(uint dwMilliseconds);

    // ────────────────────────────────────────────────────────────────────────────
    //  线程同步——信号量与临界区
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 创建一个命名或匿名信号量对象。
    /// <para>信号量维护一个计数器，允许多个线程并发访问受保护的资源，计数为 0 时阻塞等待线程。</para>
    /// </summary>
    /// <param name="lpSemaphoreAttributes">安全属性，传 null 使用默认值。</param>
    /// <param name="lInitialCount">初始计数值，范围 [0, lMaximumCount]。</param>
    /// <param name="lMaximumCount">最大计数值，即最多允许同时访问的线程数。</param>
    /// <param name="lpName">信号量名称（命名信号量），匿名时传 null。</param>
    /// <returns>成功返回信号量句柄，失败返回 null。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial void* CreateSemaphoreW(void* lpSemaphoreAttributes, int lInitialCount, int lMaximumCount, char* lpName);

    /// <summary>
    /// 释放信号量，将其计数增加指定值，唤醒相应数量的等待线程。
    /// </summary>
    /// <param name="hSemaphore">信号量句柄。</param>
    /// <param name="lReleaseCount">计数增量，必须大于 0。</param>
    /// <param name="lpPreviousCount">接收释放前计数值的指针，不需要时传 null。</param>
    /// <returns>成功返回 true，失败返回 false。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReleaseSemaphore(void* hSemaphore, int lReleaseCount, int* lpPreviousCount);

    /// <summary>
    /// 初始化临界区对象。临界区用于保护同一进程内多线程对共享资源的互斥访问，性能优于互斥量。
    /// </summary>
    /// <param name="lpCriticalSection">指向 CRITICAL_SECTION 结构体的指针。</param>
    [LibraryImport("kernel32.dll")]
    public static partial void InitializeCriticalSection(void* lpCriticalSection);

    /// <summary>
    /// 进入临界区，获取独占访问权。若临界区已被其他线程持有，则阻塞直到可用。
    /// </summary>
    /// <param name="lpCriticalSection">指向已初始化的 CRITICAL_SECTION 结构体的指针。</param>
    [LibraryImport("kernel32.dll")]
    public static partial void EnterCriticalSection(void* lpCriticalSection);

    /// <summary>
    /// 离开临界区，释放独占访问权，允许其他等待线程进入。
    /// </summary>
    /// <param name="lpCriticalSection">指向已初始化的 CRITICAL_SECTION 结构体的指针。</param>
    [LibraryImport("kernel32.dll")]
    public static partial void LeaveCriticalSection(void* lpCriticalSection);

    /// <summary>
    /// 销毁临界区对象，释放其占用的系统资源。销毁后不可再使用，需重新初始化。
    /// </summary>
    /// <param name="lpCriticalSection">指向要销毁的 CRITICAL_SECTION 结构体的指针。</param>
    [LibraryImport("kernel32.dll")]
    public static partial void DeleteCriticalSection(void* lpCriticalSection);

    // ────────────────────────────────────────────────────────────────────────────
    //  错误处理
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 获取调用线程的最后一个 Win32 错误码，用于诊断 API 调用失败的原因。
    /// <para>常见错误码：2=文件未找到，5=访问被拒绝，997=ERROR_IO_PENDING（异步操作挂起，非真正错误）。</para>
    /// </summary>
    /// <returns>最后一个错误码。</returns>
    [LibraryImport("kernel32.dll")]
    public static unsafe partial uint GetLastError();

    // ────────────────────────────────────────────────────────────────────────────
    //  Winsock 初始化
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 初始化 Winsock 库，必须在调用任何其他 Winsock 函数之前调用（WSADATA* 版本）。
    /// <para>通常请求版本 2.2，即 wVersionRequired = 0x0202。</para>
    /// </summary>
    /// <param name="wVersionRequired">请求的 Winsock 版本，高字节为次版本，低字节为主版本。</param>
    /// <param name="lpWSAData">接收 Winsock 实现信息的 WSADATA 结构体指针。</param>
    /// <returns>成功返回 0，失败返回 Winsock 错误码。</returns>
    [LibraryImport("ws2_32.dll")]
    public static unsafe partial int WSAStartup(ushort wVersionRequired, WSADATA* lpWSAData);

    /// <summary>
    /// 初始化 Winsock 库（WSAData out 参数版本）。
    /// </summary>
    /// <param name="wVersionRequested">请求的 Winsock 版本。</param>
    /// <param name="wsaData">接收 Winsock 实现信息的 WSAData 结构体。</param>
    /// <returns>成功返回 0，失败返回 Winsock 错误码。</returns>
    [LibraryImport("Ws2_32.dll", SetLastError = true)]
    public static partial int WSAStartup(ushort wVersionRequested, out WindowsHttpApi.WSAData wsaData);

    /// <summary>
    /// 终止 Winsock 库的使用，释放相关资源。每次 WSAStartup 调用都应对应一次 WSACleanup。
    /// </summary>
    /// <returns>成功返回 0，失败返回 SOCKET_ERROR。</returns>
    [LibraryImport("ws2_32.dll")]
    public static unsafe partial int WSACleanup();

    // ────────────────────────────────────────────────────────────────────────────
    //  Winsock 套接字操作
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 创建一个套接字（nint 版本）。
    /// </summary>
    /// <param name="af">地址族，如 AF_INET=2（IPv4）、AF_INET6=23（IPv6）。</param>
    /// <param name="type">套接字类型，如 SOCK_STREAM=1（TCP）、SOCK_DGRAM=2（UDP）。</param>
    /// <param name="protocol">协议，如 IPPROTO_TCP=6，通常传 0 自动选择。</param>
    /// <returns>成功返回套接字句柄，失败返回 INVALID_SOCKET。</returns>
    [LibraryImport("ws2_32.dll")]
    public static unsafe partial nint socket(int af, int type, int protocol);

    /// <summary>
    /// 创建一个套接字（IntPtr 版本，_raw 后缀区分重载）。
    /// </summary>
    /// <param name="af">地址族。</param>
    /// <param name="type">套接字类型。</param>
    /// <param name="protocol">协议。</param>
    /// <returns>成功返回套接字句柄，失败返回 INVALID_SOCKET。</returns>
    [LibraryImport("Ws2_32.dll", EntryPoint = "socket", SetLastError = true)]
    public static partial IntPtr socket_raw(int af, int type, int protocol);

    /// <summary>
    /// 将套接字绑定到指定的本地地址和端口。
    /// </summary>
    /// <param name="s">套接字句柄。</param>
    /// <param name="addr">要绑定的本地地址结构体指针。</param>
    /// <param name="namelen">地址结构体的长度。</param>
    /// <returns>成功返回 0，失败返回 SOCKET_ERROR。</returns>
    [LibraryImport("ws2_32.dll")]
    public static unsafe partial int bind(nint s, sockaddr_in* addr, int namelen);

    /// <summary>
    /// 将套接字置于监听状态，准备接受传入连接。
    /// </summary>
    /// <param name="s">套接字句柄。</param>
    /// <param name="backlog">挂起连接队列的最大长度。</param>
    /// <returns>成功返回 0，失败返回 SOCKET_ERROR。</returns>
    [LibraryImport("ws2_32.dll")]
    public static unsafe partial int listen(nint s, int backlog);

    /// <summary>
    /// 向指定地址发起连接（nint 版本）。
    /// </summary>
    /// <param name="s">套接字句柄。</param>
    /// <param name="name">目标地址结构体指针。</param>
    /// <param name="namelen">地址结构体的长度。</param>
    /// <returns>成功返回 0，失败返回 SOCKET_ERROR。</returns>
    [LibraryImport("ws2_32.dll")]
    public static unsafe partial int connect(nint s, sockaddr_in* name, int namelen);

    /// <summary>
    /// 向指定地址发起连接（IntPtr 版本，_raw 后缀区分重载）。
    /// </summary>
    /// <param name="s">套接字句柄。</param>
    /// <param name="addr">目标地址结构体指针（以 IntPtr 传入）。</param>
    /// <param name="addrlen">地址结构体的长度。</param>
    /// <returns>成功返回 0，失败返回 SOCKET_ERROR。</returns>
    [LibraryImport("Ws2_32.dll", EntryPoint = "connect", SetLastError = true)]
    public static partial int connect_raw(IntPtr s, IntPtr addr, int addrlen);

    /// <summary>
    /// 关闭套接字并释放资源。
    /// </summary>
    /// <param name="s">套接字句柄。</param>
    /// <returns>成功返回 0，失败返回 SOCKET_ERROR。</returns>
    [LibraryImport("ws2_32.dll")]
    public static unsafe partial int closesocket(nint s);

    /// <summary>
    /// 关闭套接字（IntPtr 版本，_raw 后缀区分重载）。
    /// </summary>
    /// <param name="s">套接字句柄。</param>
    /// <returns>成功返回 0，失败返回 SOCKET_ERROR。</returns>
    [LibraryImport("Ws2_32.dll", EntryPoint = "closesocket", SetLastError = true)]
    public static partial int closesocket_raw(IntPtr s);

    /// <summary>
    /// 通过套接字发送数据（byte* 版本，_raw 后缀区分重载）。
    /// </summary>
    /// <param name="s">套接字句柄。</param>
    /// <param name="buf">指向要发送数据的缓冲区。</param>
    /// <param name="len">要发送的字节数。</param>
    /// <param name="flags">发送标志，通常传 0。</param>
    /// <returns>实际发送的字节数，失败返回 SOCKET_ERROR。</returns>
    [LibraryImport("Ws2_32.dll", EntryPoint = "send", SetLastError = true)]
    public static unsafe partial int send_raw(IntPtr s, byte* buf, UIntPtr len, int flags);

    /// <summary>
    /// 从套接字接收数据（byte* 版本，_raw 后缀区分重载）。
    /// </summary>
    /// <param name="s">套接字句柄。</param>
    /// <param name="buf">指向接收数据的缓冲区。</param>
    /// <param name="len">缓冲区最大容量。</param>
    /// <param name="flags">接收标志，通常传 0。</param>
    /// <returns>实际接收的字节数，0 表示连接已关闭，失败返回 SOCKET_ERROR。</returns>
    [LibraryImport("Ws2_32.dll", EntryPoint = "recv", SetLastError = true)]
    public static unsafe partial int recv_raw(IntPtr s, byte* buf, UIntPtr len, int flags);

    /// <summary>
    /// 设置套接字选项（void* 选项值版本）。
    /// <para>常用选项：SO_REUSEADDR、TCP_NODELAY、SO_RCVTIMEO、SO_SNDTIMEO。</para>
    /// </summary>
    /// <param name="s">套接字句柄。</param>
    /// <param name="level">选项所在协议层，如 SOL_SOCKET=0xFFFF、IPPROTO_TCP=6。</param>
    /// <param name="optname">选项名称。</param>
    /// <param name="optval">指向选项值的缓冲区。</param>
    /// <param name="optlen">选项值的长度。</param>
    /// <returns>成功返回 0，失败返回 SOCKET_ERROR。</returns>
    [LibraryImport("ws2_32.dll")]
    public static unsafe partial int setsockopt(nint s, int level, int optname, void* optval, int optlen);

    /// <summary>
    /// 设置套接字选项（IntPtr 选项值版本，_raw 后缀区分重载）。
    /// </summary>
    /// <param name="s">套接字句柄。</param>
    /// <param name="level">选项所在协议层。</param>
    /// <param name="optname">选项名称。</param>
    /// <param name="optval">选项值（以 IntPtr 传入）。</param>
    /// <param name="optlen">选项值的长度。</param>
    /// <returns>成功返回 0，失败返回 SOCKET_ERROR。</returns>
    [LibraryImport("Ws2_32.dll", EntryPoint = "setsockopt", SetLastError = true)]
    public static partial int setsockopt_raw(IntPtr s, int level, int optname, IntPtr optval, int optlen);

    /// <summary>
    /// 通过主机名进行 DNS 解析，返回 hostent 结构体指针（nint 版本）。
    /// <para>注意：此函数非线程安全，建议改用 getaddrinfo。</para>
    /// </summary>
    /// <param name="name">主机名字符串，如 "example.com"。</param>
    /// <returns>指向 hostent 结构体的指针，失败返回 null。</returns>
    [LibraryImport("Ws2_32.dll", EntryPoint = "gethostbyname", SetLastError = true)]
    public static unsafe partial IntPtr gethostbyname_raw(byte* name);

    /// <summary>
    /// 获取与套接字关联的远端地址（客户端 IP 和端口）。
    /// </summary>
    /// <param name="s">已连接的套接字句柄。</param>
    /// <param name="name">接收远端地址信息的 sockaddr_in 结构体指针。</param>
    /// <param name="namelen">地址结构体的长度指针，调用前设为结构体大小，调用后更新为实际大小。</param>
    /// <returns>成功返回 0，失败返回 SOCKET_ERROR。</returns>
    [LibraryImport("ws2_32.dll")]
    public static unsafe partial int getpeername(nint s, sockaddr_in* name, int* namelen);

    // ────────────────────────────────────────────────────────────────────────────
    //  Winsock 异步 I/O
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 对套接字执行 I/O 控制操作，常用于获取扩展函数指针（如 AcceptEx、ConnectEx）。
    /// </summary>
    /// <param name="s">套接字句柄。</param>
    /// <param name="dwIoControlCode">控制代码，如 SIO_GET_EXTENSION_FUNCTION_POINTER=0xC8000006。</param>
    /// <param name="lpvInBuffer">输入缓冲区指针。</param>
    /// <param name="cbInBuffer">输入缓冲区大小。</param>
    /// <param name="lpvOutBuffer">输出缓冲区指针。</param>
    /// <param name="cbOutBuffer">输出缓冲区大小。</param>
    /// <param name="lpcbBytesReturned">实际写入输出缓冲区的字节数。</param>
    /// <param name="lpOverlapped">重叠结构指针，同步调用传 null。</param>
    /// <param name="lpCompletionRoutine">完成例程，通常传 0。</param>
    /// <returns>成功返回 0，失败返回 SOCKET_ERROR。</returns>
    [LibraryImport("ws2_32.dll")]
    public static unsafe partial int WSAIoctl(
        nint s, uint dwIoControlCode,
        void* lpvInBuffer, uint cbInBuffer,
        void* lpvOutBuffer, uint cbOutBuffer,
        uint* lpcbBytesReturned,
        OVERLAPPED* lpOverlapped,
        nint lpCompletionRoutine);

    /// <summary>
    /// 以异步（重叠）方式从套接字接收数据。
    /// <para>函数可能立即返回 SOCKET_ERROR 并设置错误码 WSA_IO_PENDING，此为正常现象，表示操作已挂起，
    /// 实际完成通知将通过 IOCP 或完成例程投递。</para>
    /// </summary>
    /// <param name="s">套接字句柄。</param>
    /// <param name="lpBuffers">指向 WSABUF 结构体数组，描述接收缓冲区。</param>
    /// <param name="dwBufferCount">缓冲区数量。</param>
    /// <param name="lpNumberOfBytesRecvd">同步完成时实际接收的字节数。</param>
    /// <param name="lpFlags">接收标志。</param>
    /// <param name="lpOverlapped">重叠结构指针，异步模式必须传入。</param>
    /// <param name="lpCompletionRoutine">完成例程，使用 IOCP 时传 0。</param>
    /// <returns>同步成功返回 0；异步挂起时返回 SOCKET_ERROR，错误码为 WSA_IO_PENDING。</returns>
    [LibraryImport("ws2_32.dll")]
    public static unsafe partial int WSARecv(
        nint s,
        WSABUF* lpBuffers, uint dwBufferCount,
        uint* lpNumberOfBytesRecvd,
        uint* lpFlags,
        OVERLAPPED* lpOverlapped,
        nint lpCompletionRoutine);

    /// <summary>
    /// 以异步（重叠）方式向套接字发送数据。
    /// <para>函数可能立即返回 SOCKET_ERROR 并设置错误码 WSA_IO_PENDING，此为正常现象，
    /// 实际完成通知将通过 IOCP 或完成例程投递。</para>
    /// </summary>
    /// <param name="s">套接字句柄。</param>
    /// <param name="lpBuffers">指向 WSABUF 结构体数组，描述发送缓冲区。</param>
    /// <param name="dwBufferCount">缓冲区数量。</param>
    /// <param name="lpNumberOfBytesSent">同步完成时实际发送的字节数。</param>
    /// <param name="dwFlags">发送标志，通常传 0。</param>
    /// <param name="lpOverlapped">重叠结构指针，异步模式必须传入。</param>
    /// <param name="lpCompletionRoutine">完成例程，使用 IOCP 时传 0。</param>
    /// <returns>同步成功返回 0；异步挂起时返回 SOCKET_ERROR，错误码为 WSA_IO_PENDING。</returns>
    [LibraryImport("ws2_32.dll")]
    public static unsafe partial int WSASend(
        nint s,
        WSABUF* lpBuffers, uint dwBufferCount,
        uint* lpNumberOfBytesSent,
        uint dwFlags,
        OVERLAPPED* lpOverlapped,
        nint lpCompletionRoutine);

    // ────────────────────────────────────────────────────────────────────────────
    //  字节序转换
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 将主机字节序的 16 位无符号整数转换为网络字节序（大端序）。常用于设置端口号。
    /// </summary>
    /// <param name="hostshort">主机字节序的 16 位整数。</param>
    /// <returns>网络字节序的 16 位整数。</returns>
    [LibraryImport("Ws2_32.dll", EntryPoint = "htons", SetLastError = false)]
    public static partial ushort htons(ushort hostshort);

    /// <summary>
    /// 将 16 位无符号整数从网络字节序转换为主机字节序。
    /// </summary>
    /// <param name="netshort">网络字节序的 16 位整数。</param>
    /// <returns>主机字节序的 16 位整数。</returns>
    [LibraryImport("ws2_32.dll")]
    public static unsafe partial ushort ntohs(ushort netshort);

    /// <summary>
    /// 将 32 位无符号整数从网络字节序转换为主机字节序。
    /// </summary>
    /// <param name="netlong">网络字节序的 32 位整数。</param>
    /// <returns>主机字节序的 32 位整数。</returns>
    [LibraryImport("ws2_32.dll")]
    public static unsafe partial uint ntohl(uint netlong);
}
