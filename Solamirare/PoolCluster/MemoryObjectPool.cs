using System.Runtime.CompilerServices;

namespace Solamirare;



/// <summary>
/// 内存对象池单元
/// <para>用于管理特定非托管类型对象的内存分配与回收，支持线程安全操作。</para>
/// </summary>
/// <typeparam name="T">默认关联的非托管类型</typeparam>
[SkipLocalsInit]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
public unsafe struct MemoryObjectPool<T>
where T : unmanaged
{

    // 指向空闲指针栈的起始地址
    // 指针数组
    ValueFrozenStack<nint> _freeStack; //16 字节


    // 指向整个内存块的起始地址
    void* innerMemory; //24


    //总容量
    uint _capacity; //28

    uint tSize; //32

    uint memoryLength; //36

    /// <summary>
    /// 线程安全：自旋锁状态 (0: unlock, 1: lock)
    /// </summary>
    int _spinlock; //40


    bool _isDisposed; //41


    fixed byte paddiing[23]; //填充到64字节


    /// <summary>
    /// 当前空余节点数量
    /// </summary>
    public uint FreeNodesCount
    {
        get
        {
            return _freeStack.Count;
        }
    }




    /// <summary>
    /// 初始化内存对象池
    /// </summary>
    /// <param name="capacity">池的容量（对象数量）</param>
    public MemoryObjectPool(uint capacity)
    {
        Reconstruct<T>(capacity);
    }


    /// <summary>
    /// 重新构建对象池，可用于改变容量或存储的类型
    /// <para>注意：此操作会释放旧内存并分配新内存，非线程安全，请确保没有其他线程正在使用。</para>
    /// </summary>
    /// <typeparam name="T2">新的目标类型</typeparam>
    /// <param name="capacity">新的容量</param>
    /// <returns>是否构建成功</returns>
    public bool Reconstruct<T2>(uint capacity)
    where T2 : unmanaged
    {
        if (capacity <= 0)
            return false;

        _capacity = capacity;

        tSize = (uint)sizeof(T2);

        memoryLength = tSize * capacity;

        // 2. 分配空闲指针栈的内存
        innerMemory = (T*)NativeMemory.AllocZeroed(memoryLength);


        // 3. 初始化空闲栈
        _freeStack = new ValueFrozenStack<nint>(capacity);

        byte* currentObjectPtr = (byte*)innerMemory;

        for (int i = 0; i < capacity; i++)
        {
            // 将第 i 个对象的地址存入栈的第 i 个位置

            _freeStack.Push((nint)currentObjectPtr);

            currentObjectPtr += tSize;
        }

        _isDisposed = false;

        return true;
    }




    /// <summary>
    /// 自旋锁
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AcquireSpinlock()
    {
        // 尝试从 0 (Unlocked) 交换到 1 (Locked)
        while (Interlocked.CompareExchange(ref _spinlock, 1, 0) != 0)
        {
            // 如果未能获取锁，则自旋等待一小段时间 (避免占用核心，让步给其他线程)
            // 在 C# 中，Thread.Yield() 比空循环更高效，因为它会通知调度器
            Thread.Yield();
        }
    }

    /// <summary>
    /// 解除自旋锁
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReleaseSpinlock()
    {
        // 直接将锁状态设置为 0 (Unlocked)
        // 使用 Interlocked.Exchange 确保写入操作的原子性和内存屏障
        Interlocked.Exchange(ref _spinlock, 0);
    }



    /// <summary>
    /// 尝试从池中分配一个对象
    /// <para>指针的类型应该由外部调用者根据最后一次调用构造函数或者重构函数时传递的类型决定</para>
    /// </summary>
    /// <param name="value">输出：分配到的内存地址，失败则为 null</param>
    /// <param name="pLength">输出：该内存块的长度（字节）</param>
    /// <returns>成功返回 true，池已空返回 false</returns>
    public bool Alloc(out void* value, out uint pLength)
    {
        if (_freeStack.Count == 0)
        {
            value = null;
            pLength = 0;
            return false;
        }

        AcquireSpinlock();
        

        bool result;

        // 4. 从栈顶弹出一个指针
        nint p = _freeStack.Count > 0 ? _freeStack.Pop() : 0;
        if (p != 0)
        {
            void* ptr = (void*)p;

            NativeMemory.Clear(ptr, tSize);

            value = ptr;

            pLength = tSize;

            result = true;
        }
        else
        {
            value = null;

            pLength = 0;

            result = false;
        }

        ReleaseSpinlock();

        return result;
    }

    /// <summary>
    /// 将对象归还回池中
    /// </summary>
    /// <param name="obj">要归还的内存指针</param>
    /// <returns>成功返回 true，指针无效返回 false</returns>
    public bool Return(void* obj)
    {
        AcquireSpinlock();

        bool result;

        // 安全性检查：确保归还的指针是属于这个池的
        if (!IsPointerFromPool(obj))
        {
            result = false;
        }
        else
        {
            NativeMemory.Clear(obj, tSize);

            // 5. 将指针压回栈顶
            result = _freeStack.Push((nint)obj);
        }

        ReleaseSpinlock();

        return result;
    }


    //用于验证指针的合法性
    bool IsPointerFromPool(void* ptr)
    {
        nuint ptrValue = (nuint)ptr;
        nuint start = (nuint)innerMemory;
        nuint end = start + (nuint)_capacity * tSize;

        // 指针必须在内存块范围内，且必须是对齐的
        return ptrValue >= start && ptrValue < end && (ptrValue - start) % tSize == 0;
    }


    /// <summary>
    /// 释放内存池占用的所有非托管资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        NativeMemory.Clear(innerMemory, memoryLength);
        NativeMemory.Free(innerMemory);
        innerMemory = null;

        _freeStack.Dispose();

        _capacity = 0;
        memoryLength = 0;
        tSize = 0;
    }

}