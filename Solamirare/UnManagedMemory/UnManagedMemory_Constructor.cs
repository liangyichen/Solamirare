
using System.Runtime.CompilerServices;

namespace Solamirare;




public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    static UnManagedMemory()
    {
        Empty = new UnManagedMemory<T>();

        Empty.@readonly = true;
    }




    /// <summary>
    /// 初始化一个空的非托管内存对象。此构造函数不会分配任何内存，其指针为 null。
    /// <para>Initializes an empty unmanaged memory object. This constructor does not allocate any memory, and its pointer is null.</para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public UnManagedMemory()
    {
        Init();
    }

    /// <summary>
    /// 初始化一个空的非托管内存对象，并指定其未来可能的分配方式。此构造函数不会立即分配内存。
    /// <para>Initializes an empty unmanaged memory object and specifies its possible future allocation method. This constructor does not allocate memory immediately.</para>
    /// </summary>
    /// <param name="PoolCluster"></param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public UnManagedMemory(MemoryPoolCluster* PoolCluster = null)
    {
        Init(PoolCluster);
    }


    /// <summary>
    /// 手动构造函数
    /// </summary>
    /// <param name="PoolCluster"></param>
    public void Init(MemoryPoolCluster* PoolCluster = null)
    {
        if (activated) onMemoryPool = false;

        initFields(0, 0);

        allocMemory(0, PoolCluster);
    }




    /// <summary>
    /// 分配一块指定容量的非托管内存，并可选择性地设置初始已用长度。
    /// <para>Allocates a block of unmanaged memory with the specified capacity, and optionally sets the initial used length.</para>
    /// </summary>
    /// <param name="TCount">要分配的内存容量，单位为 T 的数量。<para>The memory capacity to allocate, in units of T.</para></param>
    /// <param name="length">设置初始已使用的长度，单位为 T 的数量。<para>Sets the initial used length, in units of T.</para></param>
    /// <param name="PoolCluster"></param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public UnManagedMemory(uint TCount, uint length = 0, MemoryPoolCluster* PoolCluster = null)
    {
        Init(TCount, length, PoolCluster);
    }


    /// <summary>
    /// 手动构造函数
    /// </summary>
    /// <param name="TCount"></param>
    /// <param name="length"></param>
    /// <param name="PoolCluster"></param>
    public void Init(uint TCount, uint length = 0, MemoryPoolCluster* PoolCluster = null)
    {
        if (TCount < 1 || activated) onMemoryPool = false; //内存池严禁 0 分配

        initFields(TCount, length);

        allocMemory(TCount, PoolCluster);
    }




    /// <summary>
    /// 根据既有外部数据初始化非托管内存（复制到新的内存）
    /// <para>Initializes unmanaged memory based on existing external data (copies to new memory).</para>
    /// </summary>
    /// <param name="value"></param>
    /// <param name="PoolCluster"></param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public UnManagedMemory(ReadOnlySpan<T> value, MemoryPoolCluster* PoolCluster = null)
    {
        Init(value, PoolCluster);
    }


    /// <summary>
    /// 手动构造函数
    /// </summary>
    /// <param name="value"></param>
    /// <param name="PoolCluster"></param>
    public void Init(ReadOnlySpan<T> value, MemoryPoolCluster* PoolCluster = null)
    {

        if (value.Length < 1 || activated) onMemoryPool = false; //内存池严禁 0 分配

        initFields((uint)value.Length, 0);

        allocMemory((uint)value.Length, PoolCluster);

        AddRange(value);
    }





    /// <summary>
    /// 分配堆内存
    /// <para>Allocates heap memory.</para>
    /// </summary>
    /// <param name="TCount"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    void allocOnHeap(uint TCount)
    {
        if (TCount < 1) return;

        uint allocSize = (uint)(TCount * sizeof(T));

        ulong realSize = (uint)MemoryAlignmentHelper.Align((int)allocSize);

        Prototype.InternalPointer = (T*)NativeMemory.AllocZeroed((nuint)realSize);

        capacity = TCount;

        onMemoryPool = false;
    }


    /// <summary>
    /// 由内存池进行分配
    /// </summary>
    /// <param name="TCount"></param>
    bool allocOnMemoryPool(uint TCount)
    {
        if (TCount < 1) return false;

        uint allocSize = (uint)sizeof(T) * TCount;

        MemoryPollAllocatedResult result = memoryPool->Alloc(allocSize); //内存池传入逻辑所需容量即可，不需要外部向上取整

        if (result.Address is not null)
        {
            Prototype.InternalPointer = (T*)result.Address;

            capacity = TCount;

            return true;
        }

        return false;
    }



    /// <summary>
    /// 初始化内存段
    /// <para>Initializes the memory segment.</para>
    /// </summary>
    /// <param name="TCount">T的数量，对应创建的真实长度<para>The quantity of T, corresponding to the created real length.</para></param>
    /// <param name="PoolCluster"></param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    void allocMemory(uint TCount, MemoryPoolCluster* PoolCluster)
    {

        if (TCount > 0)
        {
            if (PoolCluster is not null)
            {
                memoryPool = PoolCluster;

                onMemoryPool = allocOnMemoryPool(TCount);

                if (!onMemoryPool) // 内存池分配失败，改为堆分配
                {
                    allocOnHeap(TCount);
                }
            }
            else
                allocOnHeap(TCount);

            memoryAllocated = true;
        }
        else
        {
            memoryAllocated = false;

            Prototype.InternalPointer = null;
        }

        onStack = false;

        disposed = false;
    }


    /// <summary>
    /// 为泛型参数标注对应的强类型标识，以及序列化类型
    /// <para>Annotates the corresponding strong type identifier and serialization type for the generic parameter.</para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    void initTypeCode()
    {

        Type t = typeof(T);

        TypeCode tCode = Type.GetTypeCode(t);

        SerializeType = JsonSerializes.GetTypeCode(tCode);


    }


    /// <summary>
    /// 初始化各个变量
    /// <para>Initializes various variables.</para>
    /// </summary>
    /// <param name="TCount">内存容量，单位为 T 的数量。<para>Memory capacity, in units of T.</para></param>
    /// <param name="usage">已使用的长度，单位为 T 的数量。<para>Used length, in units of T.</para></param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    void initFields(uint TCount, uint usage)
    {
        Prototype.size = usage;

        Prototype.InternalPointer = null;

        memoryPool = null;

        @readonly = false;

        capacity = TCount;

        isExternalMemory = false;

        activated = true;

        initTypeCode();
    }


    /// <summary>
    /// 重置相关状态，即将重用
    /// </summary>
    public void Reset()
    {
        activated = false;
    }


    /// <summary>
    /// 释放内存，当前对象被标记为不可用，除非再次调用 Init 手动构造函数再次初始化
    /// <para>指针指向内存由当前对象本身通过堆分配模式时，清空内存中的所有数据, 内部所有参数归 0；</para>
    /// <para>the memory pointed to by the pointer is allocated by the current object itself via heap allocation mode, all data in memory is cleared, all internal parameters are reset to 0;</para>
    /// </summary>
    public bool Dispose(bool clear = true)
    {

        if (OnStack || isExternalMemory)
        {
            if (!disposed) Prototype.Zero();

            return true;
        }


        if (disposed) return false;


        if (!onMemoryPool)
        {
            if (clear) NativeMemory.Clear(Pointer, (nuint)(sizeof(T) * UsageSize));

            if (Pointer is not null && !disposed)
            {
                NativeMemory.Free(Pointer);
            }
        }
        else
        {
            if (Pointer is not null && memoryPool is not null)
            {
                //归还后会自动清零内存中旧有的内容

                nuint size = (nuint)(capacity * sizeof(T));

                bool returnToPool = memoryPool->Return(Pointer, size, clear);

                if (returnToPool)
                {
                    memoryPool = null;

                    this.onMemoryPool = false; //重设为 false 的意义是为了回到初始化状态的默认值

                    goto FINAL;
                }
                else
                    return false;
            }

        }

    FINAL:

        Prototype.InternalPointer = null;

        Prototype.size = 0;

        disposed = true;

        memoryAllocated = false;

        activated = false;

        capacity = 0;

        return true;

    }


}