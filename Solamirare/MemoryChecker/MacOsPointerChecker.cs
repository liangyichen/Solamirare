namespace Solamirare;






/// <summary>
/// A class to determine the memory type (Stack, Heap, Unknown) of a pointer on macOS.
/// It uses P/Invoke to call native macOS APIs (pthread and Mach).
/// </summary>
internal static class MacOsPointerChecker
{
 

    [ThreadStatic]
    static ulong t_stackLowLimit;
    [ThreadStatic]
    static ulong t_stackHighLimit;
    [ThreadStatic]
    static bool t_stackLimitsInitialized;

    /// <summary>
    /// 检测指针指向的内存是否存在于栈上
    /// </summary>
    /// <param name="pointer"></param>
    /// <returns></returns>
    internal static unsafe MemoryType GetMemoryType(void* pointer)
    {
        if (pointer is null)
        {
            return MemoryType.Unallocated;
        }

        if (!t_stackLimitsInitialized)
        {
            IntPtr currentThread = MacOSAPI.pthread_self();
            ulong stackSize = MacOSAPI.pthread_get_stacksize_np(currentThread);
            IntPtr stackBase = MacOSAPI.pthread_get_stackaddr_np(currentThread);

            // macOS 栈向下增长，stackBase 是高地址
            t_stackHighLimit = (ulong)stackBase;
            t_stackLowLimit = t_stackHighLimit - stackSize;

            t_stackLimitsInitialized = true;
        }

        ulong ptrAddress = (ulong)pointer;

        if (ptrAddress >= t_stackLowLimit && ptrAddress < t_stackHighLimit)
        {
            return MemoryType.Stack;
        }


        return MemoryType.Unknown;
    }
}