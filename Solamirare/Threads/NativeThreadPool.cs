namespace Solamirare;



/// <summary>
/// 线程池
/// </summary>
public unsafe struct NativeThreadPool
{
    /// <summary>无限等待超时时间常量。</summary>
    const uint INFINITE = 0xFFFFFFFF;

    ThreadPoolState* _state;

    const int LockSize = 64;

    const int SignalSize = 64;


    /// <summary>
    /// 初始化线程池
    /// </summary>
    /// <param name="threadCount">工作线程数量</param>
    /// <param name="queueSize">任务队列容量</param>
    public void Init(int threadCount = 4, int queueSize = 1024)
    {
        if (_state != null) return;

        // 1. 分配状态内存
        _state = (ThreadPoolState*)NativeMemory.AllocZeroed((nuint)sizeof(ThreadPoolState));
        _state->MaxFreeCount = queueSize; // 使用 queueSize 作为节点池的最大缓存数量
        _state->ThreadCount = threadCount;
        _state->IsShutdown = 0;

        // 2. 任务队列初始化 (链表默认为空)
        _state->Head = null;
        _state->Tail = null;
        _state->FreeHead = null;

        // 3. 初始化同步原语
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: CriticalSection (64 bytes safe margin) + Semaphore

            _state->Lock = NativeMemory.AllocZeroed(LockSize);

            WindowsAPI.InitializeCriticalSection(_state->Lock);

            // CreateSemaphore(attr, initial, max, name)
            _state->Signal = WindowsAPI.CreateSemaphoreW(null, 0, queueSize, null);
        }
        else
        {
            // POSIX: Mutex (64 bytes) + Cond (64 bytes)
            _state->Lock = NativeMemory.AllocZeroed(LockSize);
            _state->Signal = NativeMemory.AllocZeroed(SignalSize);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                LinuxAPI.pthread_mutex_init(_state->Lock, null);
                LinuxAPI.pthread_cond_init(_state->Signal, null);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                LinuxAPI.pthread_mutex_init(_state->Lock, null);
                LinuxAPI.pthread_cond_init(_state->Signal, null);
            }
        }

        // 4. 创建工作线程
        _state->ThreadHandles = (void**)NativeMemory.AllocZeroed((nuint)(sizeof(void*) * threadCount));

        for (int i = 0; i < threadCount; i++)
        {
            void* handle;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                NativeThread.CreateOnWindows(out handle, &WindowsWorkerLoop, _state);
            }
            else
            {
                NativeThread.CreateOnPosix(out handle, &PosixWorkerLoop, _state);
            }
            _state->ThreadHandles[i] = handle;
        }
    }

    /// <summary>
    /// 提交任务到线程池
    /// </summary>
    public bool Enqueue(delegate* unmanaged<void*, void> taskFunc, void* context)
    {
        if (_state == null || _state->IsShutdown != 0) return false;

        bool enqueued = false;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsAPI.EnterCriticalSection(_state->Lock);

            // 尝试从池中获取节点
            NativeThreadNode* node = _state->FreeHead;
            if (node != null)
            {
                _state->FreeHead = node->Next;
                _state->FreeCount--;
            }
            else
            {
                node = (NativeThreadNode*)NativeMemory.AllocZeroed((nuint)sizeof(NativeThreadNode));
            }

            if (node != null)
            {
                node->Task.Function = taskFunc;
                node->Task.Context = context;
                node->Next = null;

                if (_state->Tail != null)
                {
                    _state->Tail->Next = node;
                }
                else
                {
                    _state->Head = node;
                }
                _state->Tail = node;
                _state->Count++;
                enqueued = true;
            }

            WindowsAPI.LeaveCriticalSection(_state->Lock);

            if (enqueued)
            {
                WindowsAPI.ReleaseSemaphore(_state->Signal, 1, null);
            }
        }
        else
        {
            // POSIX
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                LinuxAPI.pthread_mutex_lock(_state->Lock);
            else
                MacOSAPI.pthread_mutex_lock(_state->Lock);

            // 尝试从池中获取节点
            NativeThreadNode* node = _state->FreeHead;
            if (node != null)
            {
                _state->FreeHead = node->Next;
                _state->FreeCount--;
            }
            else
            {
                node = (NativeThreadNode*)NativeMemory.AllocZeroed((nuint)sizeof(NativeThreadNode));
            }

            if (node != null)
            {
                node->Task.Function = taskFunc;
                node->Task.Context = context;
                node->Next = null;

                if (_state->Tail != null)
                {
                    _state->Tail->Next = node;
                }
                else
                {
                    _state->Head = node;
                }
                _state->Tail = node;
                _state->Count++;
                enqueued = true;

                // Signal inside lock is safe and common
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    LinuxAPI.pthread_cond_signal(_state->Signal);
                else
                    MacOSAPI.pthread_cond_signal(_state->Signal);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                LinuxAPI.pthread_mutex_unlock(_state->Lock);
            else
                MacOSAPI.pthread_mutex_unlock(_state->Lock);
        }

        return enqueued;
    }

    /// <summary>
    /// 销毁线程池
    /// </summary>
    public void Dispose()
    {
        if (_state == null) return;

        // 1. 设置停止标志并唤醒所有线程
        // 注意：POSIX 下必须在锁内设置标志以避免 Lost Wakeup

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Volatile.Write(ref _state->IsShutdown, 1);
            // 释放足够的信号量唤醒所有线程
            WindowsAPI.ReleaseSemaphore(_state->Signal, _state->ThreadCount, null);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            LinuxAPI.pthread_mutex_lock(_state->Lock);
            Volatile.Write(ref _state->IsShutdown, 1);
            LinuxAPI.pthread_cond_broadcast(_state->Signal);
            LinuxAPI.pthread_mutex_unlock(_state->Lock);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            MacOSAPI.pthread_mutex_lock(_state->Lock);
            Volatile.Write(ref _state->IsShutdown, 1);
            MacOSAPI.pthread_cond_broadcast(_state->Signal);
            MacOSAPI.pthread_mutex_unlock(_state->Lock);
        }


        // 2. 等待所有线程结束
        for (int i = 0; i < _state->ThreadCount; i++)
        {
            NativeThread.Join(_state->ThreadHandles[i]);
        }

        // 3. 清理资源
        NativeMemory.Free(_state->ThreadHandles);

        // 清理任务队列中剩余的节点
        NativeThreadNode* current = _state->Head;
        while (current != null)
        {
            NativeThreadNode* next = current->Next;
            NativeMemory.Free(current);
            current = next;
        }

        // 清理节点池中缓存的节点
        current = _state->FreeHead;
        while (current != null)
        {
            NativeThreadNode* next = current->Next;
            NativeMemory.Free(current);
            current = next;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsAPI.DeleteCriticalSection(_state->Lock);
            WindowsAPI.CloseHandle(_state->Signal);
        }
        else
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                LinuxAPI.pthread_mutex_destroy(_state->Lock);
                LinuxAPI.pthread_cond_destroy(_state->Signal);
            }
            else
            {
                MacOSAPI.pthread_mutex_destroy(_state->Lock);
                MacOSAPI.pthread_cond_destroy(_state->Signal);
            }
        }

        NativeMemory.Free(_state->Lock);

        NativeMemory.Free(_state->Signal);

        NativeMemory.Free(_state);

        _state = null;
    }

    // --- Worker Loops ---

    [UnmanagedCallersOnly]
    private static void* PosixWorkerLoop(void* arg)
    {
        ThreadPoolState* state = (ThreadPoolState*)arg;

        while (true)
        {
            NativeThreadTask task = default;
            bool hasTask = false;

            // 1. 获取锁
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                LinuxAPI.pthread_mutex_lock(state->Lock);
            else
                MacOSAPI.pthread_mutex_lock(state->Lock);

            // 2. 等待条件：队列为空且未停止
            while (Volatile.Read(ref state->Count) == 0 && Volatile.Read(ref state->IsShutdown) == 0)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    LinuxAPI.pthread_cond_wait(state->Signal, state->Lock);
                else
                    MacOSAPI.pthread_cond_wait(state->Signal, state->Lock);
            }

            // 3. 检查是否停止
            if (Volatile.Read(ref state->IsShutdown) != 0 && Volatile.Read(ref state->Count) == 0)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    LinuxAPI.pthread_mutex_unlock(state->Lock);
                else
                    MacOSAPI.pthread_mutex_unlock(state->Lock);
                break; // 退出循环
            }

            // 4. 取出任务
            if (state->Count > 0)
            {
                NativeThreadNode* node = state->Head;
                task = node->Task;

                state->Head = node->Next;
                if (state->Head == null) state->Tail = null;
                state->Count--;
                hasTask = true;

                // 归还节点到池
                if (state->FreeCount < state->MaxFreeCount)
                {
                    node->Next = state->FreeHead;
                    state->FreeHead = node;
                    state->FreeCount++;
                }
                else
                {
                    NativeMemory.Free(node);
                }
            }

            // 5. 释放锁
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                LinuxAPI.pthread_mutex_unlock(state->Lock);
            else
                MacOSAPI.pthread_mutex_unlock(state->Lock);

            // 6. 执行任务 (无锁状态下)
            if (hasTask && task.Function != null)
            {
                task.Function(task.Context);
            }
        }

        return null;
    }


    [UnmanagedCallersOnly]
    private static uint WindowsWorkerLoop(void* arg)
    {
        ThreadPoolState* state = (ThreadPoolState*)arg;

        while (true)
        {
            // 1. 等待信号量 (有任务或停止信号)
            WindowsAPI.WaitForSingleObject(state->Signal, INFINITE);

            NativeThreadTask task = default;
            bool hasTask = false;

            // 2. 获取锁保护队列
            WindowsAPI.EnterCriticalSection(state->Lock);

            // 3. 检查是否停止
            if (Volatile.Read(ref state->IsShutdown) != 0 && Volatile.Read(ref state->Count) == 0)
            {
                WindowsAPI.LeaveCriticalSection(state->Lock);
                break; // 退出循环
            }

            // 4. 取出任务
            if (state->Count > 0)
            {
                NativeThreadNode* node = state->Head;
                task = node->Task;

                state->Head = node->Next;
                if (state->Head == null) state->Tail = null;
                state->Count--;
                hasTask = true;

                // 归还节点到池
                if (state->FreeCount < state->MaxFreeCount)
                {
                    node->Next = state->FreeHead;
                    state->FreeHead = node;
                    state->FreeCount++;
                }
                else
                {
                    NativeMemory.Free(node);
                }
            }

            WindowsAPI.LeaveCriticalSection(state->Lock);

            // 5. 执行任务
            if (hasTask && task.Function != null)
            {
                task.Function(task.Context);
            }
        }

        return 0;
    }
}
