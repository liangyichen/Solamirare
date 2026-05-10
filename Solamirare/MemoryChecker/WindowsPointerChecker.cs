namespace Solamirare
{

    internal static class WindowsPointerChecker
    {


        [ThreadStatic]
        static IntPtr t_stackLowLimit;
        [ThreadStatic]
        static IntPtr t_stackHighLimit;
        [ThreadStatic]
        static bool t_stackLimitsInitialized;



        // Memory states from VirtualQueryEx
        const uint MEM_COMMIT = 0x1000;

        // Memory types
        const uint MEM_PRIVATE = 0x20000;

        const uint MEM_IMAGE = 0x1000000;


        /// <summary>
        /// 确定指针指向的内存的可能位置。
        /// 此版本可在 x86, x64, 和 Arm64 架构上正确工作。
        /// </summary>
        /// <param name="pointer">要检查的指针。</param>
        /// <returns>一个指示内存位置的枚举。</returns>
        internal static unsafe MemoryType GetMemoryType(void* pointer)
        {

            if (pointer is null)
            {
                return MemoryType.Unallocated;
            }

            nint ptr = (nint)pointer;

            if (!t_stackLimitsInitialized)
            {
                // 1. 使用官方 API 获取当前线程的栈边界 (仅在当前线程首次调用时执行)
                WindowsAPI.GetCurrentThreadStackLimits(out t_stackLowLimit, out t_stackHighLimit);
                t_stackLimitsInitialized = true;
            }

            // 2. 检查指针是否在栈边界内
            // 注意：栈是向下增长的，所以 stackBase > stackLimit

            if ((ulong)ptr.ToInt64() <= (ulong)t_stackHighLimit.ToInt64() &&
                (ulong)ptr.ToInt64() >= (ulong)t_stackLowLimit.ToInt64())
            {
                return MemoryType.Stack;
            }


            // 4. 如果无法确定，则返回 Unknown
            return MemoryType.Unknown;
        }
    }

}
