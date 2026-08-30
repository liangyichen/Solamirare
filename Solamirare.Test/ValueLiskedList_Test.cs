using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace Solamirare.Tests;







/// <summary>
/// 链表测试
/// </summary>
public static unsafe class ValueLiskedList_Test
{
    /// <summary>
    /// 常规项目
    /// </summary>
    /// <returns></returns>
    public static bool Commons()
    {
        int i1 = 1, i2 = 2, i3 = 3, i4 = 4, i99 = 99;

        int a999 = 999, a888 = 888;


        ValueLinkedList<int> list = new ValueLinkedList<int>();
        list.Append(&i1);
        list.Append(&i2);
        list.Append(&i3);
        list.Append(&i4);
        list.Append(&a888);
        list.Append(&a999);


        list.SetAsFree(2);
        list.Append(2000); // 1 2 4 888 999 2000

        list.InsertAt(2, &i99); // 1 2 99 4 888 999 2000
        list.SetAsFree(4); // 1 2 99 4 999 2000
        list.Append(7777); // 1 2 99 4 999 2000 7777

        var v0 = *list[0];
        var v1 = *list[1];
        var v2 = *list[2];
        var v3 = *list[3];
        var v4 = *list[4];
        var v5 = *list[5];
        var v6 = *list[6];

        bool result =

        v0 == 1 && v1 == 2 && v2 == 99 && v3 == 4 && v6 == 7777
        && list[-9] == null && list[77] == null
        && !list.ContainsAddress(&i1)
        && list.Contains(99)

        ;


        list.Dispose();

        return result;
    }

    /// <summary>
    /// 测试混合外部地址与内部创建的方式添加值
    /// </summary>
    /// <returns></returns>
    public static bool MixedAppend()
    {
        int* memory_outside = stackalloc int[4 * 4];

        memory_outside[0] = 1;
        memory_outside[1] = 2;
        memory_outside[2] = 3;
        memory_outside[3] = 4;


        ValueLinkedList<int> list = new(memory_outside, 4, false);

        list.Append(5);
        list.Append(6);
        list.Append(7);
        list.Append(8);

        bool result = list.NodesCount == 8 && *list[0] == 1 && *list[7] == 8;

        list.Dispose();


        return result;
    }



    static bool foreahVoid(int index, int* item, void* caller)
    {
        *item = 9;

        return true;
    }


    public static bool ForEachMethod()
    {
        int i1 = 1, i2 = 2, i3 = 3, i4 = 4;

        ValueLinkedList<int> list = new ValueLinkedList<int>();
        list.Append(&i1);
        list.Append(&i2);
        list.Append(&i3);
        list.Append(&i4);


        list.ForEach(&foreahVoid, null);

        bool result = *list[0] == 9 && *list[1] == 9 && *list[2] == 9 && *list[3] == 9;


        list.Dispose();

        return result;

    }





    public static bool Dispose()
    {
        int i1 = 1, i2 = 2, i3 = 3, i4 = 4;

        ValueLinkedList<int> list = new ValueLinkedList<int>();
        list.Append(&i1);
        list.Append(&i2);
        list.Append(&i3);
        list.Append(&i4);

        list.Dispose();

        bool result = list.NodesCount == 0;


        return result;
    }

    public static bool Contains()
    {
        int i1 = 1, i2 = 2, i3 = 3, i4 = 4;

        ValueLinkedList<int> list = new ValueLinkedList<int>();
        list.Append(&i1);
        list.Append(&i2);
        list.Append(&i3);
        list.Append(&i4);

        bool result = list.Contains(1) && list.Contains(2) && list.Contains(3) && list.Contains(4) && !list.Contains(5);

        list.Dispose();

        return result;
    }


    public static bool Update()
    {
        ValueLinkedList<int> list = new ValueLinkedList<int>();
        list.Append(1);
        list.Append(2);
        list.Append(3);
        list.Append(4);

        int newValue = 99;

        list.Update(2, &newValue);

        bool result = *list.Index(2) == 99;


        list.Dispose();

        return result;
    }



    public static bool Equals()
    {
        ValueLinkedList<int> list = new ValueLinkedList<int>();
        list.Append(1);

        Span<int> span_1 = stackalloc int[] { 1 };

        bool result = list.Equals(span_1);

        //========================

        list.Append(2);
        list.Append(3);



        ValueLinkedList<int> list2 = new ValueLinkedList<int>();
        list2.Append(1);
        list2.Append(2);
        list2.Append(3);

        result = result && list.Equals(&list2);

        //========================

        UnManagedMemory<int> um = new UnManagedMemory<int>();
        um.Add(1);
        um.Add(2);
        um.Add(3);



        result = result && list.Equals((UnManagedCollection<int>*)&um);
        result = result && list.Equals(um);

        //========================

        um.Dispose();
        list.Dispose();
        list2.Dispose();

        return result;

    }




