using System.Runtime.CompilerServices;

namespace Solamirare;

public unsafe partial struct ValueFrozenStack<T>
where T : unmanaged
{

    /// <summary>
    /// 遍历元素
    /// <para>函数指针参数依次是：下标，元素指针，caller回传，是否继续迭代</para>
    /// </summary>
    /// <param name="onload"></param>
    /// <param name="caller"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueFrozenStack<T>* ForEach(delegate*<int, T*, void*, bool> onload, void* caller)
    {

        fixed (ValueFrozenStack<T>* p = &this)
        {

            if (onload == null || _count == 0) return p;

            for (int i = 0; i < _count; i++)
            {
                bool shouldContinue = onload(i, &_buffer[i], caller);

                if (!shouldContinue)
                {
                    break;
                }
            }

            return p;
        }

    }


}