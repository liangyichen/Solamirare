
/// <summary>
/// UnManagedMemory 的范例
/// </summary>
public static unsafe class Sample_UnManagedMemory
{

    /// <summary>
    /// 构造函数重载
    /// </summary>
    public static void Constructor()
    {

        //========================================================
        //==== 默认空对象

        UnManagedMemory<char> mem = new UnManagedMemory<char>();

        bool zero_cap = mem.Capacity == 0; //容量

        bool zero_size = mem.UsageSize == 0; //已经使用长度

        bool allocated = mem.Allocated; //false, 未做容量分配

        mem.Dispose();


        //========================================================
        //==== 直接简单赋值，内存映射


        UnManagedMemory<char> mem_0 = "abcdefg";

        bool mem_0_cap = mem_0.Capacity == 7;

        mem_0.Dispose();


        //========================================================
        // 与 Span 的互转换

        UnManagedMemory<char> mem_0_1 = "abcdefg";

        Span<char> span_0 = mem_0_1.AsSpan();

        UnManagedMemory<char> mem_0_2 = span_0.MapToUnManagedMemory(); //映射

        UnManagedMemory<char> mem_0_3 = span_0.CopyToUnManagedMemory(); //复制

        mem_0_3.Dispose();

        //========================================================
        //==== 自分配栈内存

        UnManagedMemory<char> mem_1 = new UnManagedMemory<char>(10, 0); //分配容量10，当前使用0

        mem_1.AddRange("0123456");

        bool mem_1_cap = mem_1.Capacity == 10;

        bool mem_1_usage = mem_1.UsageSize == 7; //当前已经使用的容量是7

        mem_1.Dispose();


        //=========================================================
        //==== 映射外部内存

        char* mem_2_stack = stackalloc char[10];

        UnManagedMemory<char> mem_2 = new UnManagedMemory<char>(mem_2_stack, 10, 0, MemoryTypeDefined.Stack);

        mem_2.AddRange("123");

        bool mem_2_usage = mem_2.UsageSize == 3;

        //mem_2.Dispose(); //此时 mem_2 映射外部内存，不需要做 Dispose(), 如果强制做 Dispose() 不会报错，但步骤是多余的。

        //==========================================================
        //==== 映射外部内存，填充值

        char* mem_3_stack = stackalloc char[10];

        UnManagedMemory<char> mem_3 = new UnManagedMemory<char>(mem_3_stack, "123456"); //必须知晓指针指向的容量一定足够容纳“123456”

        bool mem_3_usage = mem_2.UsageSize == 6;

        //mem_3.Dispose(); //此时 mem_3 映射外部内存，不需要做 Dispose(), 如果强制做 Dispose() 不会报错，但步骤是多余的。
    }

    /// <summary>
    /// 常规使用
    /// </summary>
    public static void CommonUsage()
    {
        //注意事项： 当前例子中的空对象会默认允许使用动态分配。
        //如果是外部内存映射模式，容量是永远不会被改变的，值的变动仅限于容量范围内。

        UnManagedMemory<char> mem = new UnManagedMemory<char>();

        bool mem_cap = mem.Capacity == 0; //容量

        bool mem_size = mem.UsageSize == 0; //已经使用长度

        mem.AddRange("abcd");

        mem_cap = mem.Capacity == 4; //容量发生改变

        mem_size = mem.UsageSize == 4; //使用长度发生改变

        bool equals = mem.Equals("abcd");

        mem.Add('e');  // 值变成 abcde

        equals = mem.Equals("abcde");

        mem_cap = mem.Capacity == 5; //容量发生改变

        mem_size = mem.UsageSize == 5; //使用长度发生改变

        mem.RemoveAt(4); // 移除了最后一个字符'e',值变成 abcd

        mem_cap = mem.Capacity == 5; //容量不会缩减

        mem_size = mem.UsageSize == 4; //使用长度缩减

        equals = mem.Equals("abcd");

        mem.AddRange("123456abcd123456"); // 值变成 abcd123456abcd123456

        mem_cap = mem.Capacity == 20; //容量发生改变

        mem_size = mem.UsageSize == 20; //使用长度发生改变

        bool start = mem.StartsWith("ab");

        bool index = mem.IndexOf("d1") == 3;

        bool lastindexof = mem.LastIndexOf("d1") == 13;

        mem.RemoveRange(8, 7, true); // 移除 56abcd1 ，并且压缩容量

        equals = mem.Equals("abcd123423456");

        mem_cap = mem.Capacity == 13; // 因为指定了压缩容量，容量已经在执行 RemoveRange 发生改变

        mem_size = mem.UsageSize == 13; // 使用长度发生改变

        mem.Replace("123423456", "");

        equals = mem.Equals("abcd");

        mem_cap = mem.Capacity == 13; // 容量依然是 13

        mem_size = mem.UsageSize == 4;

        mem.Resize(4); //手动把容量压缩到 4

        mem_cap = mem.Capacity == 4; // 容量被压缩到 4

        mem_size = mem.UsageSize == 4;


        mem.Dispose();
    }

}