    public static bool IndexOf()
    {
        int i1 = 1, i2 = 2, i3 = 3, i4 = 4, i5 = 5, i6 = 6, i7 = 7, i8 = 8, i9 = 9;

        int o1 = 1, o2 = 2, o3 = 3, o4 = 4; //测试判断值相通，但是地址不同

        ValueLinkedList<int> list = new ValueLinkedList<int>();
        list.Append(&i1);
        list.Append(&i2);
        list.Append(&i3);
        list.Append(&i4);
        list.Append(&i5);
        list.Append(&i6);
        list.Append(&i7);
        list.Append(&i8);
        list.Append(&i9);

        int collectionIndex = list.IndexOf([4, 5, 6, 7, 8, 9]);


        bool result = list.IndexOf(1) == 0 && list.IndexOf(2) == 1 && list.IndexOf(3) == 2 && list.IndexOf(4) == 3 && list.IndexOf(5) == 4;

        result = result && list.IndexOf(&o1) == 0 && list.IndexOf(&o2) == 1 && list.IndexOf(&o3) == 2 && list.IndexOf(&o4) == 3;

        result = result && collectionIndex == 3;



        list.Dispose();

        return result;
    }

    public static bool LastIndexOf()
    {
        int i1 = 1, i2 = 2, i3 = 3, i4 = 4;

        ValueLinkedList<int> list = new ValueLinkedList<int>();
        list.Append(&i1);
        list.Append(&i2);
        list.Append(&i3);
        list.Append(&i4);
        list.Append(&i3);

        bool result = list.LastIndexOf(1) == 0 && list.LastIndexOf(2) == 1 && list.LastIndexOf(3) == 4 && list.LastIndexOf(4) == 3 && list.LastIndexOf(5) == -1;

        list.Dispose();

        return result;
    }

    public static bool IndexOfAny()
    {
        int i1 = 1, i2 = 2, i3 = 3, i4 = 4;

        ValueLinkedList<int> list = new ValueLinkedList<int>();
        list.Append(&i1);
        list.Append(&i2);
        list.Append(&i3);
        list.Append(&i4);

        bool result = list.IndexOfAny(new ReadOnlySpan<int>(new int[] { 1, 2, 3, 4 })) == 0 && list.IndexOfAny(new ReadOnlySpan<int>(new int[] { 2, 3, 4 })) == 1 && list.IndexOfAny(new ReadOnlySpan<int>(new int[] { 3, 4 })) == 2 && list.IndexOfAny(new ReadOnlySpan<int>(new int[] { 4 })) == 3 && list.IndexOfAny(new ReadOnlySpan<int>(new int[] { 5 })) == -1;

        list.Dispose();

        return result;
    }

    public static bool ReUseReady()
    {
        int i1 = 1, i2 = 2, i3 = 3, i4 = 4;

        ValueLinkedList<int> list = new ValueLinkedList<int>();
        list.Append(&i1);
        list.Append(&i2);
        list.Append(&i3);
        list.Append(&i4);


        ValueLiskedListNode<int>* temoPtr = list.IndexNode(2);

        list.SetAsFree(2);

        bool result = list.FreeNodesCount == 1 && list.NodesCount == 3;

        list.Append(555);

        result = list.FreeNodesCount == 0 && list.NodesCount == 4;

        var v0 = list[0];
        var v1 = list[1];
        var v2 = list[2];
        var v3 = list[3];

        ValueLiskedListNode<int>* newValueNode = list.IndexNode(3);

        result = result && *list[0] == 1 && *list[1] == 2 && *list[2] == 4 && *list[3] == 555;

        result = result && temoPtr == newValueNode; //验证节点得到了重复利用

        list.Dispose();

        return result;
    }








    public static bool Length()
    {
        int i1 = 1, i2 = 2, i3 = 3, i4 = 4;

        ValueLinkedList<int> list = new ValueLinkedList<int>();
        list.Append(&i1);
        list.Append(&i2);
        list.Append(&i3);
        list.Append(&i4);

        uint result = list.NodesCount;

        list.Dispose();

        return result == 4;
    }

