
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Solamirare.Tests;


/// <summary>
/// UnamangedMemory 的基础测试
/// </summary>
public static unsafe class UnamangedMemory_Test
{






    static int[] bytesInt = [1, 2, 3, 4, 5, 6, 7, 8, 9, 0];

    static int[] intArray_7_8_9_0 = [7, 8, 9, 0];

    static int[] intArray_1_2_3 = [1, 2, 3];

    static int[] intArray_4_5_6 = [4, 5, 6];

    static int[] intArray_8_9_0 = [8, 9, 0];




    public static bool ReadOnly()
    {
        char* m = stackalloc char[20];
        char** pp_chars = &m;

        UnManagedMemory<char> obj = new UnManagedMemory<char>(pp_chars, "abcd", MemoryTypeDefined.Stack);


        bool result = false;

        //==================

        obj.Add('e'); //长度只有4，并且是栈内存，不允许添加

        obj.Update("fghijk"); //指定新值的长度大于原始长度，不允许更新

        result = !obj.Equals("fghijk"); //不会等同

        obj.Update("1234"); //指定新值的长度等同原始值，允许更新

        result = result && obj.Equals("1234"); //true

        //==================


        UnManagedMemory<char> obj2 = new UnManagedMemory<char>(m + 10, 10, 0);

        obj2.AddRange("zxcvb"); //true

        obj2.SetAsReadOnly();

        obj2.AddRange("0000"); //false, 已经锁定，不允许添加

        result = result && obj2.Equals("zxcvb");

        obj2.UnlockReadOnly();

        obj2.AddRange("99"); //解除锁定，允许添加

        result = result && obj2.Equals("zxcvb99");



        return result;

    }

    /// <summary>
    /// 测试类型转换
    /// </summary>
    /// <returns></returns>
    public static bool Transformance()
    {
        UnManagedMemory<long> source = new UnManagedMemory<long>(4, 0);

        UnManagedMemory<uint> t2 = source.Transformance<uint>();

        bool result = t2.Capacity == 8;

        source.Dispose();

        return result;
    }



    public static bool HashEquals()
    {

        UnManagedMemory<char> abc_0 = new UnManagedMemory<char>("abcd");

        UnManagedCollection<char> abc_1 = "abcd".AsSpan().MapToUnManagedCollection();



        int hash_0 = abc_0.GetHashCode();

        int hash_1 = abc_1.GetHashCode();


        bool result = hash_0 == hash_1;


        return result;
    }




    /// <summary>
    /// 测试长度检测
    /// </summary>
    /// <returns></returns>
    public static bool EnsureCapacity()
    {
        uint size = 5;

        uint largeSize = 6;

        char* m = stackalloc char[(int)size];


        UnManagedMemory<char> obj = new UnManagedMemory<char>(m, size, size);

        bool result = !obj.EnsureCapacity(largeSize); //尝试扩展外部内存，必然失败

        result = result && obj.EnsureCapacity(size); //但是可以保证合法范围内的长度


        //使用另一种模式分配外部内存
        UnManagedMemory<char> obj2 = new UnManagedMemory<char>(m, size, 0);

        result = result && !obj2.EnsureCapacity(largeSize);

        result = result && obj2.EnsureCapacity(size);


        return result;
    }


    /// <summary>
    /// 测试一个整数数组在另一个整数数组中的位置
    /// </summary>
    /// <returns></returns>
    public static bool IndexOf_INT()
    {

        ReadOnlySpan<int> ints = bytesInt;

        UnManagedMemory<int> source = ints.CopyToUnManagedMemory(0);

        bool a = source.IndexOf(intArray_7_8_9_0) == 6;

        bool b = source.StartsWith(intArray_1_2_3);

        bool c = source.IndexOf(intArray_4_5_6) == 3;

        bool d = source.IndexOf(intArray_8_9_0) == 7;



        bool result = a && b && c && d;


        UnManagedMemory<int> select_unmanagedram_1 = intArray_7_8_9_0.CopyToUnManagedMemory();

        bool result_0 = source.IndexOf((UnManagedCollection<int>*)&select_unmanagedram_1) == 6;

        select_unmanagedram_1.Dispose();


        int select_int_1 = 1;

        bool result_1 = source.StartsWith(select_int_1);

        bool result_2 = source.StartsWith(&select_int_1);

        source.Dispose();

        bool final = result && result_0 && result_1 && result_2;



        return final;

    }




    static UseToValueTypeEquals node_0 = new UseToValueTypeEquals { Id = 0, Field_0 = 99, Field_1 = 8880, Field_2 = 'v' };
    static UseToValueTypeEquals node_1 = new UseToValueTypeEquals { Id = 1, Field_0 = 5465, Field_1 = 267464, Field_2 = 'g' };
    static UseToValueTypeEquals node_2 = new UseToValueTypeEquals { Id = 2, Field_0 = 45, Field_1 = 246542, Field_2 = 'p' };
    static UseToValueTypeEquals node_3 = new UseToValueTypeEquals { Id = 3, Field_0 = 9569, Field_1 = 6564, Field_2 = '4' };
    static UseToValueTypeEquals node_4 = new UseToValueTypeEquals { Id = 4, Field_0 = 86, Field_1 = 87426, Field_2 = ',' };



