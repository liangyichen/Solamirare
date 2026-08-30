namespace Solamirare;


/// <summary>
/// 表示线程池任务链表中的节点。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeThreadNode
{
    /// <summary>当前节点保存的任务。</summary>
    public NativeThreadTask Task;

    /// <summary>下一个任务节点。</summary>
    public NativeThreadNode* Next;
}
