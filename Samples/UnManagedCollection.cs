

/// <summary>
/// UnManagedCollection 的范例
/// </summary>
public static unsafe class Sample_UnManagedCollection
{
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public static void Constructor()
    {
        char* chars = stackalloc char[10];

        UnManagedCollection<char> col = new UnManagedCollection<char>(chars, 10);
    }


    /// <summary>
    /// 常用方法
    /// </summary>
    public static void CommonUsage()
    {
        char* chars = stackalloc char[10];

        UnManagedCollection<char> col = new UnManagedCollection<char>(chars, 10);

        //==========================================================
        // 与 UnManagedMemory 的转换

        UnManagedMemory<char> mem = new UnManagedMemory<char>(col); //复制
        mem.Dispose();

        UnManagedMemory<char> mem_0 = col.AsUnManagedMemory(); //映射

        UnManagedCollection<char> col_0 = mem; //直接转换

        //====================
        // 与 Span 的转换

        ReadOnlySpan<char> chars_1 = "chars";

        UnManagedCollection<char> col0 = chars_1; //隐式转换

        UnManagedCollection<char> col_1 = chars_1.MapToUnManagedCollection(); //显式转换
    }
}