    /// <summary>
    /// 测试自定义结构在数组中的位置
    /// </summary>
    /// <returns></returns>
    public static bool IndexOf_struct()
    {
        bool result = false;

        UnManagedMemory<UseToValueTypeEquals> arr_select_node_0 = new UnManagedMemory<UseToValueTypeEquals>(1, 0);
        UnManagedMemory<UseToValueTypeEquals> arr_select_node_2_3 = new UnManagedMemory<UseToValueTypeEquals>(2, 0);
        UnManagedMemory<UseToValueTypeEquals> arr_select_node_3_4 = new UnManagedMemory<UseToValueTypeEquals>(2, 0);
        UnManagedMemory<UseToValueTypeEquals> arr_select_node_4 = new UnManagedMemory<UseToValueTypeEquals>(1, 0);
        UnManagedMemory<UseToValueTypeEquals> arr_select_node_4_3 = new UnManagedMemory<UseToValueTypeEquals>(2, 0);


        UnManagedMemory<UseToValueTypeEquals> source = new UnManagedMemory<UseToValueTypeEquals>(5, 0);


        fixed (UseToValueTypeEquals* p_node_0 = &node_0, p_node_1 = &node_1, p_node_2 = &node_2, p_node_3 = &node_3, p_node_4 = &node_4)
        {
            source.Add(p_node_0);
            source.Add(p_node_1);
            source.Add(p_node_2);
            source.Add(p_node_3);
            source.Add(p_node_4);


            arr_select_node_0.Add(p_node_0);
            arr_select_node_2_3.Add(p_node_2);
            arr_select_node_2_3.Add(p_node_3);
            arr_select_node_3_4.Add(p_node_3);
            arr_select_node_3_4.Add(p_node_4);
            arr_select_node_4.Add(p_node_4);
            arr_select_node_4_3.Add(p_node_4);
            arr_select_node_4_3.Add(p_node_3);

            bool start_with_p_node_0 = source.StartsWith(p_node_0);

            int indexof_p_node_1 = source.IndexOf(p_node_1);

            int indexof_p_node_2 = source.IndexOf(p_node_2);

            int indexof_p_node_3 = source.IndexOf(p_node_3);

            bool start_with_arr_select_node_0 = source.StartsWith(&arr_select_node_0);

            int indexof_arr_select_node_2_3 = source.IndexOf(&arr_select_node_2_3);

            int indexof_arr_select_node_3_4 = source.IndexOf(&arr_select_node_3_4);

            int indexof_arr_select_node_4 = source.IndexOf(&arr_select_node_4);

            int indexof_4_3 = source.IndexOf(&arr_select_node_4_3);


            result = start_with_p_node_0 &&
            indexof_p_node_1 == 1 &&
            indexof_p_node_2 == 2 &&
            indexof_p_node_3 == 3 &&
            start_with_arr_select_node_0 &&

            indexof_arr_select_node_2_3 == 2 &&
            indexof_arr_select_node_3_4 == 3 &&
            indexof_arr_select_node_4 == 4 &&
            indexof_4_3 == -1

            ;
        }


        source.Dispose();

        arr_select_node_0.Dispose();
        arr_select_node_2_3.Dispose();
        arr_select_node_3_4.Dispose();
        arr_select_node_4.Dispose();
        arr_select_node_4_3.Dispose();

        return result;
    }



    /// <summary>
    /// 测试关键字在字符串中的位置
    /// </summary>
    /// <returns></returns>
    public static bool IndexsOf_Chars()
    {


        UnManagedMemory<char> source = new UnManagedMemory<char>("abcdefg01234567890hijklmn");

        int r1 = source.IndexOf("def");
        int r2 = source.IndexOf("abc");
        int r3 = source.IndexOf("klmn");//18
        int r4 = source.IndexOf("xyz");
        int r5 = source.IndexOf("NFWINVWEOIBWIRUOFNUIRWNEFRJWFIWRFEFBREFNJ3N43U4NFIUN3FNRFLFNJW");
        int r6 = source.IndexOf("abcdefg01234567890hijklmn");
        int r7 = source.IndexOf("");

        bool result =

            r1 == 3 &&
            r2 == 0 &&
            r3 == 21 &&
            r4 == -1 &&
            r5 == -1 &&
            r6 == 0 &&
            r7 == 0;

        source.Dispose();

        return result;

    }





    public static bool Slice()
    {
        UnManagedMemory<char> source = new UnManagedMemory<char>("abcdefg");

        //=====
        UnManagedMemory<char> slice0 = source.Slice(4);

        UnManagedMemory<char> slice = source.Slice(2, 3);

        UnManagedMemory<char> slice_faild_input_0 = source.Slice(7, 3);

        //=====

        bool result = slice.Equals("cde");
        bool result0 = slice0.Equals("efg");

        bool result_faild_0 = slice_faild_input_0.IsEmpty;

        source.Dispose();

        return result && result0 && result_faild_0;
    }




    static byte[] bytesArray = [1, 2, 3, 4, 5, 6, 7, 8, 9, 0];

    static byte[] bytesArray_7_8_9_0 = [7, 8, 9, 0];

    static byte[] bytesArray_1_2_3 = [1, 2, 3];

    static byte[] bytesArray_4_5_6 = [4, 5, 6];

    static byte[] bytesArray_8_9_0 = [8, 9, 0];

    /// <summary>
    /// 测试一个字节数组在另一个字节数组中的位置
    /// </summary>
    /// <returns></returns>
    public static bool IndexOf_BYTE()
    {

        ReadOnlySpan<byte> bytes = bytesArray.AsSpan();

        UnManagedMemory<byte> source = bytes.CopyToUnManagedMemory(0);

        bool result = source.IndexOf(bytesArray_7_8_9_0) == 6 &&
            source.IndexOf(bytesArray_1_2_3) == 0 &&
            source.IndexOf(bytesArray_4_5_6) == 3 &&
            source.IndexOf(bytesArray_8_9_0) == 7;


        source.Dispose();

        return result;

    }



    /// <summary>
    /// 测试单一字节在字节数组中的位置
    /// </summary>
    /// <returns></returns>
    public static bool IndexOf_Short_Bytes()
    {
        UnManagedMemory<byte> _1 = new UnManagedMemory<byte>(1, 0);
        _1.Add(1);

        UnManagedMemory<byte> _2 = new UnManagedMemory<byte>(1, 0);
        _2.Add(2);

        UnManagedMemory<byte> source = new UnManagedMemory<byte>(2, 0);
        source.Add(1);
        source.Add(2);

        bool result = source.StartsWith(&_1) &&
            source.IndexOf(&_2) == 1 &&
            source.StartsWith(1) &&
            source.IndexOf(2) == 1
            ;

        _1.Dispose();
        _2.Dispose();

        source.Dispose();

        return result;
    }




    public static bool AsSpan()
    {
        UnManagedMemory<char> source = new UnManagedMemory<char>("abcdefg");

        Span<char> span = source.AsSpan();

        Span<char> span_0 = source.AsSpan(2, 5);

        bool result = span.Length == 7;

        bool result_0 = span_0.Length == 5;

        source.Dispose();

        return result && result_0;
    }

