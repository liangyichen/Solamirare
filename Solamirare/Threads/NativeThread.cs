namespace Solamirare;


    /// <summary>
    /// 用于在 POSIX 平台上打包 worker 函数指针和参数的上下文结构体。
    /// <para>
    /// 必须由调用方在栈上（或固定内存中）分配，并保证其生命周期覆盖线程启动完成前的窗口期。
    /// 推荐做法：Create 返回后立即调用 Join，或使用信号量确认线程已读取完毕再释放。
    /// </para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ThreadStartInfo
    {
        //维护提示：禁止修改字段顺序

        /// <summary>符合统一约定的线程函数指针（Windows 风格：void* -> uint）</summary>
        public delegate* unmanaged<void*, uint> Worker;
        /// <summary>透传给 Worker 的参数指针</summary>
        public void* Arg;
    }


/// <summary>
/// 跨平台原生线程封装 (Zero GC)
/// <para>自动适配 Windows (CreateThread) 和 POSIX (pthread) 的差异。</para>
/// <para>
/// 统一约定：线程函数签名为 <c>delegate* unmanaged&lt;void*, uint&gt;</c>（Windows 风格）。
/// POSIX 平台通过内部适配器将 uint 退出码装入 void* 返回，对调用方完全透明。
/// </para>
/// </summary>
public static unsafe class NativeThread
{
    /// <summary>
    /// 无限等待超时时间常量。
    /// </summary>
    public const uint INFINITE = 0xFFFFFFFF;




    /// <summary>
    /// 跨平台创建线程。内部自动识别平台并调用对应的原生 API。
    /// </summary>
    /// <param name="handle">输出：线程句柄 (Windows) 或线程 ID (POSIX)</param>
    /// <param name="startInfo">
    /// 指向 <see cref="ThreadStartInfo"/> 的指针。
    /// Windows 平台直接透传 startInfo->Arg 给 Worker；
    /// POSIX 平台将整个 startInfo 指针传入适配器 <see cref="PosixEntry"/>。
    /// 调用方须保证该指针在线程启动完成前保持有效。
    /// </param>
    /// <returns>是否成功</returns>
    public static bool Create(out void* handle, ThreadStartInfo* startInfo)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows 路径：签名天然匹配，直接透传，无需适配器
            handle = WindowsAPI.CreateThread(
                null, 0,
                startInfo->Worker,  // delegate* unmanaged<void*, uint> 直接符合 LPTHREAD_START_ROUTINE
                startInfo->Arg,
                0, out _);
            return handle != null;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // POSIX 路径：用 PosixEntry 适配器桥接签名差异
            // 把 startInfo 指针整体作为 arg 传给适配器
            void* threadId;
            int ret = LinuxAPI.pthread_create(&threadId, null, &PosixEntry, startInfo);
            handle = (ret == 0) ? threadId : null;
            return ret == 0;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            void* threadId;
            int ret = MacOSAPI.pthread_create(&threadId, null, &PosixEntry, startInfo);
            handle = (ret == 0) ? threadId : null;
            return ret == 0;
        }

        handle = null;
        return false;
    }

    // =========================================================================
    // POSIX 适配器
    // 将统一签名 (void* -> uint) 适配为 pthread 要求的 (void* -> void*)
    // =========================================================================

    /// <summary>
    /// POSIX 平台的非托管入口适配器。
    /// <para>
    /// 从 <see cref="ThreadStartInfo"/> 中取出 Worker 和 Arg，
    /// 调用后将 uint 退出码装入 void* 返回，符合 pthread ABI。
    /// 全程无任何托管分配。
    /// </para>
    /// </summary>
    [UnmanagedCallersOnly]
    private static void* PosixEntry(void* ctxPtr)
    {
        ThreadStartInfo* info = (ThreadStartInfo*)ctxPtr;
        uint exitCode = info->Worker(info->Arg);
        // uint 装入 void*：高 32 位为 0，低 32 位存退出码
        // pthread_join 通过 retval 参数取回此值
        return (void*)(nuint)exitCode;
    }

    // =========================================================================
    // 平台感知 API（保留，Zero GC 快速路径）
    // 调用方已知平台时可直接传递原生签名的函数指针，无需 ThreadStartInfo 包装
    // =========================================================================

    /// <summary>
    /// 创建线程 (POSIX 风格 Worker，Zero GC)
    /// <para>直接接受符合 POSIX ABI 的函数指针 (void* -> void*)，适用于 Linux 和 macOS。</para>
    /// </summary>
    internal static bool CreateOnPosix(out void* handle, delegate* unmanaged<void*, void*> worker, void* arg)
    {
        handle = null;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            fixed (void** h = &handle)
                return LinuxAPI.pthread_create(h, null, worker, arg) == 0;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            fixed (void** h = &handle)
                return MacOSAPI.pthread_create(h, null, worker, arg) == 0;
        }

        return false;
    }

    /// <summary>
    /// 创建线程 (Windows 风格 Worker，Zero GC)
    /// <para>直接接受符合 Windows ABI 的函数指针 (void* -> uint)，适用于 Windows。</para>
    /// </summary>
    internal static bool CreateOnWindows(out void* handle, delegate* unmanaged<void*, uint> worker, void* arg)
    {
        handle = null;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            handle = WindowsAPI.CreateThread(null, 0, worker, arg, 0, out _);
            return handle != null;
        }

        return false;
    }

    // =========================================================================
    // 其余跨平台 API
    // =========================================================================

    /// <summary>
    /// 等待线程结束并清理资源
    /// </summary>
    /// <param name="handle">线程句柄 (Windows) 或线程 ID (POSIX)</param>
    public static void Join(void* handle)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsAPI.WaitForSingleObject(handle, INFINITE);
            WindowsAPI.CloseHandle(handle);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            LinuxAPI.pthread_join(handle, null);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            MacOSAPI.pthread_join(handle, null);
        }
    }

    /// <summary>
    /// 获取当前线程 ID
    /// </summary>
    public static ulong GetCurrentThreadId()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return WindowsAPI.GetCurrentThreadId();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return (ulong)LinuxAPI.pthread_self();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return (ulong)MacOSAPI.pthread_self();

        return 0;
    }

    /// <summary>
    /// 强制退出当前线程
    /// </summary>
    /// <param name="exitCode">退出码（POSIX 平台装入 void* 传递给 pthread_exit）</param>
    public static void Exit(uint exitCode = 0)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsAPI.ExitThread(exitCode);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            LinuxAPI.pthread_exit((void*)(nuint)exitCode);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            MacOSAPI.pthread_exit((void*)(nuint)exitCode);
        }
    }
}
