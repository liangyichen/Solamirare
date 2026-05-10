using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Solamirare;

/// <summary>
/// Linux 系统原生 API 封装。
/// <para>维护提示：虽然 macOS 与 Linux 在部分 API 上的签名完全相同，但是考虑到两个系统的子版本以及更新频率完全不一样，
/// 随着时间的推移，共用 POSIX API 会带来潜在隐患，所以两个系统必须独立使用自己的 API。</para>
/// </summary>
public unsafe partial class LinuxAPI
{
    // ────────────────────────────────────────────────────────────────────────────
    //  基础文件 I/O
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 打开一个文件，返回文件描述符。失败时返回 -1 并设置 errno。
    /// </summary>
    /// <param name="pathname">文件路径。</param>
    /// <param name="flags">打开标志，如 O_RDONLY=0、O_WRONLY=1、O_RDWR=2。</param>
    /// <returns>成功返回文件描述符，失败返回 -1。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int open(byte* pathname, int flags);

    /// <summary>
    /// 打开文件
    /// </summary>
    /// <param name="pathname">文件路径。</param>
    /// <param name="flags">打开标志。</param>
    /// <param name="mode">创建文件时的权限位，如 0644。</param>
    /// <returns>成功返回文件描述符，失败返回 -1。</returns>
    [LibraryImport("libc", EntryPoint = "open", SetLastError = true)]
    public static partial int open(byte* pathname, int flags, uint mode);

    /// <summary>
    /// 打开文件（libc 直接绑定重载）。
    /// </summary>
    /// <param name="p">文件路径。</param>
    /// <param name="f">打开标志。</param>
    /// <param name="m">创建文件时的权限位。</param>
    /// <returns>成功返回文件描述符，失败返回 -1。</returns>
    [LibraryImport("libc")]
    public static partial int open(byte* p, int f, int m);

    /// <summary>
    /// 关闭一个已打开的文件描述符，释放相关内核资源。
    /// </summary>
    /// <param name="fd">要关闭的文件描述符。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc")]
    public static partial int close(int fd);

    /// <summary>
    /// 移动文件描述符的读写偏移量。
    /// <para><c>whence</c>：SEEK_SET=0（从文件头），SEEK_CUR=1（从当前位置），SEEK_END=2（从文件尾）。</para>
    /// </summary>
    /// <param name="fd">文件描述符。</param>
    /// <param name="offset">偏移量，单位为字节。</param>
    /// <param name="whence">偏移基准位置。</param>
    /// <returns>成功返回新的偏移量，失败返回 -1。</returns>
    [LibraryImport("libc")]
    public static partial long lseek(int fd, long offset, int whence);

    /// <summary>
    /// 从文件描述符读取数据（ulong 版本）。
    /// </summary>
    /// <param name="fd">文件描述符。</param>
    /// <param name="buf">指向接收数据的缓冲区。</param>
    /// <param name="count">最多尝试读取的字节数。</param>
    /// <returns>实际读取的字节数，0 表示 EOF，-1 表示出错。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial long read(int fd, byte* buf, ulong count);

    /// <summary>
    /// 读取文件内容。
    /// </summary>
    /// <param name="fd">接收来自 open 函数的结果。</param>
    /// <param name="buf">指向足够大的内存缓冲区，以容纳预期读取的数据。通常定义一个数组并将其首地址（指针）传入。</param>
    /// <param name="count">最多尝试从文件描述符 fd 读取的字节数。</param>
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
    /// 将缓冲区中的数据写入文件描述符（void* 版本，适用于任意类型数据）。
    /// </summary>
    /// <param name="fd">文件描述符。</param>
    /// <param name="buf">指向要写入数据的缓冲区。</param>
    /// <param name="count">要写入的字节数。</param>
    /// <returns>实际写入的字节数，-1 表示出错。</returns>
    [LibraryImport("libc", EntryPoint = "write")]
    public static partial nint write(int fd, void* buf, nuint count);

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
    /// 设置文件权限，通过文件描述符修改文件的访问权限位。
    /// </summary>
    /// <param name="fd">文件描述符。</param>
    /// <param name="mode">新的权限位，如 0644。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int fchmod(int fd, int mode);

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

