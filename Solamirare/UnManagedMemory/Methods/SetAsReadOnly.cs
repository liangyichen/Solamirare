namespace Solamirare;

public unsafe partial struct UnManagedMemory<T>
where T : unmanaged
{

    /// <summary>
    /// 把当前对象设置为只读，不允许添加与重设大小，仅允许在原始长度范围内的修改内容。
    /// </summary>
    public void SetAsReadOnly()
    {
        if (activated)
            @readonly = true;
    }

    /// <summary>
    /// 解除 ReadOnly 的状态，只有在之前通过 SetAsReadOnly 把对象设为只读后，才应该调用这个方法来解除。
    /// <para>如果错误的针对某个逻辑上必须是 ReadOnly 的对象进行解除，有可能引起进程奔溃。</para>
    /// </summary>
    public void UnlockReadOnly()
    {
        if (activated)
            @readonly = false;
    }

}