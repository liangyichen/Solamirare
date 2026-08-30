namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{

    /// <summary>
    /// 移除指定地址的元素（单次匹配）
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Remove(T* target)
    {
        if (target is null) return false;

        bool result;

        int index = IndexOf(target);

        if (index >= 0)
            result = SetAsFree(index);
        else
            result = false;


        return result;
    }

    /// <summary>
    /// 移除元素（值等同判断，单次匹配）
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool Remove(in T target)
    {
        fixed (T* p_value = &target)
        {
            return Remove(p_value);
        }
    }

}