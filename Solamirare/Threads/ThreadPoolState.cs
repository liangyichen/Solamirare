namespace Solamirare;


/// <summary>
/// 保存线程池的运行时状态。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ThreadPoolState
{

    // 任务队列 (链表)
    /// <summary>任务队列头节点。</summary>
    public NativeThreadNode* Head;


    /// <summary>任务队列尾节点。</summary>
    public NativeThreadNode* Tail;


    /// <summary>当前排队任务数量。</summary>
    public int Count;


    // 节点池 (链表栈) - 用于复用节点
    /// <summary>空闲节点链表头。</summary>
    public NativeThreadNode* FreeHead;


    // 同步原语指针
    /// <summary>同步锁对象指针。</summary>
    public void* Lock; // POSIX: pthread_mutex_t*, Windows: CRITICAL_SECTION*


    /// <summary>线程唤醒信号对象指针。</summary>
    public void* Signal; // POSIX: pthread_cond_t*, Windows: Semaphore Handle


    // 线程管理
    /// <summary>工作线程句柄数组。</summary>
    public void** ThreadHandles;


    /// <summary>当前空闲节点数量。</summary>
    public int FreeCount;


    /// <summary>空闲节点池允许缓存的最大数量。</summary>
    public int MaxFreeCount;


    // 控制标志
    /// <summary>线程池是否已进入关闭状态。</summary>
    public int IsShutdown;


    /// <summary>工作线程数量。</summary>
    public int ThreadCount;
}