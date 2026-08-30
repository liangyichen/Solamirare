namespace Solamirare;

/// <summary>
/// 跨平台协程栈初始化入口。
/// <para>
/// 整个进程只需调用一次 <see cref="Initialize"/>，
/// 内部根据当前平台转发到对应的实现。
/// </para>
/// </summary>
public static class CoroutineStack
{
    /// <summary>
    /// 初始化协程栈内存，必须在任何协程创建之前调用。
    /// <para>
    /// Windows：不实际分配内存，仅记录配置参数和并发上限。
    /// Linux / macOS：一次性预分配整块栈内存，按槽切割供协程复用。
    /// </para>
    /// </summary>
    /// <param name="totalSize">
    /// 总内存预算（字节）。
    /// Linux / macOS：实际分配此大小的内存。
    /// Windows：仅用于计算最大并发数（totalSize / slotSize）。
    /// </param>
    /// <param name="slotSize">
    /// 单个协程栈大小（字节）。
    /// Linux / macOS：必须是 4096 的整数倍。
    /// Windows：直接传给 CreateFiber 的 dwStackSize。
    /// </param>
    public static void Initialize(nuint totalSize, nuint slotSize)
    {
#if PLATFORM_WINDOWS
        WindowsCoroutineStack.Initialize(totalSize, slotSize);
#elif PLATFORM_LINUX
        LinuxCoroutineStack.Initialize(totalSize, slotSize);
#elif PLATFORM_MACOS
        MacOSCoroutineStack.Initialize(totalSize, slotSize);
#endif
    }

    /// <summary>
    /// 释放协程栈内存，通常在程序退出时调用。
    /// <para>Windows 版本无实际内存需要释放，调用此方法为空操作。</para>
    /// </summary>
    public static void Dispose()
    {
#if PLATFORM_WINDOWS
        // Windows 栈由 Win32 按需分配和释放，无需操作
#elif PLATFORM_LINUX
        LinuxCoroutineStack.Dispose();
#elif PLATFORM_MACOS
        MacOSCoroutineStack.Dispose();
#endif
    }
}