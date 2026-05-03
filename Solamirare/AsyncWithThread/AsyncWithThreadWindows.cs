using System.Runtime.CompilerServices;

namespace Solamirare;


/// <summary>
/// Windows 版本的零 GC 异步
/// </summary>
internal unsafe struct AsyncWithThreadWindows
{

    internal AsyncCallbackContext CallbackContext;
    private AsyncStateOnWindows* _state;
    private IntPtr _threadHandle;

    /// <summary>
    /// 初始化 Windows 版本的零 GC 异步实例。
    /// </summary>
    public void Init()
    {
        _state = (AsyncStateOnWindows*)NativeMemory.AllocZeroed((nuint)sizeof(AsyncStateOnWindows));
        *_state = default;

        // 唤醒工作线程用的事件（AutoResetEvent）
        _state->WaitHandle = WindowsAPI.CreateEventW(IntPtr.Zero, false, false, IntPtr.Zero);
        if (_state->WaitHandle == IntPtr.Zero) throw new InvalidOperationException("Failed to create WaitHandle event");

        // 通知主线程回调完成用的事件（ManualResetEvent，初始无信号）
        _state->CompletionEvent = WindowsAPI.CreateEventW(IntPtr.Zero, true, false, IntPtr.Zero);
        if (_state->CompletionEvent == IntPtr.Zero) throw new InvalidOperationException("Failed to create CompletionEvent");

        uint threadId;
        _threadHandle = WindowsAPI.CreateThread(
            null,
            0,
            &WorkerThreadEntry,
            _state,
            0,
            &threadId
        );

        if (_threadHandle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create thread");
    }

    /// <summary>
    /// 异步执行回调。
    /// </summary>
    public void BeginAsync(
        delegate* unmanaged<void*, void> callback,
        void* userData = null)
    {
        _state->Callback = callback;
        _state->UserData = userData;

        // 重置 CompletionEvent 为无信号状态，准备下一次等待
        WindowsAPI.ResetEvent(_state->CompletionEvent);

        Volatile.Write(ref _state->State, 1);

        WindowsAPI.SetEvent(_state->WaitHandle);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static uint WorkerThreadEntry(void* arg)
    {
        var state = (AsyncStateOnWindows*)arg;

        while (Volatile.Read(ref state->ShouldStop) == 0)
        {
            if (Volatile.Read(ref state->State) == 1)
            {
                Volatile.Write(ref state->State, 2);
                if (state->Callback != null)
                {
                    state->Callback(state->UserData);
                }
                Volatile.Write(ref state->State, 0);

                // 回调完成，通知主线程
                WindowsAPI.SetEvent(state->CompletionEvent);

                continue;
            }

            WindowsAPI.WaitForSingleObject(state->WaitHandle, 0xFFFFFFFF);
        }

        state->IsFinished = 1;

        // 线程退出时也发出信号，确保 Dispose() 中的等待能被唤醒
        WindowsAPI.SetEvent(state->CompletionEvent);

        return 0;
    }

    /// <summary>
    /// 等待当前任务完成，但不销毁线程。
    /// </summary>
    public void Wait()
    {
        if (_state != null)
        {
            // 零消耗挂起，直到工作线程 SetEvent(CompletionEvent)
            WindowsAPI.WaitForSingleObject(_state->CompletionEvent, 0xFFFFFFFF);
        }
    }

    /// <summary>
    /// 释放资源并停止后台线程。
    /// </summary>
    public void Dispose(int gracefulShutdownTimeoutMilliseconds = 3000)
    {
        if (_state != null)
        {
            Wait();

            Volatile.Write(ref _state->ShouldStop, 1);

            // 唤醒工作线程让它检查 ShouldStop
            WindowsAPI.SetEvent(_state->WaitHandle);

            // 带超时等待工作线程退出
            uint waitResult = WindowsAPI.WaitForSingleObject(_threadHandle, (uint)gracefulShutdownTimeoutMilliseconds);

            if (waitResult == 0x102) // WAIT_TIMEOUT
            {
                WindowsAPI.TerminateThread(_threadHandle, 0);
            }

            WindowsAPI.CloseHandle(_threadHandle);
            WindowsAPI.CloseHandle(_state->WaitHandle);
            WindowsAPI.CloseHandle(_state->CompletionEvent);

            NativeMemory.Free(_state);
            _state = null;
        }
    }
}