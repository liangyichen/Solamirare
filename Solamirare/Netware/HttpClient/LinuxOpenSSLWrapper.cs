namespace Solamirare;

/// <summary>
/// 提供 Linux 平台下的 OpenSSL 动态绑定封装。
/// </summary>
public static unsafe class LinuxOpenSSLWrapper
{
    private static IntPtr libHandle = IntPtr.Zero;
    /// <summary>指示当前运行环境是否支持 HTTPS 功能。</summary>
    public static bool SupportsHttps { get; }

    private delegate IntPtr d_TLS_client_method();
    private static d_TLS_client_method f_TLS_client_method;

    private delegate IntPtr d_SSL_CTX_new(IntPtr method);
    private static d_SSL_CTX_new f_SSL_CTX_new;

    private delegate void d_SSL_CTX_free(IntPtr ctx);
    private static d_SSL_CTX_free f_SSL_CTX_free;

    private delegate IntPtr d_SSL_new(IntPtr ctx);
    private static d_SSL_new f_SSL_new;

    private delegate void d_SSL_free(IntPtr ssl);
    private static d_SSL_free f_SSL_free;

    private delegate int d_SSL_set_fd(IntPtr ssl, int fd);
    private static d_SSL_set_fd f_SSL_set_fd;

    private delegate int d_SSL_connect(IntPtr ssl);
    private static d_SSL_connect f_SSL_connect;

    private delegate int d_SSL_shutdown(IntPtr ssl);
    private static d_SSL_shutdown f_SSL_shutdown;

    private delegate int d_SSL_write(IntPtr ssl, byte* buf, UIntPtr num);
    private static d_SSL_write f_SSL_write;

    private delegate int d_SSL_read(IntPtr ssl, byte* buf, int num);
    private static d_SSL_read f_SSL_read;

    private delegate int d_SSL_get_error(IntPtr ssl, int ret);
    private static d_SSL_get_error f_SSL_get_error;

    static LinuxOpenSSLWrapper()
    {
        string[] candidates = new[] { "libssl.so.3", "libssl.so.1.1", "libssl.so" };
        foreach (var name in candidates)
        {
            if (NativeLibrary.TryLoad(name, out libHandle))
                break;
        }

        if (libHandle == IntPtr.Zero)
        {
            SupportsHttps = false;
            return;
        }

        try
        {
            f_TLS_client_method = GetDelegate<d_TLS_client_method>("TLS_client_method");
            f_SSL_CTX_new = GetDelegate<d_SSL_CTX_new>("SSL_CTX_new");
            f_SSL_CTX_free = GetDelegate<d_SSL_CTX_free>("SSL_CTX_free");
            f_SSL_new = GetDelegate<d_SSL_new>("SSL_new");
            f_SSL_free = GetDelegate<d_SSL_free>("SSL_free");
            f_SSL_set_fd = GetDelegate<d_SSL_set_fd>("SSL_set_fd");
            f_SSL_connect = GetDelegate<d_SSL_connect>("SSL_connect");
            f_SSL_shutdown = GetDelegate<d_SSL_shutdown>("SSL_shutdown");
            f_SSL_write = GetDelegate<d_SSL_write>("SSL_write");
            f_SSL_read = GetDelegate<d_SSL_read>("SSL_read");
            f_SSL_get_error = GetDelegate<d_SSL_get_error>("SSL_get_error");

            SupportsHttps = f_TLS_client_method != null;
        }
        catch
        {
            SupportsHttps = false;
        }
    }

    private static T GetDelegate<T>(string name) where T : Delegate
    {
        if (libHandle == IntPtr.Zero) return null;
        if (!NativeLibrary.TryGetExport(libHandle, name, out IntPtr ptr)) return null;
        return Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }

    /// <summary>
    /// 获取 TLS 客户端方法对象。
    /// </summary>
    public static IntPtr TLS_client_method()
    {
        if (f_TLS_client_method == null) throw new PlatformNotSupportedException("OpenSSL not available");
        return f_TLS_client_method();
    }

