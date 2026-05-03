namespace Solamirare;

public unsafe partial struct ValueStack<T>
where T : unmanaged
{

    /// <summary>
    /// 遍历元素
    /// <para>函数指针参数依次是：下标，元素指针，caller回传，是否继续迭代</para>
    /// </summary>
    /// <param name="onload"></param>
    /// <param name="caller"></param>
    /// <param name="lock"></param>
    public void ForEach(delegate*<int, T*, void*, bool> onload, void* caller, bool @lock = false)
    {
        if (onload == null || _count == 0) return;

        if (@lock) AcquireSpinlock();

        try
        {
            for (ulong i = 0; i < _count; i++)
            {
                ulong localIndex;
                StackSegment<T>* segment = FindSegment(i, out localIndex);

                if (segment == null)
                {
                    // 这应该不会发生，因为我们是在 _count 范围内迭代
                    break;
                }

                T* itemPtr = segment->DataPtr + localIndex;

                bool shouldContinue = onload((int)i, itemPtr, caller);

                if (!shouldContinue)
                {
                    break;
                }
            }
        }
        finally
        {
            if (@lock) ReleaseSpinlock();
        }
    }

}