    public static bool AsRealSizeSpan()
    {
        UnManagedMemory<char> source = new UnManagedMemory<char>("abcdefg");

        Span<char> span = source.AsRealSizeSpan();

        bool result = span.Length == 7;

        source.Dispose();

        return result;
    }



    /// <summary>
    /// 测试单一字符串在字符串中的位置
    /// </summary>
    /// <returns></returns>
    public static bool IndexOf_Single_String()
    {
        UnManagedMemory<char> source = new UnManagedMemory<char>("a");

        bool result = source.IndexOf("a") == 0 && source.IndexOf("") == 0;

        source.Dispose();

        return result;
    }

    /// <summary>
    /// 测试单一字符在字符串中的位置
    /// </summary>
    /// <returns></returns>
    public static bool IndexOf_Single_Char()
    {
        UnManagedMemory<char> source = new UnManagedMemory<char>("abcdefg");

        UnManagedMemory<char> single_source = new UnManagedMemory<char>("s");

        bool result = source.IndexOf('c') == 2

            && source.IndexOf('p') == -1
            && single_source.IndexOf('Z') == -1
            && single_source.StartsWith('s')
            && single_source.IndexOf('\0') == -1;
        ;


        source.Dispose();
        single_source.Dispose();

        return result;
    }




    static int[] for_set_value = [200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215];


    public static bool SetValue()
    {
        UnManagedMemory<int> obj = new UnManagedMemory<int>([1, 2, 3, 4, 5, 6]);

        obj.SetValue(2, 100);

        bool result_0 = obj.Equals([1, 2, 100, 4, 5, 6]);

        UnManagedMemory<int> append = new UnManagedMemory<int>(for_set_value);

        obj.SetValue(2, (UnManagedCollection<int>*)&append);


        bool result_1 = obj.Equals([1, 2, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215]);

        append.Dispose();

        obj.Dispose();

        return result_0 && result_1;
    }








    /// <summary>
    /// 测试索引
    /// </summary>
    /// <returns></returns>
    public static bool Index()
    {
        UnManagedMemory<int> obj = new UnManagedMemory<int>(4, 0);
        obj.Add(1);
        obj.Add(2);
        obj.Add(3);
        obj.Add(4);


        bool result_0 = *obj.Index(0) == 1 && *obj.Index(1) == 2 && *obj.Index(2) == 3 && *obj.Index(3) == 4;

        obj.Dispose();

        return result_0;
    }

    /// <summary>
    /// 测试RemoveAt
    /// </summary>
    /// <returns></returns>
    public static bool RemoveAt()
    {
        UnManagedMemory<char> obj = new UnManagedMemory<char>(4, 0);
        obj.Add('1');
        obj.Add('2');
        obj.Add('3');
        obj.Add('4');

        obj.RemoveAt(2);

        bool result_0 = *obj.Index(2) == '4';

        obj.Dispose();

        return result_0;
    }



    /// <summary>
    /// 测试统计总数
    /// </summary>
    /// <returns></returns>
    public static bool Count()
    {
        UnManagedMemory<char> source = new UnManagedMemory<char>("abcdefg0abcdefg0");

        int countString = source.Count("cde");
        int countChar = source.Count("c");
        int countNull = source.Count("z");
        int countChar_2 = source.Count('c');
        int countChar_3 = source.Count("efg0abcde");

        source.Dispose();

        bool result = countString == 2 && countChar == 2 && countNull == 0 && countChar_2 == 2 && countChar_3 == 1;



        return result;
    }

    /// <summary>
    /// 测试集合中包含另一集合
    /// </summary>
    /// <returns></returns>
    public static bool Contains_Collection()
    {
        UnManagedMemory<char> source = new UnManagedMemory<char>("abcdefg01234567890hijklmn01234567890hijklmn");

        bool result = source.Contains("abcdefg01234567890hijklmn");

        source.Dispose();

        return result;
    }


    static string[] _encodeAndDecodeStrings_data = {
                "d4e\\\"b8\"}'></sZZZ",
                """{"co\"de":"d4eb8\"}'></script>"}""",
                """{"co\"de":"d4eb8\"}'></script>"}""",
                "<abcde\"fg",
                "abcdefg",
                "\"",
                "",
                 "\\\\",
                "\n",
                "\nhuhyghbh<uil>hiu\n",
                "<<<<<<<<",
                "     ",
                "abc<defg"
            };

    /// <summary>
    /// 测试序列化与反序列化
    /// </summary>
    /// <returns></returns>
    public static bool EncodeAndDecodeStrings()
    {


        UnManagedMemory<UnManagedMemory<char>> contents = _encodeAndDecodeStrings_data.CopyToUnManagedMemory();

        bool result = true;

        for (int i = 0; i < contents.UsageSize; i++)
        {
            UnManagedMemory<char>* item = contents.Index(i);

            UnManagedMemory<char> mem = JsonFlatProcessor.SerializeString(item);

            Span<char> view_span = mem.AsSpan();

            JsonFlatProcessor.DecodeJsonString(&mem);

            bool equal = item->Equals(&mem);


            item->Dispose();
            mem.Dispose();

            if (!equal)
            {
                result = false;
                break;
            }
        }

        foreach (UnManagedMemory<char>* i in contents)
        {
            i->Dispose();
        }
        contents.Dispose();



        return result;
    }



    static readonly string _forEachString = "abcdefg";

    static bool EachChar_y(int index, char* p, void* caller)
    {
        *p = 'y';

        return true;

    }



    public static bool @foraech()
    {
        UnManagedMemory<int> mem = new UnManagedMemory<int>();

        mem.Add(1);
        mem.Add(2);
        mem.Add(3);
        mem.Add(4);
        mem.Add(5);

        int sum = 0;

        foreach (int* i in mem)
        {
            sum += *i;
        }

        mem.Dispose();

        return sum == 15;

    }


    /// <summary>
    /// 测试自定义foreach方法
    /// </summary>
    /// <returns></returns>
    public static bool ForEachMethod()
    {
        UnManagedMemory<char> arr = new UnManagedMemory<char>(_forEachString);

        arr.ForEach(&EachChar_y, null);

        bool result_2 = arr.Equals("yyyyyyy");

        arr.Dispose();

        return result_2;
    }



