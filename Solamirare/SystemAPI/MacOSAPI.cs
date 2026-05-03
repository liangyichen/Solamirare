namespace Solamirare;

using System.Text;

/// <summary>
/// macOS 系统原生 API 封装。
/// <para>维护提示：虽然 macOS 与 Linux 在部分 API 上的签名完全相同，但是考虑到两个系统的子版本以及更新频率完全不一样，
/// 随着时间的推移，共用 POSIX API 会带来潜在隐患，所以两个系统必须独立使用自己的 API。</para>
/// </summary>
internal unsafe partial class MacOSAPI
{
    // ────────────────────────────────────────────────────────────────────────────
    //  动态库加载
    // ────────────────────────────────────────────────────────────────────────────




    /// <summary>
    /// 上下文切换函数，由 coroutine_switch_arm64.S 实现。
    /// 保存 from 的寄存器，恢复 to 的寄存器。
    /// </summary>
    [LibraryImport("libcoroutine", EntryPoint = "coroutine_switch")]
    public static partial void coroutine_switch(
        MacOSCoroutineContext* from,
        MacOSCoroutineContext* to);


    [LibraryImport("libcoroutine", EntryPoint = "coroutine_entry_trampoline")]
    public static partial void coroutine_entry_trampoline();



    /// <summary>
    /// 打开一个动态库并返回其句柄，供后续通过 <see cref="dlsym"/> 查找符号使用。
    /// </summary>
    /// <param name="filename">动态库的文件路径。</param>
    /// <param name="flags">加载标志，如 RTLD_NOW=2、RTLD_LAZY=1。</param>
    /// <returns>成功返回库句柄，失败返回 <see cref="IntPtr.Zero"/>。</returns>
    [LibraryImport("libdl.dylib")]
    public static partial IntPtr dlopen(byte* filename, int flags);

    /// <summary>
    /// 从已打开的动态库句柄中查找指定符号的地址。
    /// </summary>
    /// <param name="handle">由 <see cref="dlopen"/> 返回的库句柄。</param>
    /// <param name="symbol">要查找的符号名称（C 字符串）。</param>
    /// <returns>符号地址，未找到时返回 <see cref="IntPtr.Zero"/>。</returns>
    [LibraryImport("libdl.dylib")]
    public static partial IntPtr dlsym(IntPtr handle, byte* symbol);

