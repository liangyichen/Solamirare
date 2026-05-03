namespace Solamirare;



/// <summary>
/// Windows 平台上的异步状态机
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct AsyncStateOnWindows
{
    public delegate* unmanaged<void*, void> Callback;
    public void* UserData;

    /// <summary>用于唤醒工作线程的事件句柄（AutoResetEvent）。</summary>
    public nint WaitHandle;

    /// <summary>用于通知主线程回调已完成的事件句柄（ManualResetEvent）。</summary>
    public nint CompletionEvent;

    public int State;
    public int ShouldStop;
    public int IsFinished;
}