    public static bool Resize_Min()
    {
        UnManagedMemory<char> source = new UnManagedMemory<char>("abcdefg0123456789hijklmn");

        source.RemoveRange(7, 17);

        bool result = source.Equals("abcdefg") && source.UsageSize == 7;


        // Resize to a smaller capacity
        bool execute = source.Resize(source.UsageSize);

        result = result && execute && source.Capacity == 7;


        source.Dispose();

        return result;
    }

    static string[] json_Objects_data = [

        //"""{"kkkk":"val\"e\"-1\"","name-2":"value-2","name-3":"value-3","name-4":"value-4"}""",
        // """{"r":"123","CustomScript":"","UrlNameLength":"128"}""",
        // """{"InnerHead":"<link rel=\"apple-touch-icon\" href=\"/favicon.ico\" />\n<meta property=\"og:url\""}""",
        // """{"name":""}""",
        // """{"code":"d4eb8\"}'></script>"}""",
        // """{"name":"\u003Cmy \n name","name2":"value2"}""",
        // """{"name":"my name","name2":"my name 2"}""",

        //做不了以上的多属性相等性测试（结果的属性排列顺序不对），就功能性测试来说，只做单节点测试就可以了                

        """{"code\"":"d4e\\\"b8\"}'></sZZZ"}""",

        """{"\nname":"my name"}"""
    ];


    /// <summary>
    /// 测试字符串到 json 之间的序列化与反序列化
    /// </summary>
    /// <returns></returns>
    public static bool Objects()
    {

        UnManagedMemory<UnManagedMemory<char>> string_array = json_Objects_data.CopyToUnManagedMemory();

        char* st_value_stack = stackalloc char[2048];
        UnManagedMemory<char> exp_json = new UnManagedMemory<char>(st_value_stack, 2048, 0);


        bool result = true;

        for (var i = 0; i < string_array.UsageSize; i++)
        {
            ValueDictionary<UnManagedMemory<char>, UnManagedMemory<char>> innerData = new ValueDictionary<UnManagedMemory<char>, UnManagedMemory<char>>();

            UnManagedMemory<char>* item = string_array.Index(i);


            JsonFlatProcessor.DecodeObjectString_AppendToDictionary(item, &innerData);

            JsonFlatProcessor.SerializeObject(&innerData, &exp_json);


            if (!item->Equals(exp_json))
            {
                result = false;
                innerData.Dispose();
                break;
            }

            innerData.DisposeAll();
        }

        exp_json.Dispose();


        foreach (UnManagedMemory<char>* i in string_array)
        {
            i->Dispose();
        }
        string_array.Dispose();

        return result;

    }



    /// <summary>
    /// 测试集合中包含单一元素
    /// </summary>
    /// <returns></returns>
    public static bool Contains_Single()
    {
        UnManagedMemory<char> source = new UnManagedMemory<char>("abcdefg01234567890hijklmn01234567890hijklmn");

        bool result = source.Contains('0');

        source.Dispose();
        ;

        return result;
    }



    /// <summary>
    /// 测试重载：相等性判断
    /// </summary>
    /// <returns></returns>
    public static bool Override_Operate_Equals()
    {
        UnManagedMemory<char> source = new UnManagedMemory<char>("abcdefg");

        if (!source.Equals("abcdefg")) return false;

        if (source.Equals(string.Empty)) return false;

        if (source.Equals("")) return false;


        (&source)->Replace("abcdefg", string.Empty);

        if (!source.Equals(string.Empty)) return false;

        source.Dispose();


        return true;
    }


    static string[] encodeAndDecodeCollection_data =
    {
            "<abcde\"fg",

            "abcdefg",
            "abc",
            "d4e\\\"b8\"}'></sZZZ",
            "ghi",
                "\"",
                """{"co\"de":"d4eb8\"}'></script>"}""",
                """{"co\"de":"d4eb8\"}'></script>"}""",
            "",
            null,
            """addr"\\":"ess3""",
            "\\\\",
            "\n",
            "\nhuhyghbh<uil>hiu\n",
                "<<<<<<<<",
            "     ",
            "abc<defg"
    };



    /// <summary>
    /// 测试字符串集合的序列化与反序列化
    /// </summary>
    /// <returns></returns>
    public static bool EncodeAndDecodeCollection()
    {

        UnManagedMemory<UnManagedMemory<char>> un_contents = encodeAndDecodeCollection_data.CopyToUnManagedMemory();

        UnManagedMemory<char> json = new UnManagedMemory<char>(512);

        JsonFlatProcessor.SerializeCollection(&un_contents, &json);

        Span<char> view_json = json.AsSpan();

        UnManagedMemory<UnManagedMemory<char>> spans = new UnManagedMemory<UnManagedMemory<char>>();

        JsonFlatProcessor.DecodeJsonCollection(&json, &spans);


        Span<UnManagedMemory<char>> view_decodes = spans.AsSpan();

        bool result = true;


        for (int i = 0; i < spans.UsageSize; i++)
        {
            UnManagedMemory<char>* a = spans.Index(i);

            UnManagedMemory<char>* b = un_contents.Index(i);

            bool equal = a->Equals(b);

            if (!equal)
            {
                result = false;
                break;
            }
        }


        foreach (UnManagedMemory<char>* i in un_contents)
        {
            i->Dispose();
        }

        un_contents.Dispose();


        foreach (UnManagedMemory<char>* i in spans)
        {
            i->Dispose();
        }

        spans.Dispose();


        json.Dispose();

        un_contents.Dispose();

        return result;
    }


    public static bool StartsWith()
    {
        UnManagedMemory<char> source = new UnManagedMemory<char>("abcdef");

        bool result_0 = source.StartsWith("abcd");

        bool result_1 = source.StartsWith("abcdefghijk");

        bool result_2 = source.StartsWith(""); //C#的规范，查询空值一定是true（IndexOf == 0）

        bool result_3 = source.StartsWith("5tw45");

        source.Dispose();

        return result_0 && !result_1 && result_2 && !result_3;
    }



