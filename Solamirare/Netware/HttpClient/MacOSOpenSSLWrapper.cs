namespace Solamirare;

/// <summary>
/// 提供 macOS 平台下的 OpenSSL 动态绑定封装。
/// </summary>
public unsafe static class MacOSOpenSSLWrapper
{
    private static IntPtr sslLibHandle = IntPtr.Zero;
    private static IntPtr cryptoLibHandle = IntPtr.Zero;

    /// <summary>指示当前运行环境是否支持 HTTPS 功能。</summary>
    public static readonly bool SupportsHttps = false;

    private delegate IntPtr TLS_client_method_delegate();
    private delegate IntPtr SSL_CTX_new_delegate(IntPtr method);
    private delegate void SSL_CTX_free_delegate(IntPtr ctx);
    private delegate IntPtr SSL_new_delegate(IntPtr ctx);
    private delegate void SSL_free_delegate(IntPtr ssl);
    private delegate int SSL_set_fd_delegate(IntPtr ssl, int fd);
    private delegate int SSL_connect_delegate(IntPtr ssl);
    private delegate int SSL_write_delegate(IntPtr ssl, byte* buf, UIntPtr num);
    private delegate int SSL_read_delegate(IntPtr ssl, byte* buf, long num);
    private delegate int SSL_shutdown_delegate(IntPtr ssl);
    private delegate int SSL_get_error_delegate(IntPtr ssl, int ret);

    private static TLS_client_method_delegate TLS_client_method_func;
    private static SSL_CTX_new_delegate SSL_CTX_new_func;
    private static SSL_CTX_free_delegate SSL_CTX_free_func;
    private static SSL_new_delegate SSL_new_func;
    private static SSL_free_delegate SSL_free_func;
    private static SSL_set_fd_delegate SSL_set_fd_func;
    private static SSL_connect_delegate SSL_connect_func;
    private static SSL_write_delegate SSL_write_func;
    private static SSL_read_delegate SSL_read_func;
    private static SSL_shutdown_delegate SSL_shutdown_func;
    private static SSL_get_error_delegate SSL_get_error_func;

    private const int RTLD_LAZY = 1;

    static UnManagedMemory<UnManagedMemory<byte>> sslPaths;
    static UnManagedMemory<UnManagedMemory<byte>> cryptoPaths;

    static MacOSOpenSSLWrapper()
    {
        UnManagedMemory<UnManagedMemory<byte>> sslPaths = new UnManagedMemory<UnManagedMemory<byte>>(7, 0);
        sslPaths.Add("/opt/homebrew/opt/openssl@3/lib/libssl.3.dylib\0"u8);
        sslPaths.Add("/opt/homebrew/opt/openssl@1.1/lib/libssl.1.1.dylib\0"u8);
        sslPaths.Add("/usr/local/opt/openssl@3/lib/libssl.3.dylib\0"u8);
        sslPaths.Add("/usr/local/opt/openssl@1.1/lib/libssl.1.1.dylib\0"u8);
        sslPaths.Add("libssl.3.dylib\0"u8);
        sslPaths.Add("libssl.1.1.dylib\0"u8);
        sslPaths.Add("libssl.dylib\0"u8);

        UnManagedMemory<UnManagedMemory<byte>> cryptoPaths = new UnManagedMemory<UnManagedMemory<byte>>();
        cryptoPaths.Add("/opt/homebrew/opt/openssl@3/lib/libcrypto.3.dylib\0"u8);
        cryptoPaths.Add("/opt/homebrew/opt/openssl@1.1/lib/libcrypto.1.1.dylib\0"u8);
        cryptoPaths.Add("/usr/local/opt/openssl@3/lib/libcrypto.3.dylib\0"u8);
        cryptoPaths.Add("/usr/local/opt/openssl@1.1/lib/libcrypto.1.1.dylib\0"u8);
        cryptoPaths.Add("libcrypto.3.dylib\0"u8);
        cryptoPaths.Add("libcrypto.1.1.dylib\0"u8);
        cryptoPaths.Add("libcrypto.dylib\0"u8);

        foreach (UnManagedMemory<byte>* path in cryptoPaths)
        {
            byte* pPath = path->Pointer;

            cryptoLibHandle = MacOSAPI.dlopen(pPath, RTLD_LAZY);
            if (cryptoLibHandle != IntPtr.Zero)
            {
                path->Dispose();
                break;
            }

            path->Dispose();
        }

        foreach (UnManagedMemory<byte>* path in sslPaths)
        {
            byte* pPath = path->Pointer;

            sslLibHandle = MacOSAPI.dlopen(pPath, RTLD_LAZY);
            if (sslLibHandle != IntPtr.Zero)
            {
                path->Dispose();
                break;
            }

            path->Dispose();
        }

        sslPaths.Dispose();
        cryptoPaths.Dispose();

        if (sslLibHandle == IntPtr.Zero)
            return;

        try
        {
            TLS_client_method_func = LoadFunction<TLS_client_method_delegate>("TLS_client_method"u8);
            SSL_CTX_new_func = LoadFunction<SSL_CTX_new_delegate>("SSL_CTX_new"u8);
            SSL_CTX_free_func = LoadFunction<SSL_CTX_free_delegate>("SSL_CTX_free"u8);
            SSL_new_func = LoadFunction<SSL_new_delegate>("SSL_new"u8);
            SSL_free_func = LoadFunction<SSL_free_delegate>("SSL_free"u8);
            SSL_set_fd_func = LoadFunction<SSL_set_fd_delegate>("SSL_set_fd"u8);
            SSL_connect_func = LoadFunction<SSL_connect_delegate>("SSL_connect"u8);
            SSL_write_func = LoadFunction<SSL_write_delegate>("SSL_write"u8);
            SSL_read_func = LoadFunction<SSL_read_delegate>("SSL_read"u8);
            SSL_shutdown_func = LoadFunction<SSL_shutdown_delegate>("SSL_shutdown"u8);
            SSL_get_error_func = LoadFunction<SSL_get_error_delegate>("SSL_get_error"u8);

            SupportsHttps = true;
        }
        catch
        {
        }
    }