    /// <summary>
    /// 将文件或设备的一段内容映射到进程的虚拟地址空间。
    /// </summary>
    /// <param name="a">建议映射的起始地址，通常传 null 由内核自动选择。</param>
    /// <param name="l">映射区域的字节长度。</param>
    /// <param name="p">内存保护标志，如 PROT_READ=1、PROT_WRITE=2。</param>
    /// <param name="f">映射标志，如 MAP_SHARED=1、MAP_PRIVATE=2。</param>
    /// <param name="fd">要映射的文件描述符，匿名映射时传 -1。</param>
    /// <param name="o">文件映射的起始偏移量，必须是页大小的整数倍。</param>
    /// <returns>成功返回映射区域的起始地址，失败返回 MAP_FAILED（即 (void*)-1）。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial void* mmap(void* a, nuint l, int p, int f, int fd, long o);

    /// <summary>
    /// 解除由 <see cref="mmap"/> 创建的内存映射，释放对应的虚拟地址空间。
    /// </summary>
    /// <param name="a">映射区域的起始地址。</param>
    /// <param name="l">映射区域的字节长度。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc")]
    public static partial int munmap(void* a, nuint l);

    // ────────────────────────────────────────────────────────────────────────────
    //  目录操作
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 获取当前工作目录的绝对路径，将结果写入 <c>buffer</c>。
    /// </summary>
    /// <param name="buffer">接收路径字符串的缓冲区。</param>
    /// <param name="size">缓冲区容量，单位为字节。</param>
    /// <returns>成功返回 buffer 的地址，失败返回 0。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial nint getcwd(byte* buffer, uint size);

    /// <summary>
    /// 将一个相对路径或包含符号链接的路径转换为一个绝对路径。它的主要作用是解析和规范化文件路径。
    /// <para>它会处理路径中的 . (当前目录) 和 .. (上级目录)，并将它们解析为实际的路径。</para>
    /// <para>最终返回的路径是规范化过的绝对路径，不包含任何特殊目录或符号链接。</para>
    /// </summary>
    /// <param name="path">要解析的原始路径。</param>
    /// <param name="resolvedPath">接收解析结果的缓冲区，至少需要 PATH_MAX（4096）字节，传 null 由系统自动分配。</param>
    /// <returns>指向规范化路径字符串的指针，失败返回 null。</returns>
    [LibraryImport("libc", EntryPoint = "realpath")]
    public static partial byte* RealPath(byte* path, byte* resolvedPath);

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
    [MethodImpl(MethodImplOptions.NoInlining)]
    [LibraryImport("libc", SetLastError = true)]
    public static partial char* opendir(byte* name);

    /// <summary>
    /// 读取一个目录中的内容，它返回目录中的每一个条目。每次调用返回下一个条目，目录读完时返回 null。
    /// </summary>
    /// <param name="dir">由 <see cref="opendir"/> 返回的目录流指针。</param>
    /// <returns>指向当前目录条目信息的指针，读完时返回 null。</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [LibraryImport("libc", SetLastError = true)]
    public static partial char* readdir(char* dir);

    /// <summary>
    /// 关闭文件夹的访问，释放由 <see cref="opendir"/> 打开的目录流所占用的资源。
    /// </summary>
    /// <param name="dir">由 <see cref="opendir"/> 返回的目录流指针。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [LibraryImport("libc", SetLastError = true)]
    public static partial int closedir(char* dir);

    // ────────────────────────────────────────────────────────────────────────────
    //  异步 I/O（AIO）—— linux_aiocb2 版本
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 提交一个异步读请求（linux_aiocb2 版本），立即返回，实际 I/O 在后台完成。
    /// </summary>
    /// <param name="cb">指向已初始化的 linux_aiocb2 控制块。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int aio_read(linux_aiocb* cb);

    /// <summary>
    /// 提交一个异步写请求（linux_aiocb2 版本），立即返回，实际 I/O 在后台完成。
    /// </summary>
    /// <param name="cb">指向已初始化的 linux_aiocb2 控制块。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int aio_write(linux_aiocb* cb);

