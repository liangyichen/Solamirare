namespace Solamirare;


/// <summary>
/// MacOS 版本的零 GC 异步
/// </summary>
internal unsafe struct AsyncWithThreadMac
{

    internal AsyncCallbackContext CallbackContext;

    private AsyncStateOnPosix _state;

    private void* _threadHandle;


    /// <summary>
    /// 初始化 macOS 版本的零 GC 异步实例。
    /// </summary>
    public void Init()
    {
        _state = default;

        int* fds = stackalloc int[2];

        if (MacOSAPI.pipe(fds) != 0) throw new InvalidOperationException("Failed to create pipe");

        _state.ReadFd = fds[0];

        _state.WriteFd = fds[1];

        fixed (AsyncWithThreadMac* self = &this)
        {

            // 初始化条件变量和互斥锁
            MacOSAPI.pthread_mutex_init(&self->_state.CompletionMutex, null);

            MacOSAPI.pthread_cond_init(&self->_state.CompletionCond, null);

            int result;

            void** p_threadHandle = &self->_threadHandle;

            result = MacOSAPI.pthread_create(p_threadHandle, null, &WorkerThreadEntry, &self->_state);

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

        byte b = 1;
        MacOSAPI.write(_state.WriteFd, &b, 1);
    }

    [UnmanagedCallersOnly]
    private static void* WorkerThreadEntry(void* arg)
    {
        AsyncStateOnPosix* state = (AsyncStateOnPosix*)arg;
        PollFd pfd;
        pfd.Fd = state->ReadFd;
        pfd.Events = 1; // POLLIN
        pfd.Revents = 0;

        while (state->ShouldStop == 0)
        {
            int waitTime = -1;

            if (state->State == 1)
            {
                state->State = 2;
                if (state->Callback != null)
                {
                    state->Callback(state->UserData);
                }

                // 回调执行完毕，持锁修改 State 并发出信号，唤醒 Wait() 中挂起的主线程
                // 锁在这里的真实身份不是"互斥保护"，而是 pthread_cond_wait 协议里防止丢失唤醒的结构性要求，只是恰好借用了 mutex 这个载体。
                MacOSAPI.pthread_mutex_lock(&state->CompletionMutex);
                state->State = 0;
                state->IsFinished = 1;
                MacOSAPI.pthread_cond_signal(&state->CompletionCond);
                MacOSAPI.pthread_mutex_unlock(&state->CompletionMutex);

                continue;
            }

            MacOSAPI.poll(&pfd, 1, waitTime);

            if ((pfd.Revents & 1) != 0)
            {
                byte b;
                MacOSAPI.read(state->ReadFd, &b, 1);
            }
        }

        // 线程退出时同样通过条件变量通知，确保 Dispose() 中的等待能被唤醒
        MacOSAPI.pthread_mutex_lock(&state->CompletionMutex);
        state->IsFinished = 1;
        MacOSAPI.pthread_cond_signal(&state->CompletionCond);
        MacOSAPI.pthread_mutex_unlock(&state->CompletionMutex);

        return null;
    }

    /// <summary>
    /// 等待当前任务完成，但不销毁线程。
    /// </summary>
    public void Wait()
    {
        fixed (AsyncWithThreadMac* self = &this)
        {
            MacOSAPI.pthread_mutex_lock(&self->_state.CompletionMutex);

            // pthread_cond_wait 的标准范式：必须在循环中检查条件，防止虚假唤醒
            while (Volatile.Read(ref _state.State) != 0)
            {
                // 原子地：释放锁 + 挂起当前线程，等待工作线程 signal
                // 被唤醒后自动重新持锁，再检查一次循环条件
                MacOSAPI.pthread_cond_wait(&self->_state.CompletionCond, &self->_state.CompletionMutex);
            }

            MacOSAPI.pthread_mutex_unlock(&self->_state.CompletionMutex);
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

        Volatile.Write(ref _state.ShouldStop, 1);

        byte b = 1;
        MacOSAPI.write(_state.WriteFd, &b, 1);

        fixed (AsyncWithThreadMac* self = &this)
        {

            // 用条件变量带超时等待工作线程退出，替换原来的 Sleep 轮询
            MacOSAPI.pthread_mutex_lock(&self->_state.CompletionMutex);

            bool finished = false;
            TimeSpec timeout = TimeSpec.FromMillisecondsFromNow(gracefulShutdownTimeoutMilliseconds);

            while (_state.IsFinished == 0)
            {
                int ret = MacOSAPI.pthread_cond_timedwait(&self->_state.CompletionCond, &self->_state.CompletionMutex, &timeout);
                if (ret == MacOSAPI.ETIMEDOUT)
                {
                    break;
                }
            }

            finished = _state.IsFinished == 1;
            MacOSAPI.pthread_mutex_unlock(&self->_state.CompletionMutex);

            if (!finished)
            {
                MacOSAPI.pthread_cancel(_threadHandle);
            }

            void* retval;
            MacOSAPI.pthread_join(_threadHandle, &retval);

            // 销毁条件变量和互斥锁
            MacOSAPI.pthread_cond_destroy(&self->_state.CompletionCond);
            MacOSAPI.pthread_mutex_destroy(&self->_state.CompletionMutex);

            MacOSAPI.close(self->_state.ReadFd);
            MacOSAPI.close(self->_state.WriteFd);
            
            
        }
    }
}