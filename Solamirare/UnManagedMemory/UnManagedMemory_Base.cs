
using System.Runtime.CompilerServices;

namespace Solamirare;

/// <summary>
/// 非托管内存集合。
/// <para>支持三种内存模式：</para>
/// <para>1. 内部分配的堆内存模式。</para>
/// <para>2. 链接到外部的栈内存模式。</para>
/// <para>3. 链接到外部的堆内存模式。</para>
/// <para>注意：内部分配始终使用堆内存；栈内存模式始终链接到外部内存。</para>
/// </summary>
/// <typeparam name="T">非托管类型。</typeparam>
[SkipLocalsInit]
[Guid(SolamirareEnvironment.UnManagedMemoryGuid)]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Size = 64)]
public unsafe partial struct UnManagedMemory<T>
{

    /// <summary>
    /// 原型链的上级对象，内存段的基本描述单位。
    /// </summary>
    internal UnManagedCollection<T> Prototype;


    /// <summary>
    /// 内存池集群指针。
    /// </summary>
    MemoryPoolCluster* memoryPool;


    /// <summary>
    /// 当前分配的内存容量（元素数量）。
    /// </summary>
    internal uint capacity;

    /// <summary>
    /// 当前对象是否已销毁。
    /// </summary>
    bool disposed;


    /// <summary>
    /// 是否引用外部内存。
    /// </summary>
    bool isExternalMemory;


    /// <summary>
    /// 是否由内存池分配。
    /// </summary>
    bool onMemoryPool;


    /// <summary>
    /// 是否分配在栈上。
    /// </summary>
    bool onStack;

    /// <summary>
    /// 是否为只读模式。
    /// </summary>
    bool @readonly;

    /// <summary>
    /// 是否已分配内存。
    /// </summary>
    bool memoryAllocated;



    /// <summary>
    /// （仅当 T 为 char 时有效）序列化类型标识。
    /// </summary>
    JsonSerializeTypes serializeType;

    /// <summary>
    /// 当前对象是否处于激活状态。
    /// </summary>
    bool activated;

    /// <summary>
    /// 获取当前对象是否处于激活状态。
    /// </summary>
    public bool Activated => activated;

    /// <summary>
    /// 获取或设置指向底层非托管内存的指针。
    /// </summary>
    public T* Pointer
    {
        get
        {
            return Prototype.InternalPointer;
        }
        set
        {
            Prototype.InternalPointer = value;
        }
    }



    /// <summary>
    /// 获取或设置序列化类型。
    /// <para>用于 JSON 序列化与反序列化时的类型标识，默认值为 <see cref="JsonSerializeTypes.Any"/>。</para>
    /// <para>如果 T 为 int、double 等基元类型，此字段无效。</para>
    /// </summary>
    public JsonSerializeTypes SerializeType
    {
        get { return serializeType; }
        set { serializeType = value; }
    }




    /// <summary>
    /// 获取是否已为指针分配了内存。
    /// </summary>
    public bool Allocated
    {
        get
        {
            return memoryAllocated;
        }
    }

    /// <summary>
    /// 获取当前对象是否已释放资源。
    /// </summary>
    public bool Disposed
    {
        get
        {
            return disposed;
        }
    }



    /// <summary>
    /// 获取当前内存是否位于栈上。
    /// </summary>
    public bool OnStack
    {
        get
        {
            return onStack;
        }
    }


    /// <summary>
    /// 获取当前已使用的内存长度（元素数量）。
    /// </summary>
    public uint UsageSize
    {
        get { return Prototype.Size; }
    }


    /// <summary>
    /// 获取当前分配的内存总容量（元素数量）。
    /// </summary>
    public uint Capacity
    {
        get { return capacity; }
    }


    /// <summary>
    /// 获取当前对象是否为空（即未分配内存或长度为 0）。
    /// </summary>
    public bool IsEmpty
    {
        get
        {
            return Prototype.IsEmpty;
        }
    }

    /// <summary>
    /// 表示一个空的、只读的 <see cref="UnManagedMemory{T}"/> 实例。
    /// </summary>
    public static readonly UnManagedMemory<T> Empty;

    /// <summary>
    /// 返回一个用于循环访问集合的枚举器。
    /// </summary>
    /// <returns>用于遍历集合的枚举器。</returns>
    public UnManagedCollection<T>.Enumerator GetEnumerator()
    {
        return Prototype.GetEnumerator();
    }




}
