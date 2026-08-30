
namespace Solamirare
{


    /// <summary>
    /// 内存类型检测
    /// </summary>
    public static unsafe class MemoryTypeChecker
    {
        /// <summary>
        /// 检测指针指向的内存是否存在于栈上
        /// </summary>
        /// <param name="pointer"></param>
        /// <returns></returns>
        public static bool OnStack(void* pointer)
        {
            MemoryType memoryType;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                memoryType = WindowsPointerChecker.GetMemoryType(pointer);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                memoryType = LinuxPointerChecker.GetMemoryType(pointer);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                memoryType = MacOsPointerChecker.GetMemoryType(pointer);
            }
            else //UNIX or Others
            {
                memoryType = LinuxPointerChecker.GetMemoryType(pointer);
            }


            return memoryType is MemoryType.Stack;

        }
    }

}
