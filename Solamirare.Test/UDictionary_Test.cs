


using System.Diagnostics;
using System.IO.Pipelines;
using System.Reflection.PortableExecutable;

namespace Solamirare.Tests;

public static unsafe class UDictionary_Test
{

    public static bool Remove()
    {
        ValueDictionary<UnManagedMemory<char>, UnManagedMemory<char>>
            Headers = new ValueDictionary<UnManagedMemory<char>, UnManagedMemory<char>>(64); // 64: 初始容量


        Headers.AddOrUpdate("Server", "Solamirare");
        Headers.Remove("Server");



        Headers.ForEach(&DictionaryDisposeWithInnerLoop, null);
        Headers.Dispose();

        bool result_0 = Headers.Count == 0;


        ValueDictionary<int, int> dic2 = new ValueDictionary<int, int>(3);

        dic2.AddOrUpdate(1, 1);
        dic2.AddOrUpdate(2, 2);
        dic2.AddOrUpdate(3, 3);

        var v1 = dic2[1];
        var v2 = dic2[2];
        var v3 = dic2[3];

        dic2.Clear(); //必须通过该方法，才可以保证接下去能够利用已经分配的内存

        dic2.Add(2, 22);

        var v_2_1 = dic2[2];

        bool result_2 = v2 == v_2_1; //地址相同，重新利用了之前已经分配的内存

        dic2.Dispose();

        return result_0 && result_2;
    }


    /// <summary>
    /// 测试各种输入是否会造成内存泄漏，该方法仅用于Performance测试检测
    /// </summary>
    /// <returns></returns>
    public static bool Append()
    {

        ValueDictionary<UnManagedMemory<char>, UnManagedMemory<char>>
            Headers = new ValueDictionary<UnManagedMemory<char>, UnManagedMemory<char>>(64); // 64: 初始容量

        Headers.AddOrUpdate("Date", DateTime.UtcNow.GMTSting());

        Headers.AddOrUpdate("Server", "Solamirare");

        Headers.AddOrUpdate("Server1", "Solamirare1");

        Headers.AddOrUpdate("Server2", "Solamirare2");

        Headers.AddOrUpdate("Server3", "Solamirare3");

        Headers.AddOrUpdate("Server4", "Solamirare4");

        Headers.AddOrUpdate("Server5", "Solamirare5");


        Headers.ForEach(&DictionaryDisposeWithInnerLoop, null);
        Headers.Dispose();

        return true;
    }

    static bool DictionaryDisposeWithInnerLoop<T1, T2>(int index, UnManagedMemory<T1>* key, UnManagedMemory<T2>* value, void* caller)
where T1 : unmanaged
where T2 : unmanaged
    {
        if (key is not null)
        {
            key->Dispose();
        }

        if (value is not null)
        {
            value->Dispose();
        }

        return true;
    }






    public static bool ToJson()
    {
        ValueDictionary<UnManagedMemory<char>, UnManagedMemory<char>> dic = new ValueDictionary<UnManagedMemory<char>, UnManagedMemory<char>>(4);

        dic.AddOrUpdate("111", "v111");
        dic.AddOrUpdate("222", "v222");
        dic.AddOrUpdate("333", "v333");
        dic.AddOrUpdate("444", "v444");


        UnManagedMemory<char> json = dic.SerializeToJson();

        bool result = json.UsageSize == 53;

        //因为字典中的元素是乱序排列的，无法输出固定的json字符串，以下判断有时候成功，有时候失败
        //json.Equals("""{"333":"v333","222":"v222","111":"v111","444":"v444"}""");

        dic.ForEach(&DictionaryDisposeWithInnerLoop, null);

        json.Dispose();

        return result;
    }


    public static bool FindByBytes()
    {
        ValueDictionary<UnManagedMemory<byte>, int> dic = new ValueDictionary<UnManagedMemory<byte>, int>(4, true);

        dic.Add("111"u8, 1);
        dic.Add("222"u8, 2);
        dic.Add("333"u8, 3);
        dic.Add("444"u8, 4);

        ReadOnlySpan<byte> mem_333 = "333"u8;


        int* select_3 = dic.Index(mem_333.MapToUnManagedMemory());

        bool result = *select_3 == 3;

        foreach (var i in dic)
        {
            i->Key.Dispose();
        }

        dic.Dispose();

        return result;
    }



