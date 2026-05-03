using System.Runtime.CompilerServices;

namespace Solamirare;


/// <summary>
/// 非托管字典
/// <para>Unmanaged Dictionary</para>
/// </summary>
[SkipLocalsInit]
[Guid(SolamirareEnvironment.ValueDictionaryGuid)]
[StructLayout(LayoutKind.Sequential, Size = 64)]
public unsafe partial struct ValueDictionary<TKey, TValue>
    where TKey : unmanaged
    where TValue : unmanaged
{
    const byte ByteEmpty = 0xFF;

    const byte ByteDeleted = 0xFE;

    //====================

    internal byte* _ctrl; //为 byte 类型分配指针是因为 Swiss Table 算法所需

    MemoryPoolCluster* memoryPool;

    internal uint _capacity;

    uint _count;


    /*

    _deletedCount 是为了解决开放寻址法删除元素带来的副作用。
    它确保了字典在经历大量增删操作后，能够识别出“虽然元素不多，但坑位都被占着（被墓碑占领）”的亚健康状态，并及时触发清理操作。
    
    */
    uint _deletedCount;

    int _version;

    internal bool _disposed;

    bool _onMemoryPool;

    bool _frozen;

    bool _created;


    internal DictionarySlot<TKey, TValue>* _slots
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {

            if (_ctrl == null) return null;
            // 重新计算 slots 的偏移量，必须与 AllocateMemory 中的逻辑一致
            ulong ctrlSize = (ulong)_capacity + 16;
            uint alignment = 64;
            ulong offsetToSlots = (ctrlSize + (alignment - 1)) & ~(ulong)(alignment - 1);
            return (DictionarySlot<TKey, TValue>*)(_ctrl + offsetToSlots);
        }
    }



    //===================================

    /// <summary>
    /// 是否已经初始化
    /// </summary>
    public bool Created => _created;


    /// <summary>
    /// 获取字典是否为空
    /// <para>Gets whether the dictionary is empty.</para>
    /// </summary>
    public bool IsEmpty => _count == 0;


    /// <summary>
    /// 已经使用的容量（键值对的数量）
    /// <para>Gets the number of key-value pairs contained in the dictionary.</para>
    /// </summary>
    public uint Count => _count;

    /// <summary>
    /// 获取当前分配的容量
    /// <para>Gets the current allocated capacity.</para>
    /// </summary>
    public uint Capacity => _capacity;

    /// <summary>
    /// 初始化 ValueDictionary 的新实例
    /// <para>Initializes a new instance of the ValueDictionary struct.</para>
    /// </summary>
    public ValueDictionary() : this(8, false, null) { }

    /// <summary>
    /// 初始化 ValueDictionary 的新实例
    /// <para>Initializes a new instance of the ValueDictionary struct.</para>
    /// </summary>
    public void Init()
    {
        Init(8,false,null);
    }


    /// <summary>
    /// 初始化 ValueDictionary 的新实例
    /// <para>Initializes a new instance of the ValueDictionary struct.</para>
    /// </summary>
    /// <param name="initialCapacity">初始容量 <para>Initial capacity</para></param>
    /// <param name="frozen">是否冻结（不可修改） <para>Whether the dictionary is frozen (immutable)</para></param>
    /// <param name="memoryPool"></param>
    public void Init(uint initialCapacity,  bool frozen = false, MemoryPoolCluster* memoryPool = null)
    {
        _count = 0;
        _deletedCount = 0;
        _disposed = false;
        _frozen = frozen;
        _version = 0;
        _created = true;
        this.memoryPool = memoryPool;

        if (initialCapacity > 0)
        {
            _capacity = Math.Max(4, DictionaryMathUtils.NextPowerOfTwo(initialCapacity));

            AllocateMemory(_capacity, out _ctrl);

            Unsafe.InitBlock(_ctrl, ByteEmpty, _capacity + 16);
        }
    }

    /// <summary>
    /// 初始化 ValueDictionary 的新实例
    /// <para>Initializes a new instance of the ValueDictionary struct.</para>
    /// </summary>
    /// <param name="initialCapacity">初始容量 <para>Initial capacity</para></param>
    /// <param name="frozen">是否冻结（不可修改） <para>Whether the dictionary is frozen (immutable)</para></param>
    /// <param name="memoryPool"></param>
    public ValueDictionary(uint initialCapacity,  bool frozen = false, MemoryPoolCluster* memoryPool = null)
    {
        Init(initialCapacity,frozen,memoryPool);
    }



    #region Public Dictionary APIs


    /// <summary>
    /// 获取一个空的 ValueDictionary 实例
    /// <para>Gets an empty ValueDictionary instance.</para>
    /// </summary>
    public static ValueDictionary<TKey, TValue> Empty
    {
        get
        {
            return new ValueDictionary<TKey, TValue>(0, true);
        }
    }



    /// <summary>
    /// 获取键的哈希码
    /// <para>Gets the hash code of the key.</para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetKeyHashCode(TKey* key)
    {
        //这里尚需要改为所有类型的UnManagedMemory或UnManagedCollection
        if (typeof(TKey) == typeof(UnManagedString) || typeof(TKey) == typeof(UnManagedCollection<char>))
        {
            return (int)((UnManagedString*)key)->AsSpan().HashCode();
        }
        if (typeof(TKey) == typeof(UnManagedMemory<byte>) || typeof(TKey) == typeof(UnManagedCollection<byte>))
        {
            return (int)((UnManagedMemory<byte>*)key)->AsSpan().HashCode();
        }

        uint len = (uint)sizeof(TKey);
        return (int)ValueTypeHelper.HashCode((byte*)key, (int)len, 0);
    }

    /// <summary>
    /// 统一的核心插入逻辑
    /// <para>Unified core insertion logic.</para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int InsertNew(TKey* key, TValue* value, int hash)
    {
        if (key == null || value == null) return -1;
        if (_disposed) return -1;

        // 1. 统一扩容判定 (负载因子 0.75)
        if (_capacity == 0 || _count + _deletedCount >= DictionaryMathUtils.CalculateThreshold(_capacity))
        {
            uint newCap = _capacity == 0 ? 4 : _capacity * 2;
            if (!Resize(newCap)) return -1;
        }

        if (_ctrl == null) return -1;

        // 2. 寻找空位并写入
        int insertIdx = FindFreeSlot(_ctrl, _capacity, hash);

        CommitSlot(insertIdx, key, value, hash, GetH2(hash));

        return insertIdx;
    }





    /// <summary>
    /// 确定字典是否包含指定的键
    /// <para>Determines whether the dictionary contains the specified key.</para>
    /// </summary>
    /// <param name="key">要查找的键 <para>The key to locate</para></param>
    /// <returns>如果包含指定键返回 true <para>True if the dictionary contains the specified key</para></returns>
    public bool ContainsKey(in TKey key)
    {
        if (_disposed || _ctrl == null) return false;

        fixed (TKey* pKey = &key)
        {
            return FindSlotIndexInternal(pKey) != -1;
        }
    }

    /// <summary>
    /// 确定字典是否包含指定的值
    /// <para>Determines whether the dictionary contains the specified value.</para>
    /// </summary>
    /// <param name="value">要查找的值 <para>The value to locate</para></param>
    /// <returns>如果包含指定值返回 true <para>True if the dictionary contains the specified value</para></returns>
    public bool ContainsValue(in TValue value)
    {
        if (_disposed || _ctrl == null) return false;

        ReadOnlySpan<byte> target = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in value), 1));
        for (uint i = 0; i < _capacity; i++)
        {
            if (_ctrl[i] <= 0x7F && MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref _slots[i].Value, 1)).SequenceEqual(target))
                return true;
        }
        return false;
    }



    /// <summary>
    /// 确保字典可以保存指定数量的元素而无需扩容
    /// <para>Ensures that the dictionary can hold up to a specified number of entries without any further expansion of its backing storage.</para>
    /// </summary>
    /// <param name="capacity">要确保的容量 <para>The capacity to ensure</para></param>
    /// <returns>当前容量 <para>The current capacity</para></returns>
    public uint EnsureCapacity(uint capacity)
    {
        if (_frozen || _disposed) return _capacity;
        // 考虑负载因子 0.75，预先分配足够的空间
        uint newCap = DictionaryMathUtils.NextPowerOfTwo((uint)(capacity * 4 / 3) + 1);
        if (newCap > _capacity) Resize(newCap);
        return _capacity;
    }

    /// <summary>
    /// 将字典的容量设置为其实际元素数量
    /// <para>Sets the capacity of this dictionary to what it would be if it had been originally initialized with all its entries.</para>
    /// </summary>
    public void TrimExcess()
    {
        if (_frozen || _disposed) return;
        Resize(Math.Max(4, DictionaryMathUtils.NextPowerOfTwo(_count)));
    }

    #endregion

    #region Core Operations











    /// <summary>
    /// 移除具有指定键的值
    /// <para>如果仅仅是执行 Remove 方法后，再次通过 Add 方法添加元素，有可能不会利用已经被删除的元素占据的内存</para>
    /// <para>如果需要保证利用即有的已经分配内存，必须执行 Clear 方法执行清零</para>
    /// <para>Removes the value with the specified key.</para>
    /// </summary>
    /// <param name="key">要移除的键 <para>The key of the element to remove</para></param>
    /// <returns>如果成功找到并移除元素返回 true <para>True if the element is successfully found and removed</para></returns>
    public bool Remove(in TKey key)
    {
        if (_disposed || _ctrl == null) return false;

        Type typeofKey = typeof(TKey);

        fixed (TKey* pKey = &key)
        {

            int index = FindSlotIndexInternal(pKey);

            if (index == -1) return false;

            MarkDeleted(index);

            _count--;
            _deletedCount++;
            _version++;
            return true;
        }
    }




    #endregion


    /// <summary>
    /// 分配内存
    /// <para>Allocates memory.</para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool AllocateMemory(uint capacity, out byte* ctrl)
    {
        ulong slotSize = (ulong)(sizeof(DictionarySlot<TKey, TValue>) * capacity);
        // 关键：额外申请 16 字节用于存放开头 16 字节的拷贝
        ulong ctrlSize = (ulong)capacity + 16;

        // 统一使用连续内存分配：ctrl 在前，slots 在后 (对齐到 64)
        // 这种布局减少了内存分配次数（减少分配器元数据开销），并提高了局部性
        uint alignment = 64;
        ulong offsetToSlots = (ctrlSize + (alignment - 1)) & ~(ulong)(alignment - 1);
        ulong totalSize = offsetToSlots + slotSize;

        byte* basePtr = null;

        if (memoryPool is not null)
        {
            byte* pool_alloc = memoryPool->Alloc(totalSize).Address;

            if (pool_alloc is not null)
            {
                _onMemoryPool = true;
                basePtr = pool_alloc;
            }
            else
            {
                //强制修正为独立分配
                _onMemoryPool = false;
            }
        }

        if(!_onMemoryPool && basePtr is null)
            basePtr = (byte*)NativeMemory.AlignedAlloc((nuint)totalSize, SolamirareEnvironment.ALIGNMENT);


        if (basePtr == null)
        {
            ctrl = null;
            return false;
        }

        ctrl = basePtr;
        NativeMemory.Clear(basePtr + offsetToSlots, (nuint)slotSize);
        Unsafe.InitBlock(ctrl, ByteEmpty, (uint)ctrlSize);

        return true;
    }

    /// <summary>
    /// 释放内存
    /// <para>Frees memory.</para>
    /// </summary>
    private void FreeMemory(void* ctrlPtr, uint capacity, bool useMemoryPool)
    {
        if (ctrlPtr == null) return;
        ulong slotSize = (ulong)(sizeof(DictionarySlot<TKey, TValue>) * capacity);
        ulong ctrlSize = (ulong)capacity + 16;

        uint alignment = 64;
        ulong offsetToSlots = (ctrlSize + (alignment - 1)) & ~(ulong)(alignment - 1);
        ulong totalSize = offsetToSlots + slotSize;

      
        if (useMemoryPool)
        {
            if(memoryPool is not null)
                memoryPool->Return(ctrlPtr, totalSize);
        }
        else
        {
            NativeMemory.AlignedFree(ctrlPtr);
        }
    }

    #region Indexers

    /// <summary>
    /// 获取与指定键关联的值的指针
    /// <para>Gets a pointer to the value associated with the specified key.</para>
    /// </summary>
    /// <param name="key">要获取值的键 <para>The key of the value to get</para></param>
    /// <returns>值的指针，如果未找到则为 null <para>Pointer to the value, or null if not found</para></returns>
    public TValue* Index(in TKey key)
    {
        if (_disposed || _ctrl == null) return null;

        fixed (TKey* pKey = &key)
        {
            int idx = FindSlotIndexInternal(pKey);

            return idx != -1 ? &_slots[idx].Value : null;
        }
    }

    /// <summary>
    /// 获取与指定键关联的值的指针
    /// <para>Gets a pointer to the value associated with the specified key.</para>
    /// </summary>
    /// <param name="key">要获取值的键 <para>The key of the value to get</para></param>
    /// <returns>值的指针，如果未找到则为 null <para>Pointer to the value, or null if not found</para></returns>
    public TValue* this[in TKey key]
    {
        get
        {
            return Index(key);
        }
    }




    #endregion



    #region Internal Support (Resize & Dispose)

    /// <summary>
    /// 调整字典大小
    /// <para>Resizes the dictionary.</para>
    /// </summary>
    private bool Resize(uint newCapacity)
    {
        if (_frozen || _disposed) return false;
        DictionarySlot<TKey, TValue>* newSlots;
        byte* newCtrl;

        bool useMemoryPoolBefore = _onMemoryPool; //之前的旧内存段是否位于内存池

        bool allocNewSuccess = AllocateMemory(newCapacity, out newCtrl);

        if (newCtrl == null || !allocNewSuccess) return false;

        // 手动计算 newSlots 指针
        {
            ulong ctrlSize = (ulong)newCapacity + 16;
            uint alignment = 64;
            ulong offsetToSlots = (ctrlSize + (alignment - 1)) & ~(ulong)(alignment - 1);
            newSlots = (DictionarySlot<TKey, TValue>*)(newCtrl + offsetToSlots);
        }

        uint newMask = newCapacity - 1;
        uint oldCapacity = _capacity;

        for (uint i = 0; i < oldCapacity; i++)
        {
            // 仅处理有效条目 (0x00-0x7F)
            if (_ctrl[i] <= 0x7F)
            {
                int hashCode;
                TKey* pKey = &_slots[i].Key;

                // --- 优化：直接使用缓存的 HashCode ---
                hashCode = _slots[i].HashCode;

                byte h2 = GetH2(hashCode);

                int insertIdx = FindFreeSlot(newCtrl, newCapacity, hashCode);
                WriteSlot(newCtrl, newSlots, newCapacity, insertIdx, pKey, &_slots[i].Value, hashCode, h2);
            }
        }

        FreeMemory(_ctrl, _capacity, useMemoryPoolBefore);

        _ctrl = newCtrl;
        _capacity = newCapacity;
        _deletedCount = 0;
        _version++;

        return true;
    }

    /// <summary>
    /// 遍历键值对， 函数指针参数依次是 int 迭代次数，key, value，void* 参数传递，返回值 bool 控制是否继续迭代
    /// <para>Iterates over key-value pairs.</para>
    /// </summary>
    /// <param name="onload">回调函数指针，参数依次是：索引、键指针、值指针、调用者上下文。返回值 bool 控制是否继续迭代。<para>Callback function pointer. Parameters are: index, key pointer, value pointer, caller context. Returns bool to control whether to continue iteration.</para></param>
    /// <param name="caller">调用者上下文 <para>Caller context</para></param>
    public void ForEach(delegate*<int, TKey*, TValue*, void*, bool> onload, void* caller)
    {
        if (_disposed || onload == null || _ctrl == null) return;

        for (uint i = 0; i < _capacity; i++)
        {
            if (_ctrl[i] <= 0x7F)
            {
                DictionarySlot<TKey, TValue>* slot = &_slots[i];

                if (slot is not null)
                {

                    bool loop = onload((int)i, &slot->Key, &slot->Value, caller);

                    if (!loop) break;
                }
            }
        }
    }

    /// <summary>
    /// 分配内存（内部实现）
    /// <para>Allocates memory (internal implementation).</para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    byte* Alloc(ulong size)
    {
        if (memoryPool is not null)
        {
            byte* pool_alloc = memoryPool->Alloc(size).Address;

            if (pool_alloc is not null)
            {
                _onMemoryPool = true;
                return pool_alloc;
            }
            else
            {
                //强制修正为独立分配
                _onMemoryPool = false;
                goto ALLOC_ON_HEAP;
            }
        }

        ALLOC_ON_HEAP:
        return (byte*)NativeMemory.AlignedAlloc((nuint)size, SolamirareEnvironment.ALIGNMENT);

    }



    /// <summary>
    /// 移除所有键值对
    /// <para>注意，只有执行该 Clear 方法后，才能实现重新使用已经分配的内存（如果仅仅是执行 Remove 方法后，再次通过 Add 方法添加元素，有可能不会利用已经被删除的元素占据的内存）</para>
    /// <para>Removes all keys and values from the dictionary.</para>
    /// </summary>
    public void Clear()
    {
        if (_disposed || (_count == 0 && _deletedCount == 0) || _ctrl == null)
            return;

        Unsafe.InitBlock(_ctrl, ByteEmpty, _capacity + 16);

        _count = 0;

        _deletedCount = 0;

        _version++;
    }


    /// <summary>
    /// 释放字典使用的资源
    /// <para>Releases resources used by the dictionary.</para>
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        FreeMemory(_ctrl, _capacity, _onMemoryPool);

        _ctrl = null;

        _disposed = true;
    }

    #endregion
}