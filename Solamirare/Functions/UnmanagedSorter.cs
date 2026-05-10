using System.Runtime.CompilerServices;

namespace Solamirare;

/// <summary>
/// 排序功能
/// </summary>
public static unsafe class UnmanagedMemorySorter
{

    private static void QuickSort(void* basePtr, long numElements, long elementSize, delegate*<nint, nint, int> comparer)
    {
        if (numElements <= 1) return;

        long stackDepth = 0;
        const int MaxDepth = 64;

        // 使用 stackalloc 模拟递归栈，避免递归和托管堆分配
        // 存储待排序子数组的起始和结束索引
        long* stackLow = stackalloc long[MaxDepth];
        long* stackHigh = stackalloc long[MaxDepth];

        stackLow[0] = 0;
        stackHigh[0] = numElements - 1;
        stackDepth = 1;

        while (stackDepth > 0)
        {
            stackDepth--;
            long low = stackLow[stackDepth];
            long high = stackHigh[stackDepth];

            if (low < high)
            {
                // 分区操作（Partition）
                void* pivotPtr = (byte*)basePtr + (high * elementSize);
                long i = low - 1;

                for (long j = low; j < high; j++)
                {
                    void* currentPtr = (byte*)basePtr + (j * elementSize);

                    // 使用传入的比较函数指针进行比较
                    if (comparer((nint)currentPtr, (nint)pivotPtr) <= 0)
                    {
                        i++;
                        void* swapPtr = (byte*)basePtr + (i * elementSize);
                        Swap(swapPtr, currentPtr, (nuint)elementSize);
                    }
                }

                i++;
                void* pivotPosPtr = (byte*)basePtr + (i * elementSize);
                Swap(pivotPosPtr, pivotPtr, (nuint)elementSize); // 交换基准点到正确位置

                long p = i; // 基准点的新位置

                // 将子数组推入栈中
                if (p - 1 > low)
                {
                    stackLow[stackDepth] = low;
                    stackHigh[stackDepth] = p - 1;
                    stackDepth++;
                }
                if (p + 1 < high)
                {
                    stackLow[stackDepth] = p + 1;
                    stackHigh[stackDepth] = high;
                    stackDepth++;
                }
            }
        }
    }



    /// <summary>
    /// 内存交换函数
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="size"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Swap(void* a, void* b, nuint size)
    {
        // 使用 stackalloc 临时存储元素，然后进行两次 MemoryCopy
        byte* temp = stackalloc byte[(int)size];
        Buffer.MemoryCopy(a, temp, size, size);
        Buffer.MemoryCopy(b, a, size, size);
        Buffer.MemoryCopy(temp, b, size, size);
    }

    /// <summary>
    /// 对 T* 集合进行排序。
    /// </summary>
    internal static void Sort<T>(T* basePtr, uint count, delegate*<nint, nint, int> comparer)
    where T : unmanaged
    {
        if (basePtr == null || count <= 1) return;

        QuickSort(basePtr, count, sizeof(T), comparer);
    }


}