    public static bool FindBySpan()
    {
        ValueDictionary<UnManagedMemory<char>, int> dic = new ValueDictionary<UnManagedMemory<char>, int>(4, true);

        dic.Add("111", 1);
        dic.Add("222", 2);
        dic.Add("333", 3);
        dic.Add("444", 4);

        ReadOnlySpan<char> mem_333 = "333";

        int* select_3 = dic.Index(mem_333.MapToUnManagedMemory());

        bool result = *select_3 == 3;

        foreach (var i in dic)
        {
            i->Key.Dispose();
        }

        dic.Dispose();

        return result;
    }


    public static bool UnManagedString()
    {

        ValueDictionary<UnManagedMemory<char>, UnManagedMemory<char>> dic = new ValueDictionary<UnManagedMemory<char>, UnManagedMemory<char>>(4, true);

        bool append_0 = dic.AddOrUpdate("111", "v111");
        bool append_1 = dic.AddOrUpdate("222", "v222");
        bool append_2 = dic.AddOrUpdate("333", "v333");
        bool append_3 = dic.AddOrUpdate("444", "v444");

        UnManagedMemory<char>* p_111 = dic.Index("111".AsSpan().MapToUnManagedMemory());

        bool result = p_111->Equals("v111");

        UnManagedMemory<char>* select_2 = dic["222".AsSpan().MapToUnManagedMemory()];

        result = result && select_2->Equals("v222");



        dic.AddOrUpdate("222", "2222");


        UnManagedMemory<char>* V1_New = dic["222".AsSpan().MapToUnManagedMemory()];

        result = result && V1_New->Equals("2222");

        dic.ForEach(&DictionaryDisposeWithInnerLoop, null);

        dic.Dispose();

        return result;
    }



    public static bool ForEach()
    {
        ValueDictionary<int, int> u = new ValueDictionary<int, int>(8, true);

        u.AddOrUpdate(1, 1);
        u.AddOrUpdate(2, 2);
        u.AddOrUpdate(3, 3);
        u.AddOrUpdate(4, 4);
        u.AddOrUpdate(5, 5);

        // UDictionary<int, int>.Enumerator enumerator = u.GetEnumerator();
        // while (enumerator.MoveNext())
        // {
        //     DictionarySlot<int, int>* slot = enumerator.Current;
        //     if (slot->Key == 4)
        //     {
        //         enumerator.RemoveCurrent();
        //     }
        // }

        bool _remove = u.Remove(3);

        int result = 0;

        foreach (DictionarySlot<int, int>* i in u)
        {
            result += i->Value;
        }

        u.Dispose();

        return result == 12;
    }


    /// <summary>
    /// 测试基础的添加、指针访问与删除
    /// </summary>
    public static bool BasicOperations()
    {
        ValueDictionary<int, long> dict = new ValueDictionary<int, long>(16, true);

        // 必须先声明变量，再取地址
        int key1 = 1; long val1 = 100L;
        int key2 = 2; long val2 = 200L;

        dict.AddOrUpdate(&key1, &val1);
        dict.AddOrUpdate(&key2, &val2);

        // 测试索引器返回指针并解引用修改
        long* pResult = dict[key1];
        if (pResult == null || *pResult != 100L) { dict.Dispose(); return false; }

        *pResult = 101L; // 通过指针就地修改
        if (*dict[key1] != 101L) { dict.Dispose(); return false; }

        // 测试删除
        if (!dict.Remove(key1) || dict.Count != 1) { dict.Dispose(); return false; }

        dict.Dispose();
        return true;
    }


    /// <summary>
    /// 测试 TryGetValue 与 TryAdd
    /// </summary>
    public static bool TryMethods()
    {
        ValueDictionary<int, int> dict = new ValueDictionary<int, int>(8, true);

        int key = 10;
        int val = 1000;

        // TryAdd 内部会处理 key 的 in 引用
        if (!dict.AddOrUpdate(&key, &val)) { dict.Dispose(); return false; }

        // 重复添加，变为更新
        int val2 = 2000;
        if (!dict.AddOrUpdate(&key, &val2)) { dict.Dispose(); return false; }

        // TryGetValue
        int* getValue = dict[key];

        if (getValue == null || *getValue != 2000) { dict.Dispose(); return false; }

        dict.Dispose();
        return true;
    }

