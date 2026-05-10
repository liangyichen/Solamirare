namespace Solamirare
{




    internal static unsafe class LinuxPointerChecker
    {
        // 使用 libc 的标准函数替换 syscall，以实现跨架构兼容性
        // O_RDONLY (只读打开) 在 Linux 上通常定义为 0
        const int O_RDONLY = 0;




        // 用于存储内存区域的起始和结束地址
        unsafe struct MemoryRegion
        {
            public void* Start;
            public void* End;
        }

        /// <summary>
        /// 栈内存区域
        /// </summary>
        [ThreadStatic]
        static MemoryRegion t_stackRegion;

        [ThreadStatic]
        static bool t_stackLimitsInitialized;


        /// <summary>
        /// 解析十六进制字符
        /// </summary>
        /// <param name="buffer">缓冲区指针</param>
        /// <param name="pos">当前解析位置的引用</param>
        /// <returns>解析出的数值</returns>
        static ulong ParseHex(byte* buffer, ref int pos)
        {
            ulong value = 0;
            while (buffer[pos] >= '0' && buffer[pos] <= '9' ||
                   buffer[pos] >= 'a' && buffer[pos] <= 'f')
            {
                if (buffer[pos] >= '0' && buffer[pos] <= '9')
                {
                    value = value * 16 + (ulong)(buffer[pos] - '0');
                }
                else
                {
                    value = value * 16 + (ulong)(buffer[pos] - 'a' + 10);
                }
                pos++;
            }
            return value;
        }



        /// <summary>
        /// 获取当前进程的栈和堆的内存范围 (已修改为使用标准libc函数)
        /// </summary>
        static void getStackAndHeapRegions(out MemoryRegion stackRegion /*, out MemoryRegion heapRegion */)
        {
            stackRegion = new MemoryRegion { Start = null, End = null };
            //heapRegion = new MemoryRegion { Start = null, End = null };

            int stackProbe;
            void* pStackProbe = &stackProbe;

            byte* path = stackalloc byte[] { (byte)'/', (byte)'p', (byte)'r', (byte)'o', (byte)'c',
                                            (byte)'/', (byte)'s', (byte)'e', (byte)'l', (byte)'f',
                                            (byte)'/', (byte)'m', (byte)'a', (byte)'p', (byte)'s', 0 };

            // --- START: MODIFIED SECTION ---

            // 3. 使用 libc 的 open 函数打开文件
            int fd = LinuxAPI.open(path, O_RDONLY);
            if (fd < 0)
            {
                // open 返回 -1 表示失败
                return;
            }

            const int bufferSize = 4096;
            byte* buffer = stackalloc byte[bufferSize];
            long bytesRead;

            // 4. 使用 libc 的 read 函数读取文件
            while ((bytesRead = LinuxAPI.read(fd, buffer, bufferSize)) > 0)
            {

                int lineStart = 0;
                for (int i = 0; i < bytesRead; i++)
                {
                    if (buffer[i] == '\n')
                    {
                        int pos = lineStart;
                        ulong start = ParseHex(buffer, ref pos);
                        pos++;
                        ulong end = ParseHex(buffer, ref pos);

                        while (pos < i && buffer[pos] != ' ') pos++;
                        while (pos < i && buffer[pos] == ' ') pos++;
                        while (pos < i && buffer[pos] != ' ') pos++;
                        while (pos < i && buffer[pos] == ' ') pos++;
                        while (pos < i && buffer[pos] != ' ') pos++;
                        while (pos < i && buffer[pos] == ' ') pos++;
                        while (pos < i && buffer[pos] != ' ') pos++;
                        while (pos < i && buffer[pos] == ' ') pos++;
                        while (pos < i && buffer[pos] != ' ') pos++;
                        while (pos < i && buffer[pos] == ' ') pos++;

                        //屏蔽堆内存检测，因为不能保证 100% 正确
                        /*
                        if (pos + 5 < i && buffer[pos] == '[' && buffer[pos + 1] == 'h' && buffer[pos + 2] == 'e' && buffer[pos + 3] == 'a' && buffer[pos + 4] == 'p' && buffer[pos + 5] == ']')
                        {
                            heapRegion.Start = (void*)start;
                            heapRegion.End = (void*)end;
                        }
                        */

                        if (pStackProbe >= (void*)start && pStackProbe < (void*)end)
                        {
                            stackRegion.Start = (void*)start;
                            stackRegion.End = (void*)end;
                        }

                        lineStart = i + 1;
                    }
                }
            }

            // 5. 使用 libc 的 close 函数关闭文件
            LinuxAPI.close(fd);

            // --- END: MODIFIED SECTION ---
        }



        /// <summary>
        /// 判断一个指针指向的内存是栈、堆还是其他区域
        /// </summary>
        /// <param name="pointer">要检查的指针</param>
        /// <returns>指针类型</returns>
        internal static MemoryType GetMemoryType(void* pointer)
        {
            if (pointer == null)
            {
                return MemoryType.Unallocated;
            }

            if (!t_stackLimitsInitialized)
            {
                getStackAndHeapRegions(out t_stackRegion);
                t_stackLimitsInitialized = true;
            }

            // 判断是否在栈区域内
            // 注意：t_stackRegion.Start 可能为 null，如果 GetStackAndHeapRegions 失败
            if (t_stackRegion.Start != null && pointer >= t_stackRegion.Start && pointer < t_stackRegion.End)
            {
                return MemoryType.Stack;
            }

            // // 判断是否在堆区域内
            // if (heapRegion.Start != null && pointer >= heapRegion.Start && pointer < heapRegion.End)
            // {
            //     return MemoryType.Heap;
            // }

            return MemoryType.Unknown;
        }


    }

}
