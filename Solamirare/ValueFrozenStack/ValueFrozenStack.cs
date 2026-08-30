
using System.Runtime.CompilerServices;

namespace Solamirare;



/// <summary>
/// 值类型固定栈结构，长度不可变
/// </summary>
[SkipLocalsInit]
[Guid(SolamirareEnvironment.ValueFrozenStackGuid)]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8, Size = 16)]
public unsafe partial struct ValueFrozenStack<T>
where T : unmanaged
{
    /// <summary>
    /// 指向栈内存块的基地址
    /// </summary>
    T* _buffer;

    /// <summary>
    /// 栈的总容量（单位是 T 的数量），目前能够容纳的最大数值是 1,073,741,823
    /// </summary>
    public uint _capacity;


    /// <summary>
    /// 当前栈中已经使用的节点数量
    /// </summary>
    private uint _count;




    /// <summary>
    /// 是否来自外部的内存
    /// </summary>
    bool _externalMemory
    {
        get => (_capacity & 0x40000000) != 0;//0100 0000 0000 0000 0000 0000 0000 0000
        set
        {
            if (value) _capacity |= 0x40000000;
            else _capacity &= 0xBFFFFFFFu; //1011 1111 1111 1111 1111 1111 1111 1111
        }

    }


    /// <summary>
    /// 当前栈中已经使用的节点数量
    /// </summary>
    public uint Count
    {
        get
        {
            return _count;
        }
    }


    /// <summary>
    /// 栈的总容量（单位是 T 的数量）
    /// </summary>
    public ulong Capacity
    {
        get
        {
            return _capacity & 0x3FFFFFFFu;
        }
    }


    /// <summary>
    /// 固定栈结构，长度不可变
    /// </summary>
    /// <param name="capacity">注意单位是T的数量，目前能够容纳的最大数值是 1,073,741,823</param>
    /// <param name="onMemoryPool"></param>
    public ValueFrozenStack(uint capacity, bool onMemoryPool = false)
    {
        if (capacity <= 0)
        {
            return;
        }

        _capacity = capacity & 0x3FFFFFFFu;

        _count = 0;



        _externalMemory = false;

        nuint totalBytes = capacity * (uint)sizeof(T);
        
        _buffer = (T*)NativeMemory.AllocZeroed(totalBytes);

    }



    /// <summary>
    /// 值类型固定栈结构，长度不可变，通过外部内存来进行初始化
    /// </summary>
    /// <param name="externalMemory">外部内存段</param>
    /// <param name="capacity">注意单位是T的数量，目前能够容纳的最大数值是 1,073,741,823</param>
    public ValueFrozenStack(T* externalMemory, uint capacity)
    {
        _externalMemory = true;

        if (externalMemory is null || capacity <= 0)
        {
            _capacity = 0;

            _buffer = null;

            return;
        }


        _capacity = (capacity & 0x3FFFFFFFu) | 0x40000000; // 0100 0000 0000 0000 0000 0000 0000 0000

        _count = 0;

        nuint totalBytes = (nuint)(capacity & 0x3FFFFFFFu) * (nuint)sizeof(T);

        _buffer = externalMemory;

        NativeMemory.Clear(_buffer, totalBytes);
    }


}
