namespace Solamirare;

/// <summary>
/// 跨平台协程调度器管理。
/// <para>
/// Create / Destroy 用于管理调度器生命周期。
/// Resume / Yield 保留原有签名供需要显式传入 scheduler 的场景使用，
/// 日常使用推荐改用 <see cref="Coroutine.Resume"/> 和 <see cref="Coroutine.Yield"/>。
/// </para>
/// </summary>
public static unsafe class CoroutineScheduler
{
    /// <summary>
    /// 在非托管堆上创建调度器实例。
    /// <para>Windows：同时将当前线程转换为 Fiber，对调用方透明。</para>
    /// </summary>
    /// <returns>成功返回调度器指针，失败返回 null。</returns>
    public static void* Create()
    {
#if PLATFORM_WINDOWS
        return WindowsCoroutineScheduler.Create();
#elif PLATFORM_LINUX
        return LinuxCoroutineScheduler.Create();
#elif PLATFORM_MACOS
        return MacOSCoroutineScheduler.Create();
#else
        return null;
#endif
    }

    /// <summary>
    /// 释放调度器实例。
    /// 调用前必须确保此调度器上没有正在执行的协程。
    /// </summary>
    /// <param name="scheduler">由 <see cref="Create"/> 返回的调度器指针。</param>
    public static void Destroy(void* scheduler)
    {
        if (scheduler == null) return;
#if PLATFORM_WINDOWS
        WindowsCoroutineScheduler.Destroy(
            (WindowsCoroutineScheduler*)scheduler);
#elif PLATFORM_LINUX
        LinuxCoroutineScheduler.Destroy(
            (LinuxCoroutineScheduler*)scheduler);
#elif PLATFORM_MACOS
        MacOSCoroutineScheduler.Destroy(
            (MacOSCoroutineScheduler*)scheduler);
#endif
    }

    /// <summary>
    /// 获取当前正在执行的协程指针，不在协程内部时返回 null。
    /// </summary>
    public static void* GetCurrent(void* scheduler)
    {
        if (scheduler == null) return null;
#if PLATFORM_WINDOWS
        return ((WindowsCoroutineScheduler*)scheduler)->Current;
#elif PLATFORM_LINUX
        return ((LinuxCoroutineScheduler*)scheduler)->Current;
#elif PLATFORM_MACOS
        return ((MacOSCoroutineScheduler*)scheduler)->Current;
#else
        return null;
#endif
    }
}