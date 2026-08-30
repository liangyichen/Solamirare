namespace Solamirare;

public unsafe partial struct ValueFrozenStack<T>
where T : unmanaged
{



    /// <summary>
    /// 清理所有节点，立即可以重用
    /// </summary>
    public void Clear()
    {
        _count = 0;
    }




}