    /// <summary>
    /// 测试 AddOrUpdate
    /// </summary>
    public static bool AddOrUpdate()
    {
        ValueDictionary<int, int> dict = new ValueDictionary<int, int>(8, true);

        int key = 1;
        int val1 = 10;
        dict.AddOrUpdate(key, val1);

        int val2 = 20;
        dict.AddOrUpdate(key, val2); // 更新现有值

        if (*dict[key] != 20) { dict.Dispose(); return false; }

        // GetOrAdd (不存在时插入并获取指针)
        int key2 = 2;

        dict.AddOrUpdate(key2, 200);
        int* pStored = dict[key2];
        if (pStored == null || *pStored != 200) { dict.Dispose(); return false; }

        // 修改指针指向的内容
        *pStored = 300;
        if (*dict[key2] != 300) { dict.Dispose(); return false; }

        dict.Dispose();
        return true;
    }

    /// <summary>
    /// 测试迭代器与其 RemoveCurrent 功能
    /// </summary>
    public static bool IteratorAndRemoveCurrent()
    {
        ValueDictionary<int, int> dict = new ValueDictionary<int, int>(16, true);

        for (int i = 0; i < 10; i++)
        {
            int k = i; // 声明局部变量
            int v = i * 10;
            dict.AddOrUpdate(&k, &v);
        }

        ValueDictionary<int, int>.Enumerator etor = dict.GetEnumerator();

        while (etor.MoveNext())
        {
            // 删除键为偶数的节点
            if (etor.Current->Key % 2 == 0)
            {
                etor.RemoveCurrent();
            }
        }

        if (dict.Count != 5) { dict.Dispose(); return false; }

        int checkKey = 2;

        if (dict.ContainsKey(checkKey)) { dict.Dispose(); return false; }

        dict.Dispose();
        return true;
    }

    /// <summary>
    /// 测试 Keys 与 Values 属性 (ref struct 迭代器)
    /// </summary>
    public static bool TestCollections()
    {
        ValueDictionary<int, int> dict = new ValueDictionary<int, int>(8, true);
        int k1 = 1; int v1 = 100;
        int k2 = 2; int v2 = 200;
        dict.AddOrUpdate(&k1, &v1);
        dict.AddOrUpdate(&k2, &v2);

        int keySum = 0;
        ValueDictionary<int, int>.KeyEnumerator keyEtor = dict.Keys.GetEnumerator();
        while (keyEtor.MoveNext()) keySum += keyEtor.Current;
        if (keySum != 3) { dict.Dispose(); return false; }

        int valSum = 0;
        ValueDictionary<int, int>.ValueEnumerator valEtor = dict.Values.GetEnumerator();
        while (valEtor.MoveNext()) valSum += valEtor.Current;
        if (valSum != 300) { dict.Dispose(); return false; }

        dict.Dispose();
        return true;
    }

    /// <summary>
    /// 测试容量管理: 扩容, Rehash, TrimExcess
    /// </summary>
    public static bool CapacityManagement()
    {
        ValueDictionary<int, int> dict = new ValueDictionary<int, int>(4, false);

        for (int i = 0; i < 20; i++)
        {
            int k = i; int v = i;
            dict.AddOrUpdate(&k, &v);
        }

        if (dict.Capacity < 20) { dict.Dispose(); return false; }

        // 测试 Clear 不释放内存但重置计数
        dict.Clear();
        if (dict.Count != 0) { dict.Dispose(); return false; }

        // 测试 TrimExcess
        for (int i = 0; i < 5; i++)
        {
            int k = i; int v = i;
            dict.AddOrUpdate(&k, &v);
        }
        dict.TrimExcess();
        // 5个元素，PowerOfTwo 为 8
        if (dict.Capacity != 8) { dict.Dispose(); return false; }

        dict.Dispose();
        return true;
    }

    /// <summary>
    /// 测试 TryUpdate 与 ContainsValue
    /// </summary>
    public static bool UtilityMethods()
    {
        ValueDictionary<int, int> dict = new ValueDictionary<int, int>(8, true);
        int k = 1; int v = 500;
        dict.AddOrUpdate(&k, &v);


        int newVal = 600;
        dict.AddOrUpdate(k, newVal);
        if (*dict[k] != 600) { dict.Dispose(); return false; }

        // ContainsValue
        if (!dict.ContainsValue(newVal)) { dict.Dispose(); return false; }

        dict.Dispose();
        return true;
    }



}