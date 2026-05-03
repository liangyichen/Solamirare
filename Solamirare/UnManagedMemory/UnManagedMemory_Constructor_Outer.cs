
using System.Runtime.CompilerServices;

namespace Solamirare;

// 通过外部内存地址构造对象，内部逻辑不会再次创建内存，不可释放外部内存


public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{
    



    /// <summary>
    /// 根据既有的外部数据指针创建一个观测器，内部不会再次分配内存，Dispose 操作无效，外部需要自行保证 outterPointer 指向的内存段具备安全长度。
    /// <para>Creates an observer based on an existing external data pointer. No memory is allocated internally. Dispose operation is invalid. The external caller must ensure that the memory segment pointed to by outterPointer has a safe length.</para>
    /// <para>外部的指针不会被自动位移（值类型传递也保证了外部指针不可能会被位移）</para>
    /// <para>The external pointer will not be automatically shifted (value type passing also ensures that the external pointer cannot be shifted).</para>
    /// </summary>
    /// <param name="externalMemory">外部数据指针<para>External data pointer.</para></param>
    /// <param name="externalMemorySize">外部内存的长度（容量），单位为 T 的数量。<para>The length (capacity) of external memory, in units of T.</para></param>
    /// <param name="length">设置初始已使用的长度，单位为 T 的数量。<para>Sets the initial used length, in units of T.</para></param>
    /// <param name="memoryType">手动标识内存类型，属于栈或堆<para>Manually identify memory type, belonging to stack or heap.</para></param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public UnManagedMemory(T* externalMemory, uint externalMemorySize, uint length, MemoryTypeDefined memoryType = MemoryTypeDefined.Unknown)
    {
        Init(externalMemory, externalMemorySize, length, memoryType);
    }





    /// <summary>
    /// 根据既有的外部数据指针创建一个观测器，内部不会再次分配内存，Dispose 操作无效，外部需要自行保证 outterPointer 指向的内存段具备安全长度。
    /// <para>Creates an observer based on an existing external data pointer. No memory is allocated internally. Dispose operation is invalid. The external caller must ensure that the memory segment pointed to by outterPointer has a safe length.</para>
    /// <para>外部的指针不会被自动位移（值类型传递也保证了外部指针不可能会被位移）</para>
    /// <para>The external pointer will not be automatically shifted (value type passing also ensures that the external pointer cannot be shifted).</para>
    /// </summary>
    /// <param name="externalMemory">外部数据指针<para>External data pointer.</para></param>
    /// <param name="dataCollection">将此数据集合的值复制到 externalMemory 指向的内存段中<para>Copies the values of this data collection to the memory segment pointed to by externalMemory.</para></param>
    /// <param name="memoryType">手动标识内存类型，属于栈或堆<para>Manually identify memory type, belonging to stack or heap.</para></param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public UnManagedMemory(T* externalMemory, ReadOnlySpan<T> dataCollection, MemoryTypeDefined memoryType = MemoryTypeDefined.Unknown)
    {
        if (activated) return;

        Init(externalMemory, (uint)dataCollection.Length, 0, memoryType);

        if (dataCollection.Length > 0)
            AddRange(dataCollection);
    }







    /// <summary>
    /// 根据既有的外部数据指针创建一个只读观测器，内部不会再次分配内存，Dispose 操作无效，外部需要自行保证 outterPointer 指向的内存段具备安全长度。创建完毕后 outterPointer 会按照 dataCollection 的长度自动位移
    /// <para>Creates a read-only observer based on an existing external data pointer. No memory is allocated internally. Dispose operation is invalid. The external caller must ensure that the memory segment pointed to by outterPointer has a safe length. After creation, outterPointer will be automatically shifted according to the length of dataCollection.</para>
    /// <para>自动位移的意义在于：把不同的数据保存到一段长度足够的连续内存中，赋值的过程不需要考虑下标的位置：</para>
    /// <para>The significance of automatic shifting is: saving different data to a continuous memory segment of sufficient length, the assignment process does not need to consider the position of the index:</para>
    /// <para>例如：</para>
    /// <para>For example:</para>
    /// <para>var sub0 = new UnManagedMemory&lt;char&gt;(memory,"abc",memoryType); </para>
    /// <para>var sub1 = new UnManagedMemory&lt;char&gt;(memory,"def",memoryType); </para>
    /// <para>var sub2 = new UnManagedMemory&lt;char&gt;(memory,"ghi",memoryType); </para>
    /// <para>此时 sub1 的指针0下标会紧接着 sub0</para>
    /// <para>At this time, the pointer 0 index of sub1 will immediately follow sub0.</para>
    /// <para>此时 sub2 的指针0下标会紧接着 sub1</para>
    /// <para>At this time, the pointer 0 index of sub2 will immediately follow sub1.</para>
    /// </summary>
    /// <param name="externalMemory">必须传入指针的指针，这样才可做到自动位移， 外部使用就是简单加一个 &amp; 号即可<para>Must pass a pointer to a pointer to achieve automatic shifting; external usage is simply adding an &amp; sign.</para></param>
    /// <param name="dataCollection">把这些数据集合的值复制到 outterPointer 指向内存段中<para>Copies the values of these data collections to the memory segment pointed to by outterPointer.</para></param>
    /// <param name="memoryType">手动标识内存类型，属于栈或堆<para>Manually identify memory type, belonging to stack or heap.</para></param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public UnManagedMemory(T** externalMemory, ReadOnlySpan<T> dataCollection, MemoryTypeDefined memoryType = MemoryTypeDefined.Unknown)
    {
        if (activated) return;

        Init(*externalMemory, (uint)dataCollection.Length, 0, memoryType);

        if (dataCollection.Length > 0)
        {
            AddRange(dataCollection);

            *externalMemory += dataCollection.Length;

            SetAsReadOnly();
        }
    }



    /// <summary>
    /// 手动构造函数
    /// </summary>
    /// <param name="externalMemory"></param>
    /// <param name="externalMemorySize"></param>
    /// <param name="length"></param>
    /// <param name="memoryType"></param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Init(T* externalMemory, uint externalMemorySize, uint length, MemoryTypeDefined memoryType)
    {
        if (activated) return;

        initFields(externalMemorySize, length);

        isExternalMemory = true;

        if (memoryType == MemoryTypeDefined.Stack)
        {
            onStack = true;
        }
        else if (memoryType == MemoryTypeDefined.Heap)
        {
            onStack = false;
        }
        else
        {
            onStack = MemoryTypeChecker.OnStack(Pointer);
        }

        Prototype.InternalPointer = externalMemory;

        disposed = true; //禁止释放外部内存
    }

}