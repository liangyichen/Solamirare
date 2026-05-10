
/// <summary>
/// 固定栈
/// </summary>
public static unsafe class Sample_ValueFrozenStack
{
    /// <summary>
    /// 常规使用
    /// </summary>
    public static void CommonUsage()
    {
        ValueFrozenStack<int> stack = new ValueFrozenStack<int>(5);


        stack.Push(0);
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);
        stack.Push(4);

        stack.Push(5); // 这是失败的，固定栈不会进行扩容

        int i0 = stack.Pop();
        int i1 = stack.Pop();
        int i2 = stack.Pop();
        int i3 = stack.Pop();
        int i4 = stack.Pop();


        bool cap = stack.Capacity == 5; //容量依旧保持不变

        bool count = stack.Count == 0; //已经使用量退回0了


        stack.Dispose();
    }
}

