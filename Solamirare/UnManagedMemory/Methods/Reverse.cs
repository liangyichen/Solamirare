namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{
    /// <summary>
    /// 对集合中的元素进行反转。
    /// </summary>
    public void Reverse()
    {
        if (Pointer == null || UsageSize <= 1 || !activated) return;

        long sizeOfElement = sizeof(T);
        long left = 0;
        long right = Prototype.Size - 1;

        // 使用双指针从两端向中间遍历
        while (left < right)
        {
            // 1. 计算左侧元素和右侧元素的内存地址 (指针算术)
            void* leftPtr = (byte*)Pointer + (left * sizeOfElement);
            void* rightPtr = (byte*)Pointer + (right * sizeOfElement);

            // 2. 交换两个内存块
            UnmanagedMemorySorter.Swap(leftPtr, rightPtr, (nuint)sizeOfElement);

            // 3. 移动指针
            left++;
            right--;
        }
    }

}