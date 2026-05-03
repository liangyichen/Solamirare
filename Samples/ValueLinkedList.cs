

/// <summary>
/// 链表
/// </summary>
public static unsafe class Sample_ValueLinkedList
{
    /// <summary>
    /// 常规使用
    /// </summary>
    public static void CommonUsage()
    {
        ValueLinkedList<int> list = new ValueLinkedList<int>(10);

        list.Append(0);
        list.Append(1);
        list.Append(2);
        list.Append(3);
        list.Append(4);


        int* i0 = list[0];
        int* i1 = list[1];
        int* i2 = list[2];



        list.Dispose();
    }
}
