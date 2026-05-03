

/// <summary>
/// 环形双端队列
/// </summary>
public static unsafe class Sample_CircularDeque
{
    /// <summary>
    /// 常规使用
    /// </summary>
    public static void CommonUsage()
    {
        CircularDeque<int> c = new CircularDeque<int>(10);

        c.PushBack(2);
        c.PushBack(3);
        c.PushBack(4);
        c.PushFront(1);
        c.PushFront(0);
        c.PushBack(5);

        int i0 = c.PopFront();
        int i1 = c.PopFront();
        int i2 = c.PopFront();
        int i3 = c.PopFront();
        int i4 = c.PopFront();
        int i5 = c.PopFront();

        // i0 - i5 依次值是 ： 0,1,2,3,4,5

        c.Dispose();
    }
}