namespace Solamirare;



/// <summary>
/// Linux 版本的零 GC 异步
/// </summary>
internal unsafe struct AsyncWithThreadLinux
{

    internal AsyncCallbackContext CallbackContext;
    private AsyncStateOnPosix _state;

    private IntPtr _threadHandle;

    /// <summary>
    /// 初始化 Linux 版本的零 GC 异步实例。
    /// </summary>
    public void Init()
    {
        _state = default;

        // 创建管道用于线程通信
        int* fds = stackalloc int[2];
        if (LinuxAPI.pipe(fds) != 0) throw new InvalidOperationException("Failed to create pipe");

        fixed (AsyncWithThreadLinux* self = &this)
        {
            LinuxAPI.pthread_mutex_init(&self->_state.CompletionMutex, null);
            LinuxAPI.pthread_cond_init(&self->_state.CompletionCond, null);

            _state.ReadFd = fds[0];
            _state.WriteFd = fds[1];

            int result;


            IntPtr* p_threadHandle = &self->_threadHandle;

            result = LinuxAPI.pthread_create(
                (void**)p_threadHandle,
                null,
                &WorkerThreadEntry,
                &self->_state
            );


            if (result != 0)
                throw new InvalidOperationException("Failed to create pthread");
        }

    }

    /// <summary>
    /// 异步执行回调。
    /// </summary>
    /// <param name="callback">
    /// 回调函数指针。
    /// </param>
    /// <param name="userData">
    /// 用户数据指针。
    /// </param>
    public void BeginAsync(
        delegate* unmanaged<void*, void> callback,
        void* userData = null)
    {
        _state.Callback = callback;
        _state.UserData = userData;
        // 确保所有参数写入完成后，再设置 State 标志
        Volatile.Write(ref _state.State, 1);

        // 写入管道唤醒线程
        byte b = 1;
        LinuxAPI.write(_state.WriteFd, &b, 1);
    }

    [UnmanagedCallersOnly]
    private static void* WorkerThreadEntry(void* arg)
    {
        AsyncStateOnPosix* state = (AsyncStateOnPosix*)arg;
        PollFd pfd;
        pfd.Fd = state->ReadFd;
        pfd.Events = 1; // POLLIN
        pfd.Revents = 0;

        while (Volatile.Read(ref state->ShouldStop) == 0)
        {
            int waitTime = -1; // Infinite

            if (Volatile.Read(ref state->State) == 1)
            {
                Volatile.Write(ref state->State, 2);
                if (state->Callback != null)
                {
                    state->Callback(state->UserData);
                }
                LinuxAPI.pthread_mutex_lock(&state->CompletionMutex);
                state->State = 0;
                LinuxAPI.pthread_cond_signal(&state->CompletionCond);
                LinuxAPI.pthread_mutex_unlock(&state->CompletionMutex);
                continue;
            }

            // 使用 Poll 等待管道可读或超时
            LinuxAPI.poll(&pfd, 1, waitTime);

            // 如果管道有数据（被唤醒），读取它以清空缓冲区
            if ((pfd.Revents & 1) != 0)
            {
                byte b;
                LinuxAPI.read(state->ReadFd, &b, 1);
            }
        }

        LinuxAPI.pthread_mutex_lock(&state->CompletionMutex);
        state->IsFinished = 1;
        LinuxAPI.pthread_cond_signal(&state->CompletionCond);
        LinuxAPI.pthread_mutex_unlock(&state->CompletionMutex);
        return null;
    }

    /// <summary>
    /// 等待当前任务完成，但不销毁线程。
    /// </summary>
    public void Wait()
    {
        fixed (AsyncStateOnPosix* state = &_state)
        {
            LinuxAPI.pthread_mutex_lock(&state->CompletionMutex);

            while (state->State != 0)
            {
                LinuxAPI.pthread_cond_wait(&state->CompletionCond, &state->CompletionMutex);
            }

            LinuxAPI.pthread_mutex_unlock(&state->CompletionMutex);
        }
    }

    /// <summary>
    /// 释放资源并停止后台线程。
    /// </summary>
    /// <param name="gracefulShutdownTimeoutMilliseconds">
    /// 优雅退出的最大等待时间（毫秒）。如果超时，线程将被强制终止。默认 3000ms。
    /// </param>
    public void Dispose(int gracefulShutdownTimeoutMilliseconds = 3000)
    {
        Wait();

        _state.ShouldStop = 1;

        byte b = 1;
        LinuxAPI.write(_state.WriteFd, &b, 1);

        fixed (AsyncStateOnPosix* state = &_state)
        {
            LinuxAPI.pthread_mutex_lock(&state->CompletionMutex);

            TimeSpec timeout = TimeSpec.FromMillisecondsFromNow(gracefulShutdownTimeoutMilliseconds);

            while (_state.IsFinished == 0)
            {
                int ret = LinuxAPI.pthread_cond_timedwait(&state->CompletionCond, &state->CompletionMutex, &timeout);
                if (ret == LinuxAPI.ETIMEDOUT) break;
            }

            bool finished = _state.IsFinished == 1;

            LinuxAPI.pthread_mutex_unlock(&state->CompletionMutex);

            if (!finished)
            {
                LinuxAPI.pthread_cancel(_threadHandle);
            }

            void* retval;
            LinuxAPI.pthread_join((void*)_threadHandle, &retval);

            LinuxAPI.pthread_cond_destroy(&state->CompletionCond);
            LinuxAPI.pthread_mutex_destroy(&state->CompletionMutex);
        }

        LinuxAPI.close(_state.ReadFd);
        LinuxAPI.close(_state.WriteFd);
    }
}