    /// <summary>
    /// 查询异步 I/O 操作的当前错误状态（linux_aiocb2 版本）。
    /// </summary>
    /// <param name="cb">指向 linux_aiocb2 控制块。</param>
    /// <returns>返回 0 表示操作已成功完成，EINPROGRESS 表示仍在进行中，其他值为错误码。</returns>
    [LibraryImport("libc")]
    public static partial int aio_error(linux_aiocb* cb);

    /// <summary>
    /// 获取已完成的异步 I/O 操作的返回值（linux_aiocb2 版本，实际传输字节数）。
    /// 必须在 <see cref="aio_error(linux_aiocb*)"/> 返回 0 后调用。
    /// </summary>
    /// <param name="cb">指向已完成的 linux_aiocb2 控制块。</param>
    /// <returns>实际传输的字节数，失败返回 -1。</returns>
    [LibraryImport("libc")]
    public static partial nint aio_return(linux_aiocb* cb);

    /// <summary>
    /// 阻塞调用线程，直到 <c>list</c> 中至少有一个异步 I/O 操作完成，或等待超时（linux_aiocb2 版本）。
    /// </summary>
    /// <param name="list">指向 linux_aiocb2 指针数组。</param>
    /// <param name="n">数组中的元素数量。</param>
    /// <param name="timeout">超时时间（timespec 结构体指针），传 null 表示永久等待。</param>
    /// <returns>成功返回 0，超时返回 -1 并设置 errno 为 EAGAIN。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int aio_suspend(linux_aiocb** list, int n, void* timeout);

    // ────────────────────────────────────────────────────────────────────────────
    //  异步 I/O（AIO）—— linux_aiocb 版本
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 提交一个异步读请求（void* 通用版本），立即返回，实际 I/O 在后台完成。
    /// </summary>
    /// <param name="cb">指向已初始化的 aiocb 控制块。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int aio_read(void* cb);

// 追加到 LinuxAPI.cs 的条件变量区块

/// <summary>
/// 带超时的条件变量等待。
/// </summary>
[LibraryImport("libc", EntryPoint = "pthread_cond_timedwait")]
public static partial int pthread_cond_timedwait(void* cond, void* mutex, TimeSpec* abstime);

/// <summary>
/// 获取指定时钟的当前时间。
/// CLOCK_REALTIME = 0，CLOCK_MONOTONIC = 1（Linux 上与 macOS 不同，macOS 是 6）
/// </summary>
[LibraryImport("libc", EntryPoint = "clock_gettime")]
public static partial int clock_gettime(int clockid, TimeSpec* tp);

/// <summary>
/// ETIMEDOUT 错误码，pthread_cond_timedwait 超时时返回此值。
/// Linux 上值为 110，与 macOS（60）不同。
/// </summary>
public const int ETIMEDOUT = 110;

    // ────────────────────────────────────────────────────────────────────────────
    //  网络 / Socket
    // ────────────────────────────────────────────────────────────────────────────




    /// <summary>
    /// 将 16 位网络字节序转换为主机字节序 (uint16_t)
    /// </summary>
    [LibraryImport("libc", EntryPoint = "ntohs")]
    public static partial ushort ntohs(ushort netshort);

    /// <summary>
    /// 将 32 位网络字节序转换为主机字节序 (uint32_t)
    /// </summary>
    [LibraryImport("libc", EntryPoint = "ntohl")]
    public static partial uint ntohl(uint netlong);

    /// <summary>
    /// 获取对端地址信息 (int socket, struct sockaddr *address, socklen_t *address_len)
    /// </summary>
    [LibraryImport("libc", EntryPoint = "getpeername", SetLastError = true)]
    public static partial int getpeername(int socket, void* address, int* address_len);

    /// <summary>
    /// int accept(int sockfd, struct sockaddr *addr, socklen_t *addrlen)
    /// </summary>
    [LibraryImport("libc", EntryPoint = "accept", SetLastError = true)]
    public static partial int accept(int sockfd, void* addr, void* addrlen);

