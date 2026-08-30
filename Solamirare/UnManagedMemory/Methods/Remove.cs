using System.Runtime.CompilerServices;

namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 移除指定位置的元素（默认不会压缩内存）。
    /// </summary>
    /// <param name="index"></param>
    /// <param name="trim">是否压缩内存。</param>
    /// <returns></returns>
    public bool RemoveAt(int index, bool trim = false)
    {
        if (Pointer is null || @readonly || !activated) return false;

        if (index < 0 || index >= UsageSize)
        {
            return false;
        }

        //后方元素统一向前移一位
        if (index < UsageSize - 1)
        {
            Unsafe.CopyBlock(&Pointer[index], &Pointer[index + 1], (uint)((Prototype.Size - index - 1) * sizeof(T)));
        }

        Prototype.size--;

        if (trim) Resize(UsageSize);


        return true;
    }



    /// <summary>
    /// 移除范围
    /// </summary>
    /// <param name="index"></param>
    /// <param name="length"></param>
    /// <param name="trim">是否压缩内存</param>
    /// <returns></returns>
    public bool RemoveRange(uint index, uint length, bool trim = false)
    {
        if (Pointer is null || @readonly || !activated) return false;

        if (Capacity < 1) return false;

        bool result = false;

        if (index + length <= UsageSize)
        {
            uint copy_length = (uint)sizeof(T) * (UsageSize - index - length);

            NativeMemory.Copy(&Pointer[index + length], &Pointer[index], copy_length);

            Prototype.size -= length;

        }
        else if (index + length == UsageSize)
        {
            Prototype.Zero();
        }
        else
        {
            goto RETURN;
        }

        if (trim && !OnStack)
        {
            Resize(UsageSize);
        }

        result = true;

        RETURN:

        return result;

    }

}