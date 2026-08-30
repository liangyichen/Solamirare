namespace Solamirare;

using System;
using System.Runtime.InteropServices;

/// <summary>
/// 跨平台异步 HTTP 客户端包装器（零 GC、回调）。
/// 
/// 自动选择与当前操作系统相适应的异步模型：
/// - macOS: Kqueue + GCD
/// - Windows: IOCP + 工作线程
/// - Linux: IO_URing + 后台收割线程
/// 
/// 提供统一的同步API，使用者无需关心底层平台差异。
/// </summary>
public unsafe struct AsyncHttpClient
{
    private PlatformKind _platform;
    private void* _impl;  // 指向实际的平台实现

    private enum PlatformKind : byte
    {
        None = 0,
        macOs = 1,
        Windows = 2,
        Linux = 3,
    }

    /// <summary>
    /// 初始化跨平台异步 HTTP 客户端。
    /// 根据当前操作系统自动选择合适的底层实现。
    /// </summary>
    /// <param name="timeoutSeconds">请求超时时间（秒）。</param>
    /// <param name="retries">重试次数。</param>
    public void Initialize(int timeoutSeconds = 10, int retries = 3)
    {
        DetectPlatform();

        switch (_platform)
        {
            case PlatformKind.macOs:
                {
                    AsyncMacOSHttpClient* impl = (AsyncMacOSHttpClient*)Marshal.AllocHGlobal(sizeof(AsyncMacOSHttpClient));
                    *impl = default;
                    impl->Initialize(timeoutSeconds);
                    _impl = impl;
                }
                break;

            case PlatformKind.Windows:
                {
                    AsyncWindowsHttpClient* impl = (AsyncWindowsHttpClient*)Marshal.AllocHGlobal(sizeof(AsyncWindowsHttpClient));
                    *impl = default;
                    impl->Initialize(timeoutSeconds, retries);
                    _impl = impl;
                }
                break;

            case PlatformKind.Linux:
                {
                    AsyncLinuxHttpClient* impl = (AsyncLinuxHttpClient*)Marshal.AllocHGlobal(sizeof(AsyncLinuxHttpClient));
                    *impl = default;
                    impl->Initialize();
                    _impl = impl;
                }
                break;

            default:
                throw new PlatformNotSupportedException($"平台 {RuntimeInformation.OSDescription} 暂不支持");
        }
    }

    /// <summary>
    /// 发起一个异步 POST 请求，立即返回不阻塞调用线程。
    /// 请求完成后在后台线程调用回调函数。
    /// </summary>
    public void RequestPOSTAsync(
        UnManagedCollection<char> url,
        UnManagedMemory<byte>* responseBuffer,
        UnManagedString* body,
        HttpContentType contentType,
        delegate* unmanaged<void*, void> callback)
    {
        if (_impl == null) throw new InvalidOperationException("AsyncHttpClient 未初始化，请先调用 Initialize()");

        // 仅在 macOS 平台上实现 POST
        if (_platform == PlatformKind.macOs)
        {
            ((AsyncMacOSHttpClient*)_impl)->RequestPOSTAsync(url, responseBuffer, body, contentType, callback);
        }
        else if (_platform == PlatformKind.Windows)
        {
            ((AsyncWindowsHttpClient*)_impl)->RequestPOSTAsync(url, responseBuffer, body, contentType, callback);
        }
        else if (_platform == PlatformKind.Linux)
        {
            ((AsyncLinuxHttpClient*)_impl)->RequestPOSTAsync(url, responseBuffer, body, contentType, callback);
        }
        else
        {
            throw new PlatformNotSupportedException($"POST 请求在当前平台 ({_platform}) 暂不支持");
        }
    }

    /// <summary>
    /// 发起一个异步 GET 请求，立即返回不阻塞调用线程。
    /// 请求完成后在后台线程调用回调函数。
    /// </summary>
    /// <param name="url">请求 URL，格式：http://host[:port]/path。</param>
    /// <param name="responseBuffer">接收响应数据的缓冲区。</param>
    /// <param name="callback">请求完成后的回调，参数为请求上下文指针。</param>
    public void RequestGETAsync(
        UnManagedCollection<char> url,
        UnManagedMemory<byte>* responseBuffer,
        delegate* unmanaged<void*, void> callback)
    {
        if (_impl == null)
            throw new InvalidOperationException("AsyncHttpClient 未初始化，请先调用 Initialize()");

        switch (_platform)
        {
            case PlatformKind.macOs:
                {
                    AsyncMacOSHttpClient* impl = (AsyncMacOSHttpClient*)_impl;
                    impl->RequestGETAsync(url, responseBuffer, callback);
                }
                break;

            case PlatformKind.Windows:
                {
                    AsyncWindowsHttpClient* impl = (AsyncWindowsHttpClient*)_impl;
                    impl->RequestGETAsync(url, responseBuffer, callback);
                }
                break;

            case PlatformKind.Linux:
                {
                    AsyncLinuxHttpClient* impl = (AsyncLinuxHttpClient*)_impl;
                    impl->RequestGETAsync(url, responseBuffer, callback);
                }
                break;

            default:
                throw new PlatformNotSupportedException("未知平台");
        }
    }

    /// <summary>
    /// 释放 HTTP 客户端资源。
    /// </summary>
    public void Dispose()
    {
        if (_impl == null) return;

        switch (_platform)
        {
            case PlatformKind.macOs:
                {
                    AsyncMacOSHttpClient* impl = (AsyncMacOSHttpClient*)_impl;
                    impl->Dispose();
                }
                break;

            case PlatformKind.Windows:
                {
                    AsyncWindowsHttpClient* impl = (AsyncWindowsHttpClient*)_impl;
                    impl->Dispose();
                }
                break;

            case PlatformKind.Linux:
                {
                    AsyncLinuxHttpClient* impl = (AsyncLinuxHttpClient*)_impl;
                    impl->Dispose();
                }
                break;
        }

        if (_impl != null)
        {
            Marshal.FreeHGlobal((IntPtr)_impl);
            _impl = null;
        }
    }

    /// <summary>
    /// 检测当前操作系统并设置 _platform。
    /// </summary>
    private void DetectPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _platform = PlatformKind.macOs;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _platform = PlatformKind.Windows;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            _platform = PlatformKind.Linux;
        }
        else
        {
            _platform = PlatformKind.None;
            throw new PlatformNotSupportedException($"不支持的平台：{RuntimeInformation.OSDescription}");
        }
    }


}