    /// <summary>
    /// int fcntl(int fd, int cmd, int arg)
    /// </summary>
    [LibraryImport("libc", EntryPoint = "fcntl", SetLastError = true)]
    public static partial int fcntl(int fd, int cmd, int arg);


    /// <summary>
    /// int epoll_create1(int flags)
    /// </summary>
    [LibraryImport("libc", EntryPoint = "epoll_create1", SetLastError = true)]
    public static partial int epoll_create1(int flags);

    /// <summary>
    /// int epoll_ctl(int epfd, int op, int fd, struct epoll_event *event)
    /// </summary>
    [LibraryImport("libc", EntryPoint = "epoll_ctl", SetLastError = true)]
    public static partial int epoll_ctl(int epfd, int op, int fd, epoll_event* @event);

    /// <summary>
    /// int epoll_wait(int epfd, struct epoll_event *events, int maxevents, int timeout)
    /// </summary>
    [LibraryImport("libc", EntryPoint = "epoll_wait", SetLastError = true)]
    public static partial int epoll_wait(int epfd, epoll_event* events, int maxevents, int timeout);

    /// <summary>
    /// int* __errno_location()
    /// Linux 专用：获取当前线程 errno 指针。
    /// 配合 SetLastError = true 使用时，也可以通过 Marshal.GetLastPInvokeError() 获取。
    /// </summary>
    [LibraryImport("libc", EntryPoint = "__errno_location")]
    public static partial int* __errno_location();

    /// <summary>
    /// 创建一个网络套接字，返回其文件描述符。
    /// </summary>
    /// <param name="domain">地址族，如 AF_INET=2（IPv4）、AF_INET6=10（IPv6）。</param>
    /// <param name="type">套接字类型，如 SOCK_STREAM=1（TCP）、SOCK_DGRAM=2（UDP）。</param>
    /// <param name="protocol">协议，通常传 0 由系统自动选择。</param>
    /// <returns>成功返回套接字文件描述符，失败返回 -1。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int socket(int domain, int type, int protocol);

    /// <summary>
    /// 向指定地址发起 TCP 连接。
    /// </summary>
    /// <param name="sockfd">套接字文件描述符。</param>
    /// <param name="addr">目标地址结构体指针。</param>
    /// <param name="addrlen">地址结构体的长度。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc", SetLastError = true)]
    public static partial int connect(int sockfd, sockaddr_in* addr, uint addrlen);

    /// <summary>
    /// 将套接字绑定到指定的本地地址和端口。
    /// </summary>
    /// <param name="fd">套接字文件描述符。</param>
    /// <param name="addr">要绑定的本地地址结构体指针。</param>
    /// <param name="len">地址结构体的长度。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc")]
    public static partial int bind(int fd, sockaddr_in* addr, uint len);

    /// <summary>
    /// 将套接字设置为监听状态，准备接受入站连接。
    /// </summary>
    /// <param name="fd">套接字文件描述符。</param>
    /// <param name="backlog">等待连接队列的最大长度。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc")]
    public static partial int listen(int fd, int backlog);

    /// <summary>
    /// 关闭套接字的部分或全部通信通道。
    /// </summary>
    /// <param name="fd">套接字文件描述符。</param>
    /// <param name="how">关闭方式：SHUT_RD=0（停止接收），SHUT_WR=1（停止发送），SHUT_RDWR=2（双向关闭）。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc")]
    public static partial int shutdown(int fd, int how);

    /// <summary>
    /// 通过套接字发送数据。
    /// </summary>
    /// <param name="sockfd">套接字文件描述符。</param>
    /// <param name="buf">指向要发送数据的缓冲区。</param>
    /// <param name="len">要发送的字节数。</param>
    /// <param name="flags">发送标志，通常传 0。</param>
    /// <returns>实际发送的字节数，失败返回 -1。</returns>
    [LibraryImport("libc", SetLastError = true, EntryPoint = "send")]
    public static partial int send(int sockfd, byte* buf, UIntPtr len, int flags);

