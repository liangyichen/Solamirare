namespace Solamirare;

public unsafe partial struct ValueStack<T>
where T : unmanaged
{

    /// <summary>
    /// 尝试查看栈顶项目，但不将其移除。
    /// </summary>
    public bool TryPeek(out T* result, bool @lock = true)
    {

        if (@lock) AcquireSpinlock();
        ulong currentCount = _count;

        if (currentCount == 0)
        {
            result = default;

            if (@lock) ReleaseSpinlock();
            return false;
        }


        ulong logicalIndex = currentCount - 1;

        ulong localIndex;

        StackSegment<T>* segment = FindSegment(logicalIndex, out localIndex);

        if (segment == null)
        {
            result = null;

            if (@lock) ReleaseSpinlock();
            return false;
        }


        result = segment->DataPtr + localIndex;

        if (@lock) ReleaseSpinlock();
        return true;
    }


    /// <summary>
    /// 查看栈顶项目，但不将其移除。
    /// </summary>
    public T* Peek(bool @lock = true)
    {
        T* result;

        if (TryPeek(out result, @lock))
        {
            return result;
        }

        throw new InvalidOperationException("Stack is empty.");
    }



}