    public static unsafe IntPtr dlsym(IntPtr handle, string symbol)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(symbol + "\0");
        fixed (byte* p = bytes)
        {
            return dlsym(handle, p);
        }
    }

    private const int RTLD_NOW = 2;

    public static IntPtr OpenDispatchLibrary()
    {
        fixed (byte* p = Encoding.ASCII.GetBytes("libSystem.dylib\0"))
        {
            return dlopen(p, RTLD_NOW);
        }
    }

    [LibraryImport("libSystem.dylib")]
    public static partial IntPtr dispatch_get_global_queue(long identifier, ulong flags);

    [LibraryImport("libSystem.dylib")]
    public static partial IntPtr dispatch_source_create(IntPtr type, ulong handle, ulong mask, IntPtr queue);

    [LibraryImport("libSystem.dylib")]
    public static partial void dispatch_source_set_event_handler_f(IntPtr source, delegate* unmanaged<void*, void> handler);

    [LibraryImport("libSystem.dylib")]
    public static partial void dispatch_source_set_cancel_handler_f(IntPtr source, delegate* unmanaged<void*, void> handler);

    [LibraryImport("libSystem.dylib")]
    public static partial void dispatch_source_cancel(IntPtr source);

    [LibraryImport("libSystem.dylib")]
    public static partial void dispatch_resume(IntPtr source);

    [LibraryImport("libSystem.dylib")]
    public static partial void dispatch_release(IntPtr obj);

    [LibraryImport("libSystem.dylib")]
    public static partial void dispatch_set_context(IntPtr o, void* context);

    [LibraryImport("libSystem.dylib")]
    public static partial void* dispatch_get_context(IntPtr o);

    /// <summary>
    /// 设置指定内存区域的访问保护属性。
    /// <para>常用于为协程栈底设置不可访问的 Guard Page，防止栈溢出时静默覆盖相邻内存。</para>
    /// <para>
    /// prot 参数：
    /// PROT_NONE  = 0（不可读、不可写、不可执行）
    /// PROT_READ  = 1（可读）
    /// PROT_WRITE = 2（可写）
    /// PROT_EXEC  = 4（可执行）
    /// 可组合使用，如 PROT_READ | PROT_WRITE = 3。
    /// </para>
    /// </summary>
    /// <param name="addr">目标内存区域的起始地址，必须是页大小（4096字节）对齐的地址。</param>
    /// <param name="len">区域大小（字节），必须是页大小的整数倍。</param>
    /// <param name="prot">新的内存保护属性。</param>
    /// <returns>成功返回 0，失败返回 -1 并设置 errno。</returns>
    [LibraryImport("libSystem.dylib", EntryPoint = "mprotect")]
    public static partial int MProtect(void* addr, nuint len, int prot);

    // ────────────────────────────────────────────────────────────────────────────
    //  基础文件 I/O
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 打开或创建一个文件，返回文件描述符。失败时返回 -1 并设置 errno。
    /// <para><c>flags</c>：O_RDONLY=0, O_WRONLY=1, O_RDWR=2, O_CREAT=0x200 等。</para>
    /// <para><c>mode</c>：仅在创建文件时有效，指定权限位，如 0644。</para>
    /// </summary>
    /// <param name="path">文件路径。</param>
    /// <param name="flags">打开标志。</param>
    /// <param name="mode">创建文件时的权限位。</param>
    /// <returns>成功返回文件描述符，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib", SetLastError = true)]
    public static partial int open(byte* path, int flags, int mode);

    /// <summary>
    /// 打开文件
    /// </summary>
    /// <param name="pathname">文件路径。</param>
    /// <param name="flags">打开标志。</param>
    /// <param name="mode">创建文件时的权限位。</param>
    /// <returns>成功返回文件描述符，失败返回 -1。</returns>
    [LibraryImport("libc", EntryPoint = "open", SetLastError = true)]
    public static partial int open(byte* pathname, int flags, uint mode);

    /// <summary>
    /// 关闭一个已打开的文件描述符，释放相关内核资源。
    /// </summary>
    /// <param name="fd">要关闭的文件描述符。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib")]
    public static partial int close(int fd);

    /// <summary>
    /// 对已打开的文件描述符执行各种控制操作，如设置非阻塞模式（F_SETFL + O_NONBLOCK）。
    /// </summary>
    /// <param name="fd">文件描述符。</param>
    /// <param name="cmd">控制命令，如 F_GETFL=3、F_SETFL=4。</param>
    /// <param name="arg">命令参数。</param>
    /// <returns>依命令不同而异，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib")]
    public static partial int fcntl(int fd, int cmd, int arg);

    /// <summary>
    /// 通过路径修改文件的访问权限位。
    /// </summary>
    /// <param name="path">文件路径。</param>
    /// <param name="mode">新的权限位，如 0644。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib")]
    public static partial int chmod(byte* path, int mode);

    /// <summary>
    /// 通过文件描述符修改文件的访问权限位。
    /// </summary>
    /// <param name="fd">文件描述符。</param>
    /// <param name="mode">新的权限位，如 0644。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int fchmod(int fd, int mode);

    /// <summary>
    /// 移动文件描述符的读写偏移量。
    /// <para><c>whence</c>：SEEK_SET=0（从文件头），SEEK_CUR=1（从当前位置），SEEK_END=2（从文件尾）。</para>
    /// </summary>
    /// <param name="fd">文件描述符。</param>
    /// <param name="offset">偏移量，单位为字节。</param>
    /// <param name="whence">偏移基准位置。</param>
    /// <returns>成功返回新的偏移量，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib")]
    public static partial long lseek(int fd, long offset, int whence);

    /// <summary>
    /// 从文件描述符中读取最多 <c>count</c> 字节到缓冲区 <c>buf</c>。
    /// <para><c>buf</c> 必须指向一个足够大的内存缓冲区，以容纳预期读取的数据，
    /// 通常定义一个数组并将其首地址（指针）传入。</para>
    /// </summary>
    /// <param name="fd">接收来自 open 函数的结果。</param>
    /// <param name="buf">指向接收数据的缓冲区。</param>
    /// <param name="count">最多尝试读取的字节数。</param>
    /// <returns>实际读取的字节数，0 表示 EOF，-1 表示出错。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int read(int fd, byte* buf, int count);

    /// <summary>
    /// 把数据写入文件。将缓冲区中的 <c>count</c> 字节写入文件描述符。
    /// </summary>
    /// <param name="fd">文件描述符。</param>
    /// <param name="buffer">指向要写入数据的缓冲区。</param>
    /// <param name="count">要写入的字节数。</param>
    /// <returns>实际写入的字节数，-1 表示出错。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int write(int fd, byte* buffer, nuint count);

    /// <summary>
    /// 把内存数据同步写入磁盘，强制将文件描述符关联的内核缓冲区刷新到物理存储。
    /// </summary>
    /// <param name="fd">文件描述符。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int fsync(int fd);

    /// <summary>
    /// 获取文件信息，通过文件描述符获取文件的元数据（大小、权限、时间戳等）。
    /// </summary>
    /// <param name="fd">文件描述符。</param>
    /// <param name="buf">接收文件元数据的 Stat 结构体。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int fstat(int fd, out Stat buf);

    /// <summary>
    /// 删除文件，移除文件的一个硬链接。若硬链接数降为 0 且无进程持有该文件，则文件被彻底删除。
    /// </summary>
    /// <param name="pathname">要删除的文件路径。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int unlink(byte* pathname);

    /// <summary>
    /// 移除文件或空目录。
    /// </summary>
    /// <param name="path">要移除的文件或目录路径。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int remove(byte* path);

    // ────────────────────────────────────────────────────────────────────────────
    //  目录操作
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 获取当前工作目录的绝对路径，将结果写入 <c>buffer</c>。
    /// </summary>
    /// <param name="buffer">接收路径字符串的缓冲区。</param>
    /// <param name="size">缓冲区容量，单位为字节。</param>
    /// <returns>成功返回 <c>buffer</c> 的地址，失败返回 0。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial nint getcwd(byte* buffer, uint size);

    /// <summary>
    /// 将一个相对路径或包含符号链接的路径转换为一个绝对路径。它的主要作用是解析和规范化文件路径。
    /// <para>它会处理路径中的 . (当前目录) 和 .. (上级目录)，并将它们解析为实际的路径。</para>
    /// <para>最终返回的路径是规范化过的绝对路径，不包含任何特殊目录或符号链接。</para>
    /// </summary>
    /// <param name="path">要解析的原始路径。</param>
    /// <param name="resolvedPath">接收解析结果的缓冲区，至少需要 PATH_MAX（1024）字节，传 null 由系统自动分配。</param>
    /// <returns>指向规范化路径字符串的指针，失败返回 null。</returns>
    [LibraryImport("libc", EntryPoint = "realpath")]
    public static partial byte* realpath(byte* path, byte* resolvedPath);

    /// <summary>
    /// 检测文件是否存在或是否具有指定的访问权限。
    /// <para>关于 mode 参数：0 检查文件是否存在，4 检查文件是否可读，2 检查文件是否可写，1 检查文件是否可执行。</para>
    /// </summary>
    /// <param name="path">要检查的文件路径。</param>
    /// <param name="mode">访问权限检查模式。</param>
    /// <returns>具有指定权限返回 0，否则返回 -1。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int access(byte* path, int mode);

    /// <summary>
    /// 创建文件夹。
    /// </summary>
    /// <param name="path">要创建的目录路径。</param>
    /// <param name="mode">目录权限位，如 0755。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int mkdir(byte* path, uint mode);

    /// <summary>
    /// 打开文件夹，返回可供 <see cref="readdir"/> 遍历的目录流指针。
    /// </summary>
    /// <param name="name">要打开的目录路径。</param>
    /// <returns>成功返回目录流指针，失败返回 null。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial char* opendir(byte* name);

    /// <summary>
    /// 读取一个目录中的内容，它返回目录中的每一个条目。每次调用返回下一个条目，目录读完时返回 null。
    /// </summary>
    /// <param name="dir">由 <see cref="opendir"/> 返回的目录流指针。</param>
    /// <returns>指向当前目录条目信息的指针，读完时返回 null。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial char* readdir(char* dir);

    /// <summary>
    /// 关闭文件夹的访问，释放由 <see cref="opendir"/> 打开的目录流所占用的资源。
    /// </summary>
    /// <param name="dir">由 <see cref="opendir"/> 返回的目录流指针。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int closedir(char* dir);

    // ────────────────────────────────────────────────────────────────────────────
    //  异步 I/O（AIO）
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 提交一个异步读请求，立即返回，实际 I/O 在后台完成。
    /// 可通过 <see cref="aio_error"/> 轮询状态，通过 <see cref="aio_return"/> 获取结果。
    /// </summary>
    /// <param name="cb">指向已初始化的 aiocb 控制块。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib")]
    public static partial int aio_read(void* cb);

    /// <summary>
    /// 提交一个异步写请求，立即返回，实际 I/O 在后台完成。
    /// 可通过 <see cref="aio_error"/> 轮询状态，通过 <see cref="aio_return"/> 获取结果。
    /// </summary>
    /// <param name="cb">指向已初始化的 aiocb 控制块。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib")]
    public static partial int aio_write(void* cb);

    /// <summary>
    /// 查询异步 I/O 操作的当前错误状态。
    /// </summary>
    /// <param name="cb">指向 aiocb 控制块。</param>
    /// <returns>返回 0 表示操作已成功完成，EINPROGRESS 表示仍在进行中，其他值为错误码。</returns>
    [LibraryImport("libSystem.dylib")]
    public static partial int aio_error(void* cb);

    /// <summary>
    /// 获取已完成的异步 I/O 操作的返回值（实际传输字节数）。
    /// 必须在 <see cref="aio_error"/> 返回 0 后调用，否则结果未定义。
    /// </summary>
    /// <param name="cb">指向已完成的 aiocb 控制块。</param>
    /// <returns>实际传输的字节数，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib")]
    public static partial nint aio_return(void* cb);

    /// <summary>
    /// 阻塞调用线程，直到 <c>list</c> 中至少有一个异步 I/O 操作完成，或等待超时。
    /// </summary>
    /// <param name="list">指向 aiocb 指针数组。</param>
    /// <param name="nent">数组中的元素数量。</param>
    /// <param name="timeout">超时时间（timespec 结构体指针），传 null 表示永久等待。</param>
    /// <returns>成功返回 0，超时返回 -1 并设置 errno 为 EAGAIN。</returns>
    [LibraryImport("libSystem.dylib", SetLastError = true)]
    public static partial int aio_suspend(macos_aiocb** list, int nent, void* timeout);

    // ────────────────────────────────────────────────────────────────────────────
    //  KQueue 事件通知
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 创建一个新的 kqueue 实例，返回其文件描述符。
    /// kqueue 是 macOS/BSD 的高性能 I/O 事件通知机制，类似于 Linux 的 epoll。
    /// </summary>
    /// <returns>成功返回 kqueue 文件描述符，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib")]
    public static partial int kqueue();

    /// <summary>
    /// 向 kqueue 注册事件变更，或等待已就绪的事件。
    /// </summary>
    /// <param name="kq">由 <see cref="kqueue"/> 返回的文件描述符。</param>
    /// <param name="changelist">要注册或修改的事件列表，传 null 表示不变更。</param>
    /// <param name="nchanges">changelist 中的条目数量。</param>
    /// <param name="eventlist">接收已触发事件的缓冲区，传 null 表示不接收。</param>
    /// <param name="nevents">eventlist 的最大容量。</param>
    /// <param name="flags">额外标志位，通常传 0。</param>
    /// <param name="timeout">超时时间（timespec 结构体指针），传 null 表示永久阻塞。</param>
    /// <returns>返回已触发的事件数量，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib")]
    public static partial int kevent64(int kq, KQueueEvent64* changelist, int nchanges, KQueueEvent64* eventlist, int nevents, uint flags, void* timeout);

    // ────────────────────────────────────────────────────────────────────────────
    //  网络 / Socket
    // ────────────────────────────────────────────────────────────────────────────


    /// <summary>
    /// 将 16 位网络字节序转换为主机字节序 (uint16_t)
    /// </summary>
    [LibraryImport("libSystem.dylib", EntryPoint = "ntohs")]
    public static partial ushort ntohs(ushort netshort);

    /// <summary>
    /// 将 32 位网络字节序转换为主机字节序 (uint32_t)
    /// </summary>
    [LibraryImport("libSystem.dylib", EntryPoint = "ntohl")]
    public static partial uint ntohl(uint netlong);

    /// <summary>
    /// 获取对端地址信息 (int socket, struct sockaddr *address, socklen_t *address_len)
    /// </summary>
    [LibraryImport("libSystem.dylib", EntryPoint = "getpeername", SetLastError = true)]
    public static partial int getpeername(int socket, void* address, int* address_len);

    /// <summary>
    /// int bind(int socket, const struct sockaddr *address, socklen_t address_len);
    /// <para>将一个本地网络地址（IP/端口）分配给指定的套接字 (Socket)。</para>
    /// </summary>
    /// <param name="socket">套接字文件描述符。</param>
    /// <param name="address">指向 sockaddr 结构体的指针。</param>
    /// <param name="address_len">地址结构体的长度。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib", EntryPoint = "bind", SetLastError = true)]
    public static partial int bind(int socket, void* address, int address_len);

    /// <summary>
    /// int listen(int socket, int backlog);
    /// <para>将套接字标记为被动套接字，用于接收传入的连接请求。</para>
    /// </summary>
    /// <param name="socket">套接字文件描述符。</param>
    /// <param name="backlog">挂起连接队列的最大长度。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib", EntryPoint = "listen", SetLastError = true)]
    public static partial int listen(int socket, int backlog);

    /// <summary>
    /// int ioctl(int fildes, unsigned long request, ...);
    /// <para>对文件描述符进行各种底层控制操作。在 macOS 上，request 参数通常为 unsigned long (8字节)。</para>
    /// </summary>
    /// <param name="fildes">文件描述符。</param>
    /// <param name="request">控制命令请求码（如 FIONBIO）。</param>
    /// <param name="arg">命令相关的参数指针。</param>
    /// <returns>成功返回 0 或相关值，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib", EntryPoint = "ioctl", SetLastError = true)]
    public static partial int ioctl(int fildes, nuint request, int* arg);

    /// <summary>
    /// int accept(int socket, struct sockaddr *address, socklen_t *address_len);
    /// <para>从已监听套接字的挂起连接队列中提取第一个连接请求，创建一个新的套接字。</para>
    /// </summary>
    /// <param name="socket">监听中的套接字文件描述符。</param>
    /// <param name="address">指向接收客户端地址的缓冲区指针。</param>
    /// <param name="address_len">指向地址长度缓冲区的指针。</param>
    /// <returns>成功返回新建立连接的文件描述符，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib", EntryPoint = "accept", SetLastError = true)]
    public static partial int accept(int socket, void* address, void* address_len);




    /// <summary>
    /// int* __error(void);
    /// <para>macOS 专用：获取当前线程 errno 的内存地址。</para>
    /// <para>注意：在 Linux 上对应的函数是 __errno_location，两者名称不同。</para>
    /// </summary>
    /// <returns>返回指向当前线程错误码 (int) 的指针。</returns>
    [LibraryImport("libSystem.dylib", EntryPoint = "__error")]
    public static partial int* __error();


    /// <summary>
    /// 创建一个网络套接字，返回其文件描述符。
    /// </summary>
    /// <param name="domain">地址族，如 AF_INET=2（IPv4）、AF_INET6=30（IPv6）。</param>
    /// <param name="type">套接字类型，如 SOCK_STREAM=1（TCP）、SOCK_DGRAM=2（UDP）。</param>
    /// <param name="protocol">协议，通常传 0 由系统自动选择。</param>
    /// <returns>成功返回套接字文件描述符，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib", SetLastError = true)]
    public static partial int socket(int domain, int type, int protocol);

    /// <summary>
    /// 向指定地址发起 TCP 连接。
    /// </summary>
    /// <param name="sockfd">套接字文件描述符。</param>
    /// <param name="addr">目标地址结构体指针。</param>
    /// <param name="addrlen">地址结构体的长度。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib", SetLastError = true)]
    public static partial int connect(int sockfd, MacOSHttpPosixApi.sockaddr_in* addr, uint addrlen);

    /// <summary>
    /// 通过套接字发送数据。
    /// </summary>
    /// <param name="sockfd">套接字文件描述符。</param>
    /// <param name="buf">指向要发送数据的缓冲区。</param>
    /// <param name="len">要发送的字节数。</param>
    /// <param name="flags">发送标志，通常传 0。</param>
    /// <returns>实际发送的字节数，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib", SetLastError = true)]
    public static partial IntPtr send(int sockfd, byte* buf, UIntPtr len, int flags);

    /// <summary>
    /// 从套接字接收数据。
    /// </summary>
    /// <param name="sockfd">套接字文件描述符。</param>
    /// <param name="buf">指向接收数据的缓冲区。</param>
    /// <param name="len">缓冲区最大容量。</param>
    /// <param name="flags">接收标志，通常传 0。</param>
    /// <returns>实际接收的字节数，0 表示对端已关闭连接，-1 表示出错。</returns>
    [LibraryImport("libSystem.dylib", SetLastError = true)]
    public static partial IntPtr recv(int sockfd, byte* buf, UIntPtr len, int flags);

    /// <summary>
    /// 设置套接字选项，如 SO_REUSEADDR、TCP_NODELAY 等。
    /// </summary>
    /// <param name="sockfd">套接字文件描述符。</param>
    /// <param name="level">选项所在协议层，如 SOL_SOCKET=0xFFFF、IPPROTO_TCP=6。</param>
    /// <param name="optname">选项名称。</param>
    /// <param name="optval">指向选项值的缓冲区。</param>
    /// <param name="optlen">选项值的长度。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib", SetLastError = true)]
    public static partial int setsockopt(int sockfd, int level, int optname, void* optval, uint optlen);

    /// <summary>
    /// 获取套接字选项。
    /// </summary>
    /// <param name="sockfd">套接字文件描述符。</param>
    /// <param name="level">选项所在协议层。</param>
    /// <param name="optname">选项名称。</param>
    /// <param name="optval">指向接收选项值的缓冲区。</param>
    /// <param name="optlen">缓冲区长度。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libSystem.dylib", SetLastError = true)]
    public static partial int getsockopt(int sockfd, int level, int optname, void* optval, MacOSHttpPosixApi.socklen_t* optlen);

    /// <summary>
    /// 通过主机名进行 DNS 解析，返回 hostent 结构体指针。
    /// <para>注意：此函数非线程安全，生产环境建议改用 getaddrinfo。</para>
    /// </summary>
    /// <param name="name">主机名字符串，如 "example.com"。</param>
    /// <returns>指向 hostent 结构体的指针，解析失败返回 null。</returns>
    [LibraryImport("libSystem.dylib", SetLastError = true)]
    public static partial MacOSHttpPosixApi.hostent* gethostbyname(byte* name);

    /// <summary>
    /// 将主机字节序的 16 位无符号整数转换为网络字节序（大端序）。常用于设置端口号。
    /// </summary>
    /// <param name="hostshort">主机字节序的端口号。</param>
    /// <returns>网络字节序的端口号。</returns>
    [LibraryImport("libSystem.dylib", SetLastError = true)]
    public static partial ushort htons(ushort hostshort);

    // ────────────────────────────────────────────────────────────────────────────
    //  POSIX 线程（pthread）—— 线程生命周期
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 返回调用线程自身的 pthread_t 标识符。
    /// </summary>
    /// <returns>当前线程的 pthread_t 标识符。</returns>
    [LibraryImport("libSystem.dylib", EntryPoint = "pthread_self")]
    public static partial IntPtr pthread_self();

    /// <summary>
    /// 获取指定线程的栈顶地址（macOS 专有 API）。
    /// </summary>
    /// <param name="thread">目标线程的 pthread_t 标识符。</param>
    /// <returns>线程栈的顶部地址（高地址端）。</returns>
    [LibraryImport("libSystem.dylib", EntryPoint = "pthread_get_stackaddr_np")]
    public static partial IntPtr pthread_get_stackaddr_np(IntPtr thread);

    /// <summary>
    /// 获取指定线程的栈大小，单位为字节（macOS 专有 API）。
    /// </summary>
    /// <param name="thread">目标线程的 pthread_t 标识符。</param>
    /// <returns>线程栈的大小，单位为字节。</returns>
    [LibraryImport("libSystem.dylib", EntryPoint = "pthread_get_stacksize_np")]
    public static partial ulong pthread_get_stacksize_np(IntPtr thread);

    /// <summary>
    /// 创建一个新的线程。
    /// </summary>
    /// <param name="thread">输出参数：指向 pthread_t (void*) 的指针，用于存储新线程的 ID。</param>
    /// <param name="attr">线程属性，传 null 使用默认值。</param>
    /// <param name="start_routine">线程入口函数指针。</param>
    /// <param name="arg">传递给线程函数的参数指针。</param>
    /// <returns>0 表示成功，否则返回错误码。</returns>
    [LibraryImport("libc", EntryPoint = "pthread_create")]
    public static partial int pthread_create(void** thread, void* attr, delegate* unmanaged<void*, void*> start_routine, void* arg);

    /// <summary>
    /// 等待指定线程终止，阻塞调用线程直到目标线程结束。
    /// </summary>
    /// <param name="thread">线程 ID (pthread_t)。</param>
    /// <param name="value_ptr">输出参数：接收线程的返回值 (void**)，传 null 表示不关心返回值。</param>
    /// <returns>0 表示成功，否则返回错误码。</returns>
    [LibraryImport("libc", EntryPoint = "pthread_join")]
    public static partial int pthread_join(void* thread, void** value_ptr);

    /// <summary>
    /// 终止当前线程，<c>value_ptr</c> 作为线程返回值，可被调用 <see cref="pthread_join"/> 的线程接收。
    /// </summary>
    /// <param name="value_ptr">返回值指针。</param>
    [LibraryImport("libc", EntryPoint = "pthread_join")]
    public static partial void pthread_exit(void* value_ptr);

    /// <summary>
    /// 将线程标记为分离状态。当分离的线程终止时，其资源会自动释放回系统，无需其他线程 Join。
    /// </summary>
    /// <param name="thread">线程 ID。</param>
    /// <returns>0 表示成功，否则返回错误码。</returns>
    [LibraryImport("libc", EntryPoint = "pthread_detach")]
    public static partial int pthread_detach(void* thread);

    /// <summary>
    /// 向目标线程发送取消请求。线程实际响应时机取决于其取消状态和取消点设置。
    /// </summary>
    /// <param name="thread">目标线程 ID。</param>
    /// <returns>0 表示成功，否则返回错误码。</returns>
    [LibraryImport("libc", EntryPoint = "pthread_cancel")]
    public static partial int pthread_cancel(void* thread);

    // ────────────────────────────────────────────────────────────────────────────
    //  POSIX 线程（pthread）—— 互斥锁（Mutex）
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 初始化互斥锁。<c>attr</c> 传 null 使用默认属性（非递归、进程内可见）。
    /// </summary>
    /// <param name="mutex">指向互斥锁对象的指针。</param>
    /// <param name="attr">互斥锁属性，传 null 使用默认值。</param>
    /// <returns>0 表示成功，否则返回错误码。</returns>
    [LibraryImport("libc", EntryPoint = "pthread_mutex_init")]
    public static partial int pthread_mutex_init(void* mutex, void* attr);

    /// <summary>
    /// 加锁互斥锁。若锁已被其他线程持有，则阻塞直到锁可用。
    /// </summary>
    /// <param name="mutex">指向互斥锁对象的指针。</param>
    /// <returns>0 表示成功，否则返回错误码。</returns>
    [LibraryImport("libc", EntryPoint = "pthread_mutex_lock")]
    public static partial int pthread_mutex_lock(void* mutex);

    /// <summary>
    /// 释放互斥锁，允许其他等待该锁的线程继续执行。
    /// </summary>
    /// <param name="mutex">指向互斥锁对象的指针。</param>
    /// <returns>0 表示成功，否则返回错误码。</returns>
    [LibraryImport("libc", EntryPoint = "pthread_mutex_unlock")]
    public static partial int pthread_mutex_unlock(void* mutex);

    /// <summary>
    /// 销毁互斥锁并释放相关资源。销毁前必须确保锁处于未锁定状态且无线程等待。
    /// </summary>
    /// <param name="mutex">指向互斥锁对象的指针。</param>
    /// <returns>0 表示成功，否则返回错误码。</returns>
    [LibraryImport("libc", EntryPoint = "pthread_mutex_destroy")]
    public static partial int pthread_mutex_destroy(void* mutex);

    // ────────────────────────────────────────────────────────────────────────────
    //  POSIX 线程（pthread）—— 条件变量（Condition Variable）
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 初始化条件变量。<c>attr</c> 传 null 使用默认属性。
    /// </summary>
    /// <param name="cond">指向条件变量对象的指针。</param>
    /// <param name="attr">条件变量属性，传 null 使用默认值。</param>
    /// <returns>0 表示成功，否则返回错误码。</returns>
    [LibraryImport("libc", EntryPoint = "pthread_cond_init")]
    public static partial int pthread_cond_init(void* cond, void* attr);

    /// <summary>
    /// 原子地释放 <c>mutex</c> 并阻塞等待条件变量被触发。被唤醒后自动重新获取 <c>mutex</c>。
    /// </summary>
    /// <param name="cond">指向条件变量对象的指针。</param>
    /// <param name="mutex">调用前必须已持有的互斥锁指针。</param>
    /// <returns>0 表示成功，否则返回错误码。</returns>
    [LibraryImport("libc", EntryPoint = "pthread_cond_wait")]
    public static partial int pthread_cond_wait(void* cond, void* mutex);

    /// <summary>
    /// 唤醒至少一个正在等待该条件变量的线程。
    /// </summary>
    /// <param name="cond">指向条件变量对象的指针。</param>
    /// <returns>0 表示成功，否则返回错误码。</returns>
    [LibraryImport("libc", EntryPoint = "pthread_cond_signal")]
    public static partial int pthread_cond_signal(void* cond);

    /// <summary>
    /// 广播唤醒所有正在等待该条件变量的线程。
    /// </summary>
    /// <param name="cond">指向条件变量对象的指针。</param>
    /// <returns>0 表示成功，否则返回错误码。</returns>
    [LibraryImport("libc", EntryPoint = "pthread_cond_broadcast")]
    public static partial int pthread_cond_broadcast(void* cond);

    /// <summary>
    /// 销毁条件变量并释放相关资源。销毁前必须确保无线程正在等待该条件变量。
    /// </summary>
    /// <param name="cond">指向条件变量对象的指针。</param>
    /// <returns>0 表示成功，否则返回错误码。</returns>
    [LibraryImport("libc", EntryPoint = "pthread_cond_destroy")]
    public static partial int pthread_cond_destroy(void* cond);

        /// <summary>
    /// 带超时的条件变量等待。原子地释放 <c>mutex</c> 并阻塞，直到条件被触发或到达绝对超时时间。
    /// 被唤醒后自动重新获取 <c>mutex</c>。
    /// </summary>
    /// <param name="cond">指向条件变量对象的指针。</param>
    /// <param name="mutex">调用前必须已持有的互斥锁指针。</param>
    /// <param name="abstime">绝对超时时间（UTC），使用 TimeSpec 结构体描述。</param>
    /// <returns>0 表示成功被唤醒，ETIMEDOUT (60) 表示超时，否则返回错误码。</returns>
    [LibraryImport("libc", EntryPoint = "pthread_cond_timedwait")]
    public static partial int pthread_cond_timedwait(void* cond, void* mutex, TimeSpec* abstime);
 
    /// <summary>
    /// ETIMEDOUT 错误码，pthread_cond_timedwait 超时时返回此值。
    /// macOS 上值为 60。
    /// </summary>
    public const int ETIMEDOUT = 60;
 

    // ────────────────────────────────────────────────────────────────────────────
    //  管道 / 轮询
    // ────────────────────────────────────────────────────────────────────────────


    /// <summary>
    /// 获取指定时钟的当前时间，结果写入 <paramref name="tp"/>。
    /// <para>clockid 常量：CLOCK_REALTIME=0（墙钟时间），CLOCK_MONOTONIC=6（单调时间）。</para>
    /// 供 <see cref="TimeSpec.FromMillisecondsFromNow"/> 内部使用，
    /// 为 pthread_cond_timedwait 计算绝对超时时间点。
    /// </summary>
    /// <param name="clockid">时钟类型，传 0 表示 CLOCK_REALTIME。</param>
    /// <param name="tp">接收当前时间的 TimeSpec 结构体指针。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc", EntryPoint = "clock_gettime")]
    public static partial int clock_gettime(int clockid, TimeSpec* tp);


    /// <summary>
    /// 创建一个匿名管道，<c>fds[0]</c> 为读端，<c>fds[1]</c> 为写端。
    /// 常用于线程间或进程间的单向通信。
    /// </summary>
    /// <param name="fds">长度为 2 的 int 数组，索引 0 为读端 fd，索引 1 为写端 fd。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc", EntryPoint = "pipe")]
    public static partial int pipe(int* fds);

    /// <summary>
    /// 轮询多个文件描述符的 I/O 就绪状态，可同时监听读、写、异常等事件。
    /// </summary>
    /// <param name="fds">指向 PollFd 结构体数组，每项描述一个待监听的文件描述符及其关注事件。</param>
    /// <param name="nfds">数组中的元素数量。</param>
    /// <param name="timeout">超时毫秒数：-1 永久阻塞，0 立即返回，正数为等待上限。</param>
    /// <returns>就绪的文件描述符数量，超时返回 0，失败返回 -1。</returns>
    [LibraryImport("libc", EntryPoint = "poll")]
    public static partial int poll(PollFd* fds, uint nfds, int timeout);
}