    /// <summary>
    /// 从套接字接收数据。
    /// </summary>
    /// <param name="sockfd">套接字文件描述符。</param>
    /// <param name="buf">指向接收数据的缓冲区。</param>
    /// <param name="len">缓冲区最大容量。</param>
    /// <param name="flags">接收标志，通常传 0。</param>
    /// <returns>实际接收的字节数，0 表示对端已关闭连接，-1 表示出错。</returns>
    [LibraryImport("libc", SetLastError = true, EntryPoint = "recv")]
    public static partial int recv(int sockfd, byte* buf, UIntPtr len, int flags);

    /// <summary>
    /// 设置套接字选项（通用版本），如 SO_REUSEADDR、TCP_NODELAY 等。
    /// </summary>
    /// <param name="fd">套接字文件描述符。</param>
    /// <param name="lvl">选项所在协议层，如 SOL_SOCKET=1、IPPROTO_TCP=6。</param>
    /// <param name="opt">选项名称。</param>
    /// <param name="val">指向选项值的缓冲区。</param>
    /// <param name="len">选项值的长度。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc")]
    public static partial int setsockopt(int fd, int lvl, int opt, void* val, uint len);

    /// <summary>
    /// 设置套接字选项（timeval 版本），用于设置超时等时间类选项。
    /// </summary>
    /// <param name="sockfd">套接字文件描述符。</param>
    /// <param name="level">选项所在协议层。</param>
    /// <param name="optname">选项名称，如 SO_RCVTIMEO、SO_SNDTIMEO。</param>
    /// <param name="optval">指向 timeval 结构体的指针。</param>
    /// <param name="optlen">timeval 结构体的长度。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc", SetLastError = true, EntryPoint = "setsockopt")]
    public static partial int setsockopt(int sockfd, int level, int optname, timeval* optval, uint optlen);

    /// <summary>
    /// 通过主机名进行 DNS 解析，返回 hostent 结构体指针。
    /// <para>注意：此函数非线程安全，生产环境建议改用 getaddrinfo。</para>
    /// </summary>
    /// <param name="name">主机名字符串，如 "example.com"。</param>
    /// <returns>指向 hostent 结构体的指针，解析失败返回 null。</returns>
    [LibraryImport("libc", SetLastError = true, EntryPoint = "gethostbyname")]
    public static partial hostent* gethostbyname(byte* name);

    /// <summary>
    /// 将主机字节序的 16 位无符号整数转换为网络字节序（大端序）。常用于设置端口号。
    /// </summary>
    /// <param name="hostshort">主机字节序的端口号。</param>
    /// <returns>网络字节序的端口号。</returns>
    [LibraryImport("libc", SetLastError = true, EntryPoint = "htons")]
    public static partial ushort htons(ushort hostshort);

    /// <summary>
    /// 创建一个 eventfd 文件描述符，用于线程间或进程间的事件通知。
    /// </summary>
    /// <param name="initval">计数器初始值。</param>
    /// <param name="flags">标志位，如 EFD_NONBLOCK=0x800、EFD_SEMAPHORE=1。</param>
    /// <returns>成功返回 eventfd 文件描述符，失败返回 -1。</returns>
    [LibraryImport("libc")]
    public static partial int eventfd(uint initval, int flags);

    /// <summary>
    /// 向 eventfd 文件描述符写入一个值，将其累加到内部计数器上，用于触发事件通知。
    /// </summary>
    /// <param name="fd">由 <see cref="eventfd"/> 返回的文件描述符。</param>
    /// <param name="value">要累加的值。</param>
    /// <returns>成功返回 0，失败返回 -1。</returns>
    [LibraryImport("libc")]
    public static partial int eventfd_write(int fd, ulong value);

    // ────────────────────────────────────────────────────────────────────────────
    //  系统调用
    // ────────────────────────────────────────────────────────────────────────────


    /// <summary>
    /// 将当前执行上下文保存到 ucp，必须在 MakeContext 前调用完成初始化。
    /// </summary>
    [LibraryImport("libc", EntryPoint = "getcontext")]
    public static partial int GetContext(LinuxCoroutineContext* ucp);

    /// <summary>
    /// 设置上下文的入口函数和参数，argc 为参数个数，只支持 int 类型参数。
    /// </summary>
    [LibraryImport("libc", EntryPoint = "makecontext")]
    public static partial void MakeContext(
        LinuxCoroutineContext* ucp,
        nint func,
        int argc,
        int argsHi,
        int argsLo);

