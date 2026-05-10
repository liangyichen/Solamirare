
public static unsafe class Sample_ValueStack
{
    public static void CommonUsage()
    {
        ValueStack<int> stack = new ValueStack<int>(5);

        stack.Push(0);
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);
        stack.Push(4);

        int* i0 = stack.Pop();
        int* i1 = stack.Pop();
        int* i2 = stack.Pop();
        int* i3 = stack.Pop();
        int* i4 = stack.Pop();


        bool cap = stack.Capacity == 5; //容量依旧保持不变

        bool count = stack.Count == 0; //已经使用量退回0了



        stack.Push(0);
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);
        stack.Push(4);
        stack.Push(5);
        stack.Push(6);
        stack.Push(7);
        stack.Push(8);
        stack.Push(9);



        cap = stack.Capacity == 10; // 已经扩容到10

        count = stack.Count == 10; // 使用量也已经到10


        stack.Dispose();
    }
}