    public static bool IsEmpty()
    {
        ValueLinkedList<int> list = new ValueLinkedList<int>();

        bool result = list.IsEmpty;

        list.Dispose();

        return result;
    }

    public static bool Get()
    {
        int i1 = 1, i2 = 2, i3 = 3, i4 = 4;

        ValueLinkedList<int> list = new ValueLinkedList<int>();
        list.Append(&i1);
        list.Append(&i2);
        list.Append(&i3);
        list.Append(&i4);

        int result = *list.Index(2);

        list.Dispose();

        return result == 3;
    }



    public static bool ContainsSpan()
    {
        int i1 = 1, i2 = 2, i3 = 3, i4 = 4;

        ValueLinkedList<int> list = new ValueLinkedList<int>();
        list.Append(&i1);
        list.Append(&i2);
        list.Append(&i3);
        list.Append(&i4);

        bool result = list.Contains(new ReadOnlySpan<int>(new int[] { 1, 2, 3, 4 })) && list.Contains(new ReadOnlySpan<int>(new int[] { 2, 3, 4 })) && list.Contains(new ReadOnlySpan<int>(new int[] { 3, 4 })) && list.Contains(new ReadOnlySpan<int>(new int[] { 4 })) && !list.Contains(new ReadOnlySpan<int>(new int[] { 5 }));

        list.Dispose();

        return result;
    }


    /// <summary>
    /// 测试引用添加与混合添加
    /// </summary>
    /// <returns></returns>
    public static bool AppendReferences()
    {
        ValueLinkedList<int> list = new ValueLinkedList<int>();
        bool result;

        int i0 = 0, i1 = 1, i2 = 2, i3 = 3;

        bool append_0 = list.Append(i2);
        bool append_1 = list.Append(i3);
        bool append_2 = list.AppendReferences(&i0, 1, false);
        bool append_3 = list.AppendReferences(&i1, 1, false);

        result = list.NodesCount == 4;

        bool c0 = list.Contains(i0);
        bool c1 = list.Contains(i1);

        result = result
        && c0 && c1
        && list.Contains(i2) && list.Contains(i3)
        ;

        list.Dispose();

        return result;
    }




    public static bool Append()
    {
        int i1 = 1, i2 = 2, i3 = 3, i4 = 4, i5 = 5, i6 = 6, i7 = 7;

        ValueLinkedList<int> list = new();
        list.Append(&i1, null);
        list.Append(&i2, null);
        list.Append(&i3, null);
        list.Append(&i4, null);
        list.Append(&i5, null);


        int index_1 = list.IndexOf(&i1);
        int index_2 = list.IndexOf(&i2);
        int index_3 = list.IndexOf(&i3);
        int index_4 = list.IndexOf(&i4);
        int index_5 = list.IndexOf(&i5);
        int index_6 = list.IndexOf(&i6);
        int index_7 = list.IndexOf(&i7);


        int v_index_1 = list.IndexOf(1);
        int v_index_2 = list.IndexOf(2);
        int v_index_3 = list.IndexOf(3);
        int v_index_4 = list.IndexOf(4);
        int v_index_5 = list.IndexOf(5);
        int v_index_6 = list.IndexOf(6);
        int v_index_7 = list.IndexOf(7);



        bool result = index_1 == 0 && index_2 == 1 && index_3 == 2 && index_4 == 3 && index_5 == 4 && index_6 == -1 && index_7 == -1
            && v_index_1 == 0 && v_index_2 == 1 && v_index_3 == 2 && v_index_4 == 3 && v_index_5 == 4 && v_index_6 == -1 && v_index_7 == -1;



        //====================

        list.Dispose();

        return result;
    }

    public static bool Replace()
    {
        int i1 = 1, i2 = 2, i3 = 3, i4 = 4, i5 = 5;


        ValueLinkedList<int> list = new();
        list.Append(&i1);
        list.Append(&i2);
        list.Append(&i3);
        list.Append(&i4);
        list.Append(&i5);


        list.Replace([2, 3], [22, 33, 44, 55, 66]);


        int v0 = *list[0];
        int v1 = *list[1];
        int v2 = *list[2];
        int v3 = *list[3];
        int v4 = *list[4];
        int v5 = *list[5];
        int v6 = *list[6];
        int v7 = *list[7];



        bool result = v0 == 1 && v1 == 22 && v2 == 33 && v3 == 44 && v4 == 55 && v5 == 66 && v6 == 4 && v7 == 5;


        list.Dispose();

        return result;

    }
}