    /// <summary>
    /// 保存当前上下文到 oucp，切换执行到 ucp。
    /// </summary>
    [LibraryImport("libc", EntryPoint = "swapcontext")]
    public static partial int SwapContext(
        LinuxCoroutineContext* oucp,
        LinuxCoroutineContext* ucp);

    /// <summary>
    /// 设置内存区域的访问保护属性，用于设置 Guard Page。
    /// </summary>
    [LibraryImport("libc", EntryPoint = "mprotect")]
    public static partial int MProtect(void* addr, nuint len, int prot);


    /// <summary>
    /// int uname(struct utsname *buf);
    /// <para>获取当前系统的内核名称、节点名称、发行版本、版本号以及硬件架构类型。</para>
    /// <para>注意：buf 指向的结构体大小在不同 Linux 发行版上可能略有差异（通常每个字段 65 字节）。</para>
    /// </summary>
    /// <param name="buf">指向接收系统信息的缓冲区指针（通常为 utsname 结构体）。</param>
    /// <returns>成功返回 0，失败返回 -1，并设置 errno。</returns>
    [LibraryImport("libc", EntryPoint = "uname", SetLastError = true)]
    public static partial int uname(void* buf);


    /// <summary>
    /// 执行一个 Linux 系统调用（5 参数版本）。
    /// </summary>
    /// <param name="n">系统调用号。</param>
    /// <param name="a1">第 1 个参数。</param>
    /// <param name="a2">第 2 个参数。</param>
    /// <param name="a3">第 3 个参数。</param>
    /// <param name="a4">第 4 个参数。</param>
    /// <param name="a5">第 5 个参数。</param>
    /// <returns>系统调用的返回值，失败时为负的 errno 值。</returns>
    [LibraryImport("libc")]
    public static partial long syscall(long n, long a1, long a2, long a3, long a4, long a5);

    /// <summary>
    /// 执行一个 Linux 系统调用（最多 6 参数，支持默认值）。
    /// </summary>
    /// <param name="nr">系统调用号。</param>
    /// <param name="a1">第 1 个参数，默认为 0。</param>
    /// <param name="a2">第 2 个参数，默认为 0。</param>
    /// <param name="a3">第 3 个参数，默认为 0。</param>
    /// <param name="a4">第 4 个参数，默认为 0。</param>
    /// <param name="a5">第 5 个参数，默认为 0。</param>
    /// <param name="a6">第 6 个参数，默认为 0。</param>
    /// <returns>系统调用的返回值，失败时为负的 errno 值。</returns>
    [LibraryImport("libc")]
    public static partial long syscall(long nr,
        long a1 = 0, long a2 = 0, long a3 = 0, long a4 = 0, long a5 = 0, long a6 = 0);

    /// <summary>
    /// 将错误码转换为对应的可读错误描述字符串。
    /// </summary>
    /// <param name="errnum">错误码，通常来自 errno。</param>
    /// <returns>指向错误描述字符串的指针。</returns>
    [LibraryImport("libc")]
    public static partial byte* strerror(int errnum);

    // ────────────────────────────────────────────────────────────────────────────
    //  管道 / 轮询
    // ────────────────────────────────────────────────────────────────────────────

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

    // ────────────────────────────────────────────────────────────────────────────
    //  POSIX 线程（pthread）—— 线程生命周期
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 创建一个新的线程。
    /// </summary>
    /// <param name="thread">输出参数：指向 pthread_t 的指针，用于存储新线程的 ID。</param>
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
    [LibraryImport("libc", EntryPoint = "pthread_exit")]
    public static partial void pthread_exit(void* value_ptr);

    /// <summary>
    /// 获取当前线程 ID，返回调用线程自身的 pthread_t 标识符。
    /// </summary>
    /// <returns>当前线程的 pthread_t 标识符。</returns>
    [LibraryImport("libc", EntryPoint = "pthread_self")]
    public static partial void* pthread_self();

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
    public static partial int pthread_cancel(IntPtr thread);

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
}