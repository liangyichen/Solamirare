


/// <summary>
/// UnManagedMemory 高级用法
/// </summary>
public static unsafe class AdvanceSample_UnManagedMemory
{
    /// <summary>
    /// foreach 遍历与 ForEach 函数指针遍历
    /// </summary>
    public static void Sample1()
    {
        UnManagedMemory<char> chars = new UnManagedMemory<char>("abcdefg");

        int transfer_source = 100;


        //使用ForEach接受函数指针进行迭代，可以减少内存分配次数
        chars.ForEach(&foreach_method, &transfer_source);


        bool transfer_changed = transfer_source == 200; //已经在函数指针内部改变了最初的值


        // 效果等同于以下是用 foreach

        foreach (char* c in chars)
        {
            // 使用 foreach 会更加方便，但是迭代过程中会涉及到构造枚举器等操作，会进行更多的内存分配与释放、占用更多的cpu资源
        }

        chars.Dispose();
    }

    static bool foreach_method(int index, char* c, void* arg)
    {
        int* transfer = (int*)arg; //可以接受任意内存段指针， 这里是外部传入的 transfer_source

        int i = index; //当前迭代的次数

        char* item_c = c; //当前迭代数据

        //----------------
        *transfer = 200;  //外部数据源会被改变为200

        return true; //是否有必要继续下一轮迭代
    }


    /// <summary>
    /// 等同比较
    /// </summary>
    public static void Sample2()
    {
        UnManagedMemory<char> chars = new UnManagedMemory<char>("abcdefg");

        chars.Equals("abcdefg"); //true

        chars.Equals("Abcdefg"); //false

        chars.SequenceEqualIgnoreCase("Abcdefg"); //true

        chars.Dispose();
    }


    /// <summary>
    /// 状态码监控当前对象的状态
    /// </summary>
    public static void Sample3()
    {
        UnManagedMemory<char> chars = new UnManagedMemory<char>("abcdefg");

        MemoryFingerprint128 status = chars.StatusCode;

        chars.Add('h');

        bool changed = status != chars.StatusCode; //对象内部状态已经改变

        status = chars.StatusCode;

        chars.Update("Abcdefgh");

        changed = status == chars.StatusCode; //更新了间接指向内存的值（a->A），但是当前对象的各个属性没有发生变动，所以状态码不会变动

        chars.Dispose();
    }


    /// <summary>
    /// 哈希码用于监控指向内存的内容变化
    /// </summary>
    public static void Sample4()
    {
        UnManagedMemory<char> chars = new UnManagedMemory<char>(20, 0);

        chars.AddRange("abcdefg"); //这时候容量是20，使用长度是7

        int hash = chars.GetHashCode();

        chars.EnsureCapacity(100); //容量已经改变，对象本身的状态已经改变

        bool changed = hash == chars.GetHashCode(); //指向内容没变，哈希码也不会改变

        chars.SetValue(3, 'M'); //对象本身没有改变，指向内容发生改变

        changed = hash != chars.GetHashCode(); //哈希码发生改变


        chars.Dispose();
    }


    /// <summary>
    /// 映射模式
    /// </summary>
    public static void Sample5()
    {
        UnManagedMemory<char> chars = new UnManagedMemory<char>("abcdefg");

        UnManagedMemory<char> sliceOBJ = chars.Slice(1, 3); // bcd

        sliceOBJ.SetValue(0, 'B'); //映射模式本身不分配内存，而是指向别的内存段

        bool validate = chars.Equals("aBcdefg"); //原始内容已经被改变

        //sliceOBJ.Dispose(); // <--- 这是多余的，明确当前对象为映射模式的情况下不需要进行 Dispose()

        chars.Dispose();
    }



    /// <summary>
    /// 字节序列转换
    /// </summary>
    public static void Sample6()
    {
        UnManagedMemory<char> chars = new UnManagedMemory<char>("abcdefg");

        UnManagedMemory<byte> bytes = chars.CopyToBytes();

        UnManagedMemory<char> restore = bytes.CopyToChars();

        bool validate = restore.Equals(chars); //true

        bytes.Dispose();
        restore.Dispose();
        chars.Dispose();
    }


    /// <summary>
    /// 外部内存模式
    /// </summary>
    public static void Sample7()
    {
        char* mem = stackalloc char[7];

        UnManagedMemory<char> chars = new UnManagedMemory<char>(mem, 7, 0);

        bool append_abc = chars.AddRange("abc");

        bool append_defg = chars.AddRange("defg");

        bool append_Failure = !chars.Add('h'); //已经用满了，禁止扩容

    }


    /// <summary>
    /// 转换形态，实现内存段的重用
    /// </summary>
    public static void Sample8()
    {
        UnManagedMemory<int> mem = new UnManagedMemory<int>(32, 0);

        UnManagedMemory<char> mem_chars = mem.Transformance<char>();

        bool mem_chars_size = mem_chars.Capacity == 64; //int占据4字节，char占据2字节，容量相差翻倍，变形后的容量可以容纳 32 * 2 = 64

        UnManagedMemory<int> mem_int_ori = mem_chars.Transformance<int>();

        bool mem_int_ori_cap = mem_int_ori.Capacity == 32; //再次转换回int, 依旧可以使用32容量

        mem_chars.Dispose(); //无效
        mem_int_ori.Dispose(); //无效

        mem.Dispose(); //必须在原始的对象上释放
    }


    /// <summary>
    /// 局部插入
    /// </summary>
    public static void Sample9()
    {
        UnManagedMemory<char> mem = new UnManagedMemory<char>("123456");

        mem.Insert(2, '0'); // 1203456

        mem.InsertRange(2, "00"); //12000456

        bool validate = mem.Equals("12000456");

        mem.Dispose(); //必须在原始的对象上释放
    }


    /// <summary>
    /// Zero 与 Reset，两种清空方法的区别
    /// </summary>
    public static void Sample10()
    {
        UnManagedMemory<char> mem = new UnManagedMemory<char>("123456");

        mem.Zero();

        bool validate = mem.IsEmpty; //Zero 会执行下标归0，等同于清零

        mem.AddRange("123456");

        mem.Clear();

        validate = mem.UsageSize == 6 && *mem[0] == 0 && *mem[5] == 0; //Clear不会改变下标，但是会执行内容清零

        mem.Dispose(); //必须在原始的对象上释放
    }


    /// <summary>
    /// 对象本身的再次构造，目的是保持对象的内存地址不变
    /// </summary>
    /// <returns></returns>
    public static bool ResetAndReAlloc()
    {
        UnManagedMemory<char>* mem = stackalloc UnManagedMemory<char>[1];

        mem->Init("123456"); //初次执行构造函数不受之前是否进行Dispose的限制

        mem->Dispose();

        mem->Init("abcdef"); //再次执行构造函数，必须在之前执行 Dispose()，否则无效

        bool validate = mem->Equals("abcdef");

        mem->Init("xyz"); //企图在没有执行 Dispose 的情况下再次手动构造

        validate = mem->Equals("abcdef"); //依然是旧值，上面的手动构造函数无效

        mem->Dispose();

        return validate;
    }
}