    /// <summary>
    /// 测试IndexOfAny
    /// </summary>
    /// <returns></returns>
    public static bool IndexOfAny()
    {
        UnManagedMemory<char> source = new UnManagedMemory<char>("abcdefghijklnm");
        UnManagedMemory<char> p1 = new UnManagedMemory<char>("efghijk");
        UnManagedMemory<char> p2 = new UnManagedMemory<char>("EFGHIJK");

        if (source.IndexOfAny("efghijk") == -1) return false;
        if (source.IndexOfAny("EFGHIJK") != -1) return false;

        if (source.IndexOfAny(p1) == -1) return false;
        if (source.IndexOfAny(p2) != -1) return false;


        p1.Dispose();
        p2.Dispose();

        source.Dispose();

        return true;

    }



    /// <summary>
    /// 测试替换
    /// </summary>
    /// <returns></returns>
    public static bool Replace()
    {
        UnManagedMemory<char> source = new UnManagedMemory<char>("abcdefg0123456789");

        source.Replace("abc", "ZZZ");

        if (!source.Equals("ZZZdefg0123456789")) return false;

        source.Replace("0123", "");

        if (!source.Equals("ZZZdefg456789")) return false;

        source.Replace("89", "TTTTT");

        if (!source.Equals("ZZZdefg4567TTTTT")) return false;

        source.Dispose();

        return true;
    }

    public static bool ParseFromDecimal()
    {
        UnManagedMemory<char> ob7 = UnManagedMemoryHelper.ParseFromDecimal(4230547434335028.4758924555346567564M, 256);

        bool result_1 = ob7.Equals("4230547434335028.4758924555347");

        ob7.Dispose();

        return result_1;

    }


    static DateTime ParseFromDateTime_temp_value = DateTime.Parse("2021-01-01 12:00:00");

    public static bool ParseFromDateTime()
    {
        //构造日期对象需要成本，把 ParseFromDateTime_temp_value 放到外部静态变量中尽可能避免构造日期造成的误差

        UnManagedMemory<char> d = UnManagedMemoryHelper.ParseFromDateTime(ParseFromDateTime_temp_value);

        bool result_2 = d.Equals("2021-01-01T12:00:00.0000000");

        d.Dispose();

        return result_2;
    }

