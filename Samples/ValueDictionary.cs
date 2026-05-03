
/// <summary>
/// ValueDictionary 使用范例
/// </summary>
public static unsafe class Sample_ValueDictionary
{
    public static void CommonUsage()
    {
        ValueDictionary<int,char> dic = new ValueDictionary<int, char>(10);

        dic.Add(0,'a');
        dic.Add(1,'b');
        dic.Add(2,'c');

        dic.Dispose();


        ValueDictionary<int,UnManagedMemory<char>> dic_0 = new ValueDictionary<int, UnManagedMemory<char>>(10);

        dic_0.Add(0,"abc");
        dic_0.Add(1,"def");
        dic_0.Add(2,"ghi");

        UnManagedMemory<char>* p0 = dic_0[0];
        UnManagedMemory<char>* p1 = dic_0[0];
        UnManagedMemory<char>* p2 = dic_0[0];


        //如果不确定 value 是映射模式或者复制模式，或者每个value的状态不一定，那么使用以下遍历释放，可以保证所有的value节点一定得到释放
        foreach(DictionarySlot<int, UnManagedMemory<char>>* i in dic_0)
        {
            if(i->Value.Allocated)
                i->Value.Dispose();
        }

        dic_0.Dispose(); //释放字典本身

    }
}