using System.Runtime.CompilerServices;

namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 设置值（相当于范围赋值），输入的集合会覆盖对应的后续值，例如原始值 [1,2,3,4,5,6], 设置 (2,[100,101,102]) 后， 结果是 [1,2,100,101,102,6]。
    /// <para>越界输入 index 将会扩展容量，有可能会导致内存地址变更。</para>
    /// </summary>
    /// <param name="index">指定位置。</param>
    /// <param name="memory"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public bool SetValue(uint index, T* memory, uint length)
    {
        if (Pointer is null)
            return false;

        if (memory is null || length < 1 || memory is null || !activated)
            return false;

        uint requiredCapacity = index + length; //需要的容量

        bool ensure = EnsureCapacity(requiredCapacity, MemoryScaleMode.AppendEquals);

        if (ensure)
        {
            if (requiredCapacity > UsageSize) Prototype.size = requiredCapacity;

            NativeMemory.Copy(memory, &Pointer[index], (uint)(length * sizeof(T)));

            return true;
        }
        else
        {
            return false;
        }
    }


    /// <summary>
    /// 设置值（相当于范围赋值），输入的集合会覆盖对应的后续值，例如原始值 [1,2,3,4,5,6], 设置 (2,[100,101,102]) 后， 结果是 [1,2,100,101,102,6]。
    /// <para>越界输入 index 将会扩展容量，同时有可能也会导致内存地址变更。</para>
    /// </summary>
    /// <param name="index">指定位置设置。</param>
    /// <param name="memory"></param>
    /// <returns></returns>
    public bool SetValue(uint index, UnManagedCollection<T>* memory)
    {
        if (Pointer is null)
            return false;

        return SetValue(index, memory->InternalPointer, memory->Size);
    }


    /// <summary>
    /// 在指定位置设置值，越界超出范围会自动扩容
    /// </summary>
    /// <param name="index"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool SetValue(uint index, in T value)
    {
        bool result = false;

        fixed (T* p = &value)
            result = SetValue(index, p, 1);

        return result;
    }


}