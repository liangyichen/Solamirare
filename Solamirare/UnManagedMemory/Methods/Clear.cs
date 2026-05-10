namespace Solamirare;


public unsafe partial struct UnManagedMemory<T>
{
    /// <summary>
    /// 将集合中的所有元素按照真实容量长度执行内容清零，容量与使用长度不会被改变。
    /// </summary>
    /// <returns></returns>
    public void Clear()
    {
        if (@readonly || !activated || Pointer is null) return;

        
        NativeMemory.Clear(Pointer,capacity);
        
    }
}