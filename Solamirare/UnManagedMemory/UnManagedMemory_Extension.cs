using System.Runtime.CompilerServices;

namespace Solamirare;



/// <summary>
/// UnManagedMemory 扩展方法
/// </summary>
public static unsafe class UnManagedMemory_Extension
{

    /// <summary>
    /// 一个表示为逻辑空的时间值 (1970-01-01 00:00:00)
    /// </summary>
    public static DateTime EmptyDatetimeValue { get; private set; }



    static UnManagedMemory_Extension()
    {
        EmptyDatetimeValue = DateTime.Parse("1970-01-01 00:00:00");
    }



    private static int IntComparer(nint ptrA, nint ptrB)
    {
        int a = *(int*)ptrA;
        int b = *(int*)ptrB;
        if (a < b) return -1;
        if (a > b) return 1;
        return 0;
    }


    private static int FloatComparer(nint ptrA, nint ptrB)
    {
        float a = *(float*)ptrA;
        float b = *(float*)ptrB;
        if (a < b) return -1;
        if (a > b) return 1;
        return 0;
    }


    private static int DoubleComparer(nint ptrA, nint ptrB)
    {
        double a = *(double*)ptrA;
        double b = *(double*)ptrB;
        if (a < b) return -1;
        if (a > b) return 1;
        return 0;
    }






    /// <summary>
    /// 针对 int 集合的排序
    /// </summary>
    /// <param name="source">要排序的集合</param>
    public static void Sort(this in UnManagedMemory<int> source)
    {
        if (source.Pointer is not null && source.Activated)
            UnmanagedMemorySorter.Sort(source.Pointer, source.UsageSize, &IntComparer);
    }


    /// <summary>
    /// 针对 double 集合的排序
    /// </summary>
    /// <param name="source">要排序的集合</param>
    public static void Sort(this in UnManagedMemory<double> source)
    {
        if (source.Pointer is not null && source.Activated)
            UnmanagedMemorySorter.Sort(source.Pointer, source.UsageSize, &DoubleComparer);
    }




    /// <summary>
    /// 针对 float 集合的排序
    /// </summary>
    /// <param name="source">要排序的集合</param>
    public static void Sort(this in UnManagedMemory<float> source)
    {
        if (source.Pointer is not null && source.Activated)
            UnmanagedMemorySorter.Sort(source.Pointer, source.UsageSize, &FloatComparer);
    }


    /// <summary>
    /// 将托管数组复制到新的非托管内存中
    /// </summary>
    /// <typeparam name="T">非托管类型</typeparam>
    /// <param name="source">要复制的托管数组</param>
    /// <returns>包含数组副本的非托管内存对象</returns>
    public static UnManagedMemory<T> CopyToUnManagedMemory<T>(this T[] source)
    where T : unmanaged
    {
        if (source is not null)
        {
            UnManagedMemory<T> contents = new UnManagedMemory<T>(source);

            return contents;
        }

        return new UnManagedMemory<T>();
    }





    /// <summary>
    /// 将 <see cref="ReadOnlySpan{T}"/> 映射到 <see cref="UnManagedCollection{T}"/>，不进行内存复制
    /// <para>注意：数据源必须位于非托管堆或栈上，不能是托管堆内存（如数组或字符串）</para>
    /// </summary>
    /// <typeparam name="T">非托管类型</typeparam>
    /// <param name="source">要映射的数据源</param>
    /// <returns>映射后的非托管集合</returns>
    public static UnManagedCollection<T> MapToUnManagedCollection<T>(this ReadOnlySpan<T> source)
    where T : unmanaged
    {
        if (source.Length < 1) return UnManagedCollection<T>.Empty;

        fixed (T* p = source)
        {
            UnManagedCollection<T> exist = new UnManagedCollection<T>(p, (uint)source.Length);

            return exist;
        }
    }



    /// <summary>
    /// 将 <see cref="ReadOnlySpan{T}"/> 映射到 <see cref="UnManagedMemory{T}"/>，不进行内存复制
    /// <para>注意：数据源必须位于非托管堆或栈上，不能是托管堆内存（如数组或字符串）</para>
    /// </summary>
    /// <param name="source">要映射的数据源</param>
    /// <returns>映射后的非托管内存对象</returns>
    public static UnManagedMemory<T> MapToUnManagedMemory<T>(this ReadOnlySpan<T> source)
    where T : unmanaged
    {
        if (source.Length < 1) return UnManagedMemory<T>.Empty;

        fixed (T* p = source)
        {
            UnManagedMemory<T> exist = new UnManagedMemory<T>(p, (uint)source.Length, (uint)source.Length);

            return exist;
        }
    }




