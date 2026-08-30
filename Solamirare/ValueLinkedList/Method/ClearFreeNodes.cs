namespace Solamirare;

public unsafe partial struct ValueLinkedList<T>
 where T : unmanaged
{

    /// <summary>
    /// 清除并释放空闲节点占据的空间
    /// </summary>
    public void ClearFreeNodes()
    {
        // 此处调用 _dispose 仅释放 isLocalNode=true 的节点（独立堆分配的节点）。
        // isLocalNode=false 的节点（来自预分配内存块）会被跳过，保证了隔离性。
        _dispose(_freeNodesHead);

        _freeNodesHead = null;
        _freeNodesCount = 0;
    }

}