    /// <summary>
    /// 创建 SSL 上下文。
    /// </summary>
    /// <param name="method">TLS 方法对象。</param>
    public static IntPtr SSL_CTX_new(IntPtr method)
    {
        if (f_SSL_CTX_new == null) throw new PlatformNotSupportedException("OpenSSL not available");
        return f_SSL_CTX_new(method);
    }

    /// <summary>
    /// 释放 SSL 上下文。
    /// </summary>
    /// <param name="ctx">待释放的上下文句柄。</param>
    public static void SSL_CTX_free(IntPtr ctx)
    {
        if (f_SSL_CTX_free == null) return;
        f_SSL_CTX_free(ctx);
    }

    /// <summary>
    /// 基于指定上下文创建 SSL 连接对象。
    /// </summary>
    /// <param name="ctx">SSL 上下文句柄。</param>
    public static IntPtr SSL_new(IntPtr ctx)
    {
        if (f_SSL_new == null) throw new PlatformNotSupportedException("OpenSSL not available");
        return f_SSL_new(ctx);
    }

    /// <summary>
    /// 释放 SSL 连接对象。
    /// </summary>
    /// <param name="ssl">待释放的 SSL 句柄。</param>
    public static void SSL_free(IntPtr ssl)
    {
        if (f_SSL_free == null) return;
        f_SSL_free(ssl);
    }

    /// <summary>
    /// 将套接字绑定到 SSL 连接对象。
    /// </summary>
    /// <param name="ssl">SSL 句柄。</param>
    /// <param name="fd">套接字描述符。</param>
    public static int SSL_set_fd(IntPtr ssl, int fd)
    {
        if (f_SSL_set_fd == null) throw new PlatformNotSupportedException("OpenSSL not available");
        return f_SSL_set_fd(ssl, fd);
    }

    /// <summary>
    /// 发起 SSL 握手。
    /// </summary>
    /// <param name="ssl">SSL 句柄。</param>
    public static int SSL_connect(IntPtr ssl)
    {
        if (f_SSL_connect == null) throw new PlatformNotSupportedException("OpenSSL not available");
        return f_SSL_connect(ssl);
    }

    /// <summary>
    /// 关闭 SSL 连接。
    /// </summary>
    /// <param name="ssl">SSL 句柄。</param>
    public static int SSL_shutdown(IntPtr ssl)
    {
        if (f_SSL_shutdown == null) return 0;
        return f_SSL_shutdown(ssl);
    }

    /// <summary>
    /// 通过 SSL 连接发送数据。
    /// </summary>
    /// <param name="ssl">SSL 句柄。</param>
    /// <param name="buf">待发送数据缓冲区。</param>
    /// <param name="num">待发送字节数。</param>
    public static int SSL_write(IntPtr ssl, byte* buf, UIntPtr num)
    {
        if (f_SSL_write == null) throw new PlatformNotSupportedException("OpenSSL not available");
        return f_SSL_write(ssl, buf, num);
    }

    /// <summary>
    /// 通过 SSL 连接读取数据。
    /// </summary>
    /// <param name="ssl">SSL 句柄。</param>
    /// <param name="buf">接收数据缓冲区。</param>
    /// <param name="num">缓冲区可接收的最大字节数。</param>
    public static int SSL_read(IntPtr ssl, byte* buf, int num)
    {
        if (f_SSL_read == null) throw new PlatformNotSupportedException("OpenSSL not available");
        return f_SSL_read(ssl, buf, num);
    }

    /// <summary>
    /// 获取 SSL 错误码。
    /// </summary>
    /// <param name="ssl">SSL 句柄。</param>
    /// <param name="ret">SSL 函数返回值。</param>
    public static int SSL_get_error(IntPtr ssl, int ret)
    {
        if (f_SSL_get_error == null) throw new PlatformNotSupportedException("OpenSSL not available");
        return f_SSL_get_error(ssl, ret);
    }
}
