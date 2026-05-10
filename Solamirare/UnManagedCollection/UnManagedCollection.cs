using System.Runtime.CompilerServices;

namespace Solamirare;

/// <summary>
/// 内存段的基本描述单位
/// <para>The basic description unit of a memory segment.</para>
/// </summary>
/// <typeparam name="T"></typeparam>
[SkipLocalsInit]
[Guid(SolamirareEnvironment.UnManagedCollectionGuid)]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8, Size = 16)]
public unsafe partial struct UnManagedCollection<T>
where T : unmanaged
{

    /// <summary>
    /// <para>指针，指向数据所在的内存地址</para>
    /// <para>Pointer, pointing to the memory address where the data is located.</para>
    /// <para>禁止外部调用者直接访问，防止误修改指向</para>
    /// <para>Direct access by external callers is prohibited to prevent accidental modification of the pointer.</para>
    /// <para>对象指针下标：0</para>
    /// <para>Object pointer index: 0.</para>
    /// </summary>
    internal T* InternalPointer;



    /// <summary>
    /// 当前使用的内存长度，单位是 T 的个数
    /// <para>The length of memory currently in use, in units of T.</para>
    /// <para>指针位移：8</para>
    /// <para>Pointer offset: 8.</para>
    /// </summary>
    internal uint size;


    /// <summary>
    /// 填充到16字节
    /// <para>Padding to 16 bytes.</para>
    /// </summary>
    private fixed byte padding[4];


    static UnManagedCollection()
    {
        Empty = new UnManagedCollection<T>();
        Empty.InternalPointer = null;
        Empty.size = 0;
    }



    /// <summary>
    /// 当前使用的内存长度，单位是 T 的个数
    /// <para>The length of memory currently in use, in units of T.</para>
    /// </summary>
    public uint Size
    {
        get { return size; }
    }

    /// <summary>
    /// 当前是否为空
    /// <para>Whether it is currently empty.</para>
    /// </summary>
    public bool IsEmpty
    {
        get
        {
            return InternalPointer is null || Size == 0;
        }
    }


    /// <summary>
    /// 初始化一个空的 UnManagedCollection 实例。
    /// <para>Initializes an empty instance of UnManagedCollection.</para>
    /// </summary>
    public UnManagedCollection()
    {
        size = 0;
        InternalPointer = null;
    }


    /// <summary>
    /// 依据外部内存创建对象，观测器模式，禁止释放
    /// <para>Creates an object based on external memory, observer pattern, release prohibited.</para>
    /// </summary>
    /// <param name="memory"></param>
    /// <param name="size"></param>
    public UnManagedCollection(T* memory, uint size)
    {
        if (memory is not null)
        {
            InternalPointer = memory;

            this.size = size;
        }
        else
        {
            this = Empty;
        }
    }




    /// <summary>
    /// 空对象
    /// <para>Empty object.</para>
    /// </summary>
    public static readonly UnManagedCollection<T> Empty;


}