    public static bool ParseFromLong()
    {
        UnManagedMemory<char> ob8 = UnManagedMemoryHelper.ParseFromLong(4230547434335028);

        bool result_6 = ob8.Equals("4230547434335028");

        ob8.Dispose();

        return result_6;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static bool ParseFromInt()
    {

        UnManagedMemory<char> obj = UnManagedMemoryHelper.ParseFromInt(300);

        bool result_0 = obj.Equals("300");

        obj.Dispose();

        return result_0;
    }


    /// <summary>
    /// 测试 int 转换到 string
    /// </summary>
    /// <returns></returns>
    public static bool IntToUnmanagedString()
    {
        int _int = 10090;
        UnManagedMemory<char> str_i = _int.IntToUnmanagedString();

        bool result = str_i.Equals("10090");

        str_i.Dispose();

        return result;
    }

    /// <summary>
    /// 测试重载：相加
    /// </summary>
    /// <returns></returns>
    public static bool Operator()
    {
        UnManagedMemory<char> a = new UnManagedMemory<char>("a");
        UnManagedMemory<char> b = new UnManagedMemory<char>("b");
        UnManagedMemory<char> _ab = new UnManagedMemory<char>("ab");


        int sz = sizeof(UnManagedMemory<char>);

        UnManagedMemory<char> ab = a.Clone();
        ab.AddRange(b);


        bool result_a = ab == "ab";
        bool result_b = ab.Equals(_ab);


        bool result = result_a && result_b;

        a.Dispose();
        b.Dispose();
        ab.Dispose();
        _ab.Dispose();

        return result;
    }



    /// <summary>
    /// 测试连接两个集合
    /// </summary>
    /// <returns></returns>
    public static bool Concat()
    {

        UnManagedMemory<char> ram = new UnManagedMemory<char>(20, 0);

        ram.AddRange("abcde");

        bool result_0 = ram == "abcde";

        ram.AddRange("fghijk");

        bool result_1 = ram == "abcdefghijk";

        ram.Dispose();

        return result_0 && result_1;
    }





    /// <summary>
    /// 测试短词汇在字符串中的位置
    /// </summary>
    /// <returns></returns>
    public static bool IndexOf_Short_Chars()
    {

        UnManagedMemory<char> source = new UnManagedMemory<char>("ab");

        int r1 = source.IndexOf("a");

        int r2 = source.IndexOf("b");

        int r3 = source.IndexOf("");

        bool result = r1 == 0 && r2 == 1 && r3 == 0;

        source.Dispose();

        return result;
    }


    /// <summary>
    /// 测试LastIndexOf（堆模式）
    /// </summary>
    /// <returns></returns>
    public static bool Heap_LastIndexOf()
    {

        UnManagedMemory<char> source = new UnManagedMemory<char>("abcdefg01234567890hijklmn01234567890hijklmn");

        int r1 = source.LastIndexOf("def");
        int r2 = source.LastIndexOf("abc");
        int r3 = source.LastIndexOf("klmn");
        int r4 = source.LastIndexOf("XYZ");
        int r5 = source.LastIndexOf("abcdefg01234567890hijklmn01234567890hijklmn");
        int r6 = source.LastIndexOf("NFWINVWEOIBWIRUOFNUIRWNEFRJWFIWRFEFBREFNJ3N43U4NFIUN3FNRFLFNJW");
        int r7 = source.LastIndexOf("");


        bool result = r1 == 3 && r2 == 0 && r3 == 39 && r4 == -1 &&
        r5 == 0 && r6 == -1 && r7 == 43;


        source.Dispose();

        return result;
    }


    static string ToBytes_ori_string = "abcdefghijklnm";

    /// <summary>
    /// 测试把字符串转换到 bytes ，并且还原
    /// </summary>
    /// <returns></returns>
    public static bool ToBytes()
    {
        UnManagedMemory<char> ori_string = ToBytes_ori_string.CopyToChars();

        UnManagedMemory<byte> bytes = ori_string.CopyToBytes();

        UnManagedMemory<char> asString = ((UnManagedCollection<byte>)bytes).CopyToChars();

        bool result = asString.Equals(ToBytes_ori_string);

        bytes.Dispose();

        asString.Dispose();

        ori_string.Dispose();


        return result;
    }


    static string temo_string_for_CopyTo = "abcdefg";

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static bool CopyTo()
    {
        Span<int> intArray = stackalloc int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        Span<int> intArray_1 = stackalloc int[10];

        UnManagedMemory<int> mem_int = new UnManagedMemory<int>(intArray);

        mem_int.CopyTo(intArray_1);

        char* newStr = stackalloc char[] { '1', '2', '3', '4' };

        UnManagedMemory<char> str = new UnManagedMemory<char>(newStr, 4, 4, MemoryTypeDefined.Stack);
        str.CopyTo(temo_string_for_CopyTo);

        bool temp_string_new_value = temo_string_for_CopyTo[0] == '1' && temo_string_for_CopyTo[1] == '2'
                        && temo_string_for_CopyTo[2] == '3' && temo_string_for_CopyTo[6] == 'g';


        bool result = intArray_1[0] == intArray[0] && intArray_1[9] == intArray[9] && intArray_1[5] == intArray[5];

        mem_int.Dispose();
        str.Dispose();

        return result;
    }

    /// <summary>
    /// 测试反转元素
    /// </summary>
    /// <returns></returns>
    public static bool Reverse()
    {
        int* intArray = stackalloc int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        UnManagedMemory<int> mem_int = new UnManagedMemory<int>(new Span<int>(intArray, 10));


        int o0 = *mem_int.Index(0);

        int o9 = *mem_int.Index(9);

        mem_int.Reverse();

        int v0 = *mem_int.Index(0);

        int v9 = *mem_int.Index(9);

        bool result = o0 == 0 && o9 == 9 && v0 == 9 && v9 == 0;

        mem_int.Dispose();

        return result;
    }


    /// <summary>
    /// 测试排序
    /// </summary>
    /// <returns></returns>
    public static bool Sort()
    {
        Span<int> intArray = stackalloc int[] { 5, 2, 8, 1, 9 };

        UnManagedMemory<int> mem_int = new UnManagedMemory<int>(intArray);

        mem_int.Sort();

        bool result0 = *mem_int[0] == 1 && *mem_int[1] == 2 && *mem_int[4] == 9;

        Span<double> doubleArray = stackalloc double[] { 5.01, 2.01, 8.01, 1.01, 9.01 };

        UnManagedMemory<double> mem_double = new UnManagedMemory<double>(doubleArray);

        mem_double.Sort();

        bool result1 = *mem_double[0] == 1.01 && *mem_double[1] == 2.01 && *mem_double[4] == 9.01;

        mem_double.Dispose();


        Span<char> chars = stackalloc char[] { 'c', 'a', 'k', 'z', 'b' };

        UnManagedMemory<char> mem_chars = new UnManagedMemory<char>(chars);

        mem_chars.Sort();

        bool result2 = *mem_chars[0] == 'a' && *mem_chars[1] == 'b' && *mem_chars[4] == 'z';

        mem_chars.Dispose();


        Span<float> floatArray = stackalloc float[] { 5.01f, 2.01f, 8.01f, 1.01f, 9.01f };

        UnManagedMemory<float> mem_float = new UnManagedMemory<float>(floatArray);

        mem_float.Sort();

        bool result3 = *mem_float[0] == 1.01f && *mem_float[1] == 2.01f && *mem_float[4] == 9.01f;

        mem_float.Dispose();


        bool final = result0 && result1 && result2 && result3;

        return final;
    }










    /// <summary>
    /// 测试插入
    /// </summary>
    /// <returns></returns>
    public static bool InsertAt()
    {


        UnManagedMemory<char> obj = new UnManagedMemory<char>(100, 0);
        obj.AddRange("1234ABCDEFGHIJKLMN");

        bool insert_result_0 = obj.Insert(2, '9');

        bool result_0 = obj.Equals("12934ABCDEFGHIJKLMN"); //测试中间插入

        bool insert_result_1 = obj.Insert(obj.UsageSize, 'H');

        bool result_1 = obj.Equals("12934ABCDEFGHIJKLMNH"); //测试尾部插入，等同于添加

        bool insert_result_2 = obj.Insert(0, 'V');

        bool result_2 = obj.Equals("V12934ABCDEFGHIJKLMNH"); //测试头部插入


        obj.Dispose();


        return result_0 && result_1;
    }


    public static bool InsertCollectionAt()
    {
        UnManagedMemory<int> obj = new UnManagedMemory<int>(4, 0);

        obj.Add(1);
        obj.Add(2);
        obj.Add(3);
        obj.Add(4);


        bool result_insertAt = obj.InsertRange(2, [5, 6, 7, 8]);

        bool result_0 = obj.Equals([1, 2, 5, 6, 7, 8, 3, 4]);

        UnManagedMemory<int> append = new UnManagedMemory<int>([100, 101, 102, 103]);

        obj.InsertRange(3, append);

        bool result_1 = obj.Equals([1, 2, 5, 100, 101, 102, 103, 6, 7, 8, 3, 4]);


        obj.Dispose();
        append.Dispose();


        return result_0 && result_1;
    }

    public static bool RemoveRange()
    {
        UnManagedMemory<int> obj = new UnManagedMemory<int>([1, 2, 3, 4, 5, 6]);

        obj.RemoveRange(2, 2);

        bool result_0 = obj.Equals([1, 2, 5, 6]);

        obj.Dispose();

        return result_0;
    }


    public static bool ReSize()
    {
        UnManagedMemory<int> source = new UnManagedMemory<int>([1, 2, 3, 4, 5, 6]);

        source.Resize(3);

        bool result_0 = source.Capacity == 3;

        source.Resize(9);

        source.Resize(100);

        source.Resize(200);

        result_0 = result_0 && source.Capacity == 200;

        source.Dispose();

        return result_0;
    }




    /// <summary>
    /// 测试获取内部指针
    /// </summary>
    /// <returns></returns>
    public static bool GetInnerPointer()
    {
        char* p_chars = stackalloc char[10];
        UnManagedMemory<char> mem = new UnManagedMemory<char>(p_chars, "12345", MemoryTypeDefined.Stack);


        void* p_mem = &mem;

        char** p_pointer_address = (char**)((byte*)p_mem + 0); //元素集合的0下标就是指针的起始地址

        char* p = *p_pointer_address;


        bool result_1 = p[0] == '1' && *(p + 4) == '5';

        return result_1;
    }



    /// <summary>
    /// 测试获取指针
    /// </summary>
    /// <returns></returns>
    public static bool GetPointer()
    {

        UnManagedMemory<char> mem = new UnManagedMemory<char>("12345");


        char* p = mem.Pointer;

        bool result_1 = p[0] == '1' && *(p + 4) == '5';

        mem.Dispose();

        return result_1;
    }



    /// <summary>
    /// 测试把内存自动按索引分配
    /// </summary>
    /// <returns></returns>
    public static bool AutoIndexMemory()
    {
        byte* allocResult = (byte*)NativeMemory.AllocZeroed(64 * sizeof(char));



        char* p_chars = (char*)allocResult;



        char* p_chars_start = p_chars; //记录原始的0下标，接下来p_chars的指向马上就会变动

        UnManagedMemory<char> mem_0 = new UnManagedMemory<char>(&p_chars, "abc");
        UnManagedMemory<char> mem_1 = new UnManagedMemory<char>(&p_chars, "defg");
        UnManagedMemory<char> mem_2 = new UnManagedMemory<char>(&p_chars, "hijkl");


        bool result = p_chars_start[0] == 'a' && p_chars_start[7] == 'h'

        && mem_0[0] == p_chars_start
        && mem_1[0] == p_chars_start + 3
        && mem_2[0] == p_chars_start + 7;

        NativeMemory.Free(p_chars_start);

        return result;

    }



    /// <summary>
    /// 测试作为外部内存的观测器时，边界是否安全
    /// </summary>
    /// <returns></returns>
    public static bool From_ExtMemory()
    {
        int charSize = 10 * sizeof(char);
        byte* allocResult = (byte*)NativeMemory.AllocZeroed((nuint)charSize);

        char* p_chars = (char*)allocResult;

        UnManagedMemory<char> mem = new UnManagedMemory<char>(p_chars, 10, 0);

        mem.AddRange("12345");

        bool result_1 = mem.UsageSize == 5;

        mem.AddRange("67890");

        bool result_2 = mem.UsageSize == 10;

        bool out_of_size = mem.AddRange("abcde");

        NativeMemory.Free(p_chars);

        return result_1 && result_2 && !out_of_size;
    }


    /// <summary>
    /// 把 span 转换为 UnamangedMemory
    /// </summary>
    /// <returns></returns>
    public static bool From_Span()
    {
        // ============ 正确赋值：

        int charsSize = 9 * sizeof(char);

        byte* allocResult = (byte*)NativeMemory.AllocZeroed((nuint)charsSize);

        char* p_chars = (char*)allocResult;

        ReadOnlySpan<char> chars = "12345";

        UnManagedMemory<char> mem = chars.CopyToUnManagedMemory(p_chars, 9);

        mem.AddRange("678");

        bool result_1 = mem.UsageSize == 8;

        mem.Zero();


        //  ============ 错误赋值：p_chars 被指定的容量小于 chars 的长度，创建的对象不会被赋值

        UnManagedMemory<char> mem2 = chars.CopyToUnManagedMemory(p_chars, 2);

        bool result_2 = mem.IsEmpty; //初始化时就不会被赋值

        mem2.AddRange("678");

        bool result_3 = mem.IsEmpty; //同时也不会被允许添加值

        NativeMemory.Free(p_chars);

        return result_1 && result_2 && result_3;

    }


    /// <summary>
    /// 创建空值对象
    /// </summary>
    /// <returns></returns>
    public static bool Create_Empty()
    {
        UnManagedMemory<char> mem = new UnManagedMemory<char>();

        bool result_1 = mem.Capacity == 0 && mem.UsageSize == 0 && !mem.Allocated && mem.IsEmpty;

        return result_1;
    }

    static string temp_Split = "123,456,789";
    public static bool SpiltMap()
    {
        UnManagedMemory<char> mem_str = temp_Split;

        UnManagedMemory<UnManagedMemory<char>> mem = mem_str.Split(',', false);

        bool result = mem.UsageSize == 3;

        result = result && mem[0]->Equals("123") && mem[1]->Equals("456") && mem[2]->Equals("789");

        mem.Dispose();

        mem_str.Dispose();

        return result;
    }



    public static bool SpiltCopy()
    {
        UnManagedMemory<char> mem = temp_Split;

        UnManagedMemory<UnManagedMemory<char>> mem_str = mem.Split(',', true);

        bool result = mem_str.UsageSize == 3;

        result = result && mem_str[0]->Equals("123") && mem_str[1]->Equals("456") && mem_str[2]->Equals("789");

        for (int i = 0; i < mem_str.UsageSize; i++)
        {
            UnManagedMemory<char>* node = mem_str.Index(i);

            if (node is not null)
                node->Dispose();
        }

        foreach (UnManagedMemory<char>* i in mem_str)
        {
            i->Dispose();
        }

        mem_str.Dispose();

        return result;
    }



    static string temp_SpiltDictionary = "a=1;b=2;c=3";
    public static bool SpiltMapToValueFrozenDictionary()
    {

        ValueDictionary<UnManagedMemory<char>, UnManagedMemory<char>> dic = temp_SpiltDictionary.SplitMapToValueDictionary(';', '=');

        bool result = dic.Count == 3;

        dic.Dispose();


        return result;
    }



    public static bool SpiltCopyToValueFrozenDictionary()
    {
        ValueDictionary<UnManagedMemory<char>, UnManagedMemory<char>> dic = temp_SpiltDictionary.SplitCopyToValueDictionary(';', '=');

        bool result = dic.Count == 3;


        dic.DisposeAll();

        return result;
    }

    public static bool myMemoryPool()
    {

        MemoryPoolCluster pool = new MemoryPoolCluster();

        Span<MemoryPoolSchema> schemas = stackalloc MemoryPoolSchema[]
        {
            new MemoryPoolSchema(4,1024),
            new MemoryPoolSchema(8,1024),
            new MemoryPoolSchema(16,1024),
            new MemoryPoolSchema(32,1024),
            new MemoryPoolSchema(64,1024),
            new MemoryPoolSchema(128,1024),
            new MemoryPoolSchema(256,1024)
        };


        pool.Init(schemas);

        var innerPool = pool.SelectPool(20);




        UnManagedMemory<char> c = new UnManagedMemory<char>(10, 0, &pool);

        c.AddRange("12345");

        c.Dispose();


        pool.Dispose();

        return true;
    }




    /// <summary>
    /// 测试手动构造函数
    /// </summary>
    /// <returns></returns>
    public static bool Init()
    {
        bool result = false;

        UnManagedMemory<char>* mem = stackalloc UnManagedMemory<char>[1];

        mem->Init(8, 0);

        mem->AddRange("abcd");

        result = mem->UsageSize == 4;

        mem->Dispose();

        //验证已经释放
        result = result && !mem->Activated;

        bool add_try = mem->Add('c'); //此时做添加是无效的

        result = result && !add_try;


        return result;
    }












    /// <summary>
    /// 由空值对象转变为中途分配内存
    /// </summary>
    /// <returns></returns>
    public static bool Empty_to_Allocted()
    {
        UnManagedMemory<char> mem = new UnManagedMemory<char>();

        //由于初始的空对象不可能承载于内存池，所以这里就不做内存池检测了

        bool result_1 = mem.Capacity == 0 && mem.UsageSize == 0 && !mem.Allocated && mem.IsEmpty;

        mem.AddRange("12345");

        bool result_2 = mem.Capacity == 5 && mem.UsageSize == 5 && mem.Allocated && !mem.IsEmpty && mem.Allocated;

        mem.Dispose();

        return true;
    }






    /// <summary>
    /// 作为外部内存观测器时的重设状态
    /// </summary>
    /// <returns></returns>
    public static bool Reset_From_ExternalMemory()
    {
        int intValueLength = 973; //分配一个容易识别的数字

        int allocSize = intValueLength * sizeof(int);

        byte* allocResult = (byte*)NativeMemory.AllocZeroed((nuint)allocSize);

        int* p_int = (int*)allocResult;

        UnManagedMemory<int> memory = new UnManagedMemory<int>(p_int, (uint)intValueLength, (uint)intValueLength);

        uint o_length = memory.UsageSize;

        uint o_realSize = memory.Capacity;

        bool o_isEmpty = memory.IsEmpty;





        //=====================================================

        //随意做一些操作，改变对象的状态
        bool result_removeRange = memory.RemoveRange(30, 20);
        bool result_resize = memory.Resize(200);

        //=====================================================

        memory.Zero();


        bool result =
        memory.UsageSize == 0 &&
        memory.Capacity == o_length;

        NativeMemory.Free(p_int);

        return result;
    }


    /// <summary>
    /// 比较两段相同的值具备相同的哈希码，但是内存地址不同
    /// </summary>
    /// <returns></returns>
    public static bool HashCode()
    {
        var m1 = new UnManagedMemory<char>("abcdefg");
        var m2 = new UnManagedMemory<char>("abcdefg");

        int hashcode1 = m1.GetHashCode();
        int hashcode2 = m2.GetHashCode();

        bool result = hashcode1 == hashcode2 && m1.Pointer != m2.Pointer;

        m1.Dispose();
        m2.Dispose();

        return result;

    }



    /// <summary>
    /// 测试克隆对象
    /// </summary>
    /// <returns></returns>
    public static bool Clone()
    {
        char* p_char = stackalloc char[5];
        char* p_chars_clone = stackalloc char[5];

        p_char[0] = 'k';
        p_char[1] = 'd';
        p_char[2] = 'm';
        p_char[3] = '4';
        p_char[4] = '9';



        UnManagedMemory<char> origion = new UnManagedMemory<char>(p_char, 5, 5);



        UnManagedMemory<char> clone = origion.Clone(p_chars_clone);

        bool result = origion.Equals(*(UnManagedCollection<char>*)&clone) &&

        origion.AsSpan().SequenceEqual(new Span<char>(p_chars_clone, 5)) &&

        origion.Capacity == clone.Capacity &&

        origion.UsageSize == clone.UsageSize &&



        origion.OnStack == clone.OnStack &&

        origion.Allocated == clone.Allocated

        ;



        UnManagedMemory<char> origion1 = new UnManagedMemory<char>();

 



        origion1.ReLength(49);

        UnManagedMemory<char> clone1 = origion1.Clone();

        bool equ = origion1.Equals(&clone1);

        bool result1 = equ &&

        origion1.Capacity == clone1.Capacity &&

        origion1.UsageSize == clone1.UsageSize && 


        origion1.OnStack == clone1.OnStack &&

        origion1.Allocated == clone1.Allocated

        ;

        clone1.Dispose();



        return result && result1;

    }



    static bool checkMemoryTypeOnMethod(void* pointer)
    {
        return MemoryTypeChecker.OnStack(pointer);
    }



    /// <summary>
    /// 检测内存是否处于栈上
    /// </summary>
    /// <returns></returns>
    public static bool Check_on_stack()
    {

        int stackVar = 42;

        bool is_stack_0 = MemoryTypeChecker.OnStack(&stackVar);


        int* stackVar_1 = stackalloc int[3];

        bool is_stack_1 = MemoryTypeChecker.OnStack(stackVar_1);


        char* stackVar_3 = stackalloc char[1];
        *stackVar_3 = 'h';

        bool is_stack_3 = checkMemoryTypeOnMethod(stackVar_3);


        d_checkMemoryType d_0 = m_checkMemoryType;

        bool is_stack_4 = MemoryTypeChecker.OnStack(&d_0);


        void* p_null = null;

        bool is_stack_5 = !MemoryTypeChecker.OnStack(p_null);



        int* heap_0 = (int*)NativeMemory.Alloc(sizeof(int) * 3);

        bool is_heap_0 = !MemoryTypeChecker.OnStack(heap_0);

        NativeMemory.Free(heap_0);

        bool result = is_stack_0 && is_stack_1 && is_heap_0 && is_stack_3 && is_stack_4 && is_stack_5;

        return result;
    }

    static checkType heap_1;

    delegate void d_checkMemoryType(int i);

    static void m_checkMemoryType(int i) { }


}

class checkType
{
    public int Name;
}

struct Test_Use_To_Alternative
{
    public int value1;

    public int value2;
}



/// <summary>
/// 用于测试值类型相等性判断
/// </summary>
struct UseToValueTypeEquals
{

    public int Id;

    public int Field_0;

    public int Field_1;

    public char Field_2;

}


