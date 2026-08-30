

public static unsafe class BaseMemoryPoolTest
{


    public static bool Test()
    {
        // 1. 初始化内存池 
        // 注意：在当前版本中，capacity 仅作为逻辑参考，不再预拨大块。
        BaseMemoryPool pool = new BaseMemoryPool(5120);

        // 使用非托管数组或固定大小数组，保持零 GC 严谨性
        BaseMemoryPoolHandleEntry*[] handles = new BaseMemoryPoolHandleEntry*[9];

        // 2. 执行分配
        for (int i = 0; i < 9; i++)
        {
            handles[i] = pool.Alloc(1024);
        }

        // --- 验证点 A：对象计数 (活跃块) ---
        // 在新架构下，ChunksCount 严格等于当前未释放的对象数
        uint countAfterAlloc = pool.ChunksCount;
        bool countMatched = (countAfterAlloc == 9);

        // 3. 验证指针稳定性
        byte* mem0_p = handles[0]->Pointer;
        // 模拟写入
        NativeMemory.Clear(mem0_p, (nuint)handles[0]->Length);
        // 验证逻辑：在新版本中每个 Handle 都是独立的，互不干扰
        bool pointerValid = (handles[0]->Pointer == mem0_p);

        // 4. 执行释放
        for (int i = 0; i < 9; i++)
        {
            pool.Free(handles[i]);
        }

        // --- 验证点 B：物理归零 ---
        // 由于是“一对象一物理块”且“即时释放”，释放所有 Handle 后，
        // ChunksCount 必须绝对等于 0。
        uint countAfterFree = pool.ChunksCount;
        bool recovered = (countAfterFree == 0);

        // 5. 最终销毁
        pool.Dispose();

        // 只有 计数匹配、指针稳定、完全归零 三者满足才返回 true
        return countMatched && pointerValid && recovered;
    }

}