    /// <summary>
    /// 将 <see cref="ReadOnlySpan{T}"/> 复制到新的非托管内存中
    /// </summary>
    /// <param name="source">要复制的数据源</param>
    /// <param name="capacity">
    /// 手动指定初始容量
    /// <para>如果小于数据源长度，则忽略此参数并使用数据源长度</para>
    /// <para>设置为 0 则自动分配等同于数据源的长度</para>
    /// </param>
    /// <returns>包含数据副本的非托管内存对象</returns>
    public static UnManagedMemory<T> CopyToUnManagedMemory<T>(this ReadOnlySpan<T> source, int capacity = 0)
    where T : unmanaged
    {
        if (source.Length < 1) return UnManagedMemory<T>.Empty;

        int length = capacity > source.Length ? capacity : source.Length;

        UnManagedMemory<T> exist = new UnManagedMemory<T>((uint)length, 0);

        exist.AddRange(source);




        return exist;
    }

    /// <summary>
    /// 将 <see cref="ReadOnlySpan{T}"/> 复制到指定的外部非托管内存中
    /// <para>返回的 <see cref="UnManagedMemory{T}"/> 对象依赖于外部内存，不可执行 Dispose 操作释放该内存</para>
    /// </summary>
    /// <typeparam name="T">非托管类型</typeparam>
    /// <param name="source">要复制的数据源</param>
    /// <param name="externalBuffer">用于存储数据的外部内存指针</param>
    /// <param name="bufferSize">
    /// 外部内存的大小
    /// <para>如果大于 0，则以此值标记内存长度</para>
    /// <para>如果为 0，则假定其长度足以容纳数据源</para>
    /// <para>若手动赋值，必须大于等于数据源长度，否则复制不会执行</para>
    /// </param>
    /// <returns>指向外部内存的非托管内存对象</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnManagedMemory<T> CopyToUnManagedMemory<T>(this ReadOnlySpan<T> source, T* externalBuffer, uint bufferSize = 0)
    where T : unmanaged
    {
        if (source.Length < 1) return default;

        uint realSize = bufferSize is not 0 ? bufferSize : (uint)source.Length;

        UnManagedMemory<T> exist = new UnManagedMemory<T>(externalBuffer, realSize, 0);

        if (bufferSize >= source.Length)
        {
            exist.AddRange(source);
        }


        return exist;
    }







    /// <summary>
    /// 返回一个新的非托管内存，将元素右对齐，左侧填充指定元素以达到总长度
    /// </summary>
    /// <param name="source">源数据</param>
    /// <param name="totalWidth">结果的总长度</param>
    /// <param name="paddingElement">用于填充的元素</param>
    /// <returns>填充后的新非托管内存</returns>
    public static UnManagedMemory<T> PadLeft<T>(this in UnManagedMemory<T> source, int totalWidth, T paddingElement = default)
    where T : unmanaged
    {
        if (source.UsageSize >= totalWidth) return source.Clone();

        uint width = (uint)totalWidth;
        UnManagedMemory<T> result = new UnManagedMemory<T>(width, width);

        uint padLen = width - source.UsageSize;
        T* pRes = result.Pointer;

        for (uint i = 0; i < padLen; i++) pRes[i] = paddingElement;

        if (!source.IsEmpty)
        {
            Unsafe.CopyBlock(pRes + padLen, source.Pointer, (uint)(source.UsageSize * sizeof(T)));
        }

        return result;
    }

    /// <summary>
    /// 返回一个新的非托管内存，将元素左对齐，右侧填充指定元素以达到总长度
    /// </summary>
    /// <param name="source">源数据</param>
    /// <param name="totalWidth">结果的总长度</param>
    /// <param name="paddingElement">用于填充的元素</param>
    /// <returns>填充后的新非托管内存</returns>
    public static UnManagedMemory<T> PadRight<T>(this in UnManagedMemory<T> source, int totalWidth, T paddingElement = default)
    where T : unmanaged
    {
        if (!source.Activated) return UnManagedMemory<T>.Empty;

        if (source.UsageSize >= totalWidth) return source.Clone();

        uint width = (uint)totalWidth;
        UnManagedMemory<T> result = new UnManagedMemory<T>(width, width);

        T* pRes = result.Pointer;

        if (!source.IsEmpty)
        {
            Unsafe.CopyBlock(pRes, source.Pointer, (uint)(source.UsageSize * sizeof(T)));
        }

        for (uint i = source.UsageSize; i < width; i++) pRes[i] = paddingElement;

        return result;
    }

}