    private static T LoadFunction<T>(ReadOnlySpan<byte> name) where T : Delegate
    {
        fixed (byte* pName = name)
        {
            IntPtr funcPtr = MacOSAPI.dlsym(sslLibHandle, pName);
            if (funcPtr == IntPtr.Zero)
                throw new Exception();
            return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
        }
    }

    /// <summary>
    /// 获取 TLS 客户端方法对象。
    /// </summary>
    public static IntPtr TLS_client_method() => TLS_client_method_func();

    /// <summary>
    /// 创建 SSL 上下文。
    /// </summary>
    /// <param name="method">TLS 方法对象。</param>
    public static IntPtr SSL_CTX_new(IntPtr method) => SSL_CTX_new_func(method);

    /// <summary>
    /// 释放 SSL 上下文。
    /// </summary>
    /// <param name="ctx">待释放的上下文句柄。</param>
    public static void SSL_CTX_free(IntPtr ctx) => SSL_CTX_free_func(ctx);

    /// <summary>
    /// 基于指定上下文创建 SSL 连接对象。
    /// </summary>
    /// <param name="ctx">SSL 上下文句柄。</param>
    public static IntPtr SSL_new(IntPtr ctx) => SSL_new_func(ctx);

    /// <summary>
    /// 释放 SSL 连接对象。
    /// </summary>
    /// <param name="ssl">待释放的 SSL 句柄。</param>
    public static void SSL_free(IntPtr ssl) => SSL_free_func(ssl);

    /// <summary>
    /// 将套接字绑定到 SSL 连接对象。
    /// </summary>
    /// <param name="ssl">SSL 句柄。</param>
    /// <param name="fd">套接字描述符。</param>
    public static int SSL_set_fd(IntPtr ssl, int fd) => SSL_set_fd_func(ssl, fd);

    /// <summary>
    /// 发起 SSL 握手。
    /// </summary>
    /// <param name="ssl">SSL 句柄。</param>
    public static int SSL_connect(IntPtr ssl) => SSL_connect_func(ssl);

    /// <summary>
    /// 通过 SSL 连接发送数据。
    /// </summary>
    /// <param name="ssl">SSL 句柄。</param>
    /// <param name="buf">待发送数据缓冲区。</param>
    /// <param name="num">待发送字节数。</param>
    public static int SSL_write(IntPtr ssl, byte* buf, UIntPtr num) => SSL_write_func(ssl, buf, num);

    /// <summary>
    /// 通过 SSL 连接读取数据。
    /// </summary>
    /// <param name="ssl">SSL 句柄。</param>
    /// <param name="buf">接收数据缓冲区。</param>
    /// <param name="num">缓冲区可接收的最大字节数。</param>
    public static int SSL_read(IntPtr ssl, byte* buf, long num) => SSL_read_func(ssl, buf, num);

    /// <summary>
    /// 关闭 SSL 连接。
    /// </summary>
    /// <param name="ssl">SSL 句柄。</param>
    public static int SSL_shutdown(IntPtr ssl) => SSL_shutdown_func(ssl);

    /// <summary>
    /// 获取最近一次 SSL 操作的错误代码。
    /// </summary>
    /// <param name="ssl">SSL 句柄。</param>
    /// <param name="ret">SSL 函数（如 SSL_connect, SSL_read, SSL_write）的返回值。</param>
    public static int SSL_get_error(IntPtr ssl, int ret) => SSL_get_error_func(ssl, ret);
}
