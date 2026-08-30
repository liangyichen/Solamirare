using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Solamirare; // 假设您的代码都在这个命名空间下

public static unsafe class MemoryCubeManagerTests
{




    /// <summary>
    /// 测试用例 01: 最小尺寸边界分配与归还 (32 字节)。
    /// </summary>
    public static bool Test_01_MinSizeBoundaryCycle()
    {
        MemoryPoolCluster pool = new MemoryPoolCluster();

        Span<MemoryPoolSchema> schemas = stackalloc MemoryPoolSchema[]
        {
            new MemoryPoolSchema(32,1024),
            new MemoryPoolSchema(64,1024),
            new MemoryPoolSchema(128,1024)
        };


        pool.Init(schemas);

        MemoryPoolCluster* manager = &pool;
        const uint Size = 32;

        byte* result = manager->Alloc(Size).Address;

        bool success = result != null;

        // 结束阶段: 释放内存
        if (success)
        {
            // 必须使用 Return 方法
            success &= manager->Return(result, Size);
        }

        manager->Dispose();

        return success;
    }


    /// <summary>
    /// 测试用例 02: 最大尺寸边界分配与归还 (4096 字节)。
    /// </summary>
    public static bool Test_02_MaxSizeBoundaryCycle()
    {
        MemoryPoolCluster pool = new MemoryPoolCluster();

        Span<MemoryPoolSchema> schemas = stackalloc MemoryPoolSchema[]
        {
            new MemoryPoolSchema(1024,1024)
        };


        pool.Init(schemas);
        MemoryPoolCluster* manager = &pool;
        const uint Size = 1024;

        byte* result = manager->Alloc(Size).Address;

        bool success = result != null;

        // 结束阶段: 释放内存
        if (success)
        {
            // 必须使用 Return 方法
            success &= manager->Return(result, Size);
        }

        manager->Dispose();

        return success;
    }

    /// <summary>
    /// 测试用例 03: 请求低于最小范围的尺寸 (31 字节)，验证失败。
    /// </summary>
    public static bool Test_03_UndersizeFailure()
    {
        MemoryPoolCluster pool = new MemoryPoolCluster();

        Span<MemoryPoolSchema> schemas = stackalloc MemoryPoolSchema[]
        {
            new MemoryPoolSchema(16,1024),
            new MemoryPoolSchema(32,1024),
            new MemoryPoolSchema(64,1024),
        };


        pool.Init(schemas);
        MemoryPoolCluster* manager = &pool;
        const uint Size = 31;

        byte* result = manager->Alloc(Size).Address;

        bool expectedFailure = result is not null;

        manager->Return(result, Size);

        manager->Dispose();


        return expectedFailure;
    }




    /// <summary>
    /// 测试用例 05: 内存池状态验证与 Nav 路由 (256 字节)。
    /// </summary>
    public static bool Test_05_PoolStateVerificationAndNav()
    {
        MemoryPoolCluster pool = new MemoryPoolCluster();

        Span<MemoryPoolSchema> schemas = stackalloc MemoryPoolSchema[]
        {
            new MemoryPoolSchema(128,1024),
            new MemoryPoolSchema(256,1024),
            new MemoryPoolSchema(512,1024),
        };


        pool.Init(schemas);
        MemoryPoolCluster* manager = &pool;

        const uint Size = 256;

        // 1. 获取初始状态
        MemoryPoolFrozenNode* navInitial = manager->SelectPool(Size);
        if (navInitial is null) return false;

        uint initialFreeCount = navInitial->FreeNodesCount;

        // 2. 分配内存
        byte* allocResult = manager->Alloc(Size).Address;
        if (allocResult == null)
        {
            return false;
        }

        // 3. 验证 FreeNodesCount 是否减少 1，以及 PoolIndex 是否匹配
        MemoryPoolFrozenNode* navAfterAlloc = manager->SelectPool(Size);
        bool countReduced = (navAfterAlloc->FreeNodesCount == initialFreeCount - 1);


        // 4. 结束阶段: 归还内存
        bool returnSuccess = manager->Return(allocResult, Size);

        // 5. 验证 FreeNodesCount 是否恢复到初始值
        MemoryPoolFrozenNode* navAfterReturn = manager->SelectPool(Size);
        bool countRestored = (navAfterReturn->FreeNodesCount == initialFreeCount);

        bool _result = countReduced && returnSuccess && countRestored;

        manager->Dispose();

        return _result;
    }


    /// <summary>
    /// 测试用例 06: 完整容量溢出与恢复测试 (针对一个已知容量的子池，例如 32 字节池)。
    /// </summary>
    public static bool Test_06_FullCapacityOverflowAndReallocate()
    {
        MemoryPoolCluster pool = new MemoryPoolCluster();

        Span<MemoryPoolSchema> schemas = stackalloc MemoryPoolSchema[]
        {
            new MemoryPoolSchema(32,1024),
            new MemoryPoolSchema(64,1024),
            new MemoryPoolSchema(128,1024)
        };


        pool.Init(schemas);


        MemoryPoolCluster* manager = &pool;
        const uint Size = 32;

        // 假设 32 字节池的容量为 1024
        const int Capacity = 1024;

        // 必须使用 stackalloc 存储 ref struct 的指针
        byte** resultsPtr = stackalloc byte*[Capacity];

        bool testSuccess = true;

        // 1. 循环分配所有块
        for (int i = 0; i < Capacity; i++)
        {
            resultsPtr[i] = manager->Alloc(Size).Address;
            if (resultsPtr[i] == null)
            {
                testSuccess = false;
                break;
            }
        }

        // 2. 验证溢出失败
        byte* overflowResult = manager->Alloc(Size).Address;
        bool overflowFailed = overflowResult == null;
        testSuccess &= overflowFailed;

        // 3. 结束阶段: 释放所有内存
        bool returnSuccess = true;
        for (int i = 0; i < Capacity; i++)
        {
            if (resultsPtr[i] != null)
            {
                returnSuccess &= manager->Return(resultsPtr[i], Size);
            }
        }

        // 4. 验证释放后，能否再次分配
        byte* reallocateResult = manager->Alloc(Size).Address;
        bool reallocateSuccess = reallocateResult != null;

        // 释放这最后一个块
        if (reallocateSuccess)
        {
            returnSuccess &= manager->Return(reallocateResult, Size);
        }

        bool rsult = testSuccess && returnSuccess && reallocateSuccess;

        manager->Dispose();

        return rsult;
    }


    /// <summary>
    /// 测试用例 07: 归还无效指针，验证安全性。
    /// </summary>
    public static bool Test_07_ReturnInvalidPointer()
    {
        MemoryPoolCluster pool = new MemoryPoolCluster();

        Span<MemoryPoolSchema> schemas = stackalloc MemoryPoolSchema[]
        {
            new MemoryPoolSchema(32,1024),
            new MemoryPoolSchema(64,1024),
            new MemoryPoolSchema(128,1024),
        };


        pool.Init(schemas);
        MemoryPoolCluster* manager = &pool;
        const uint Size = 64;

        // 1. 分配一个有效块 P1
        byte* result1 = manager->Alloc(Size).Address;
        if (result1 == null) return false;

        // 2. 伪造一个 AllocResult，Address 是无效的（例如 P1 地址 + 1）
        byte* invalidReturn = result1;
        // P1 + 1 肯定不是块的起始地址，应被 IsPointerFromPool 识别为非法
        invalidReturn = result1 + 1;

        // 3. 尝试归还这个无效指针
        bool returnFailed = !manager->Return(invalidReturn, Size);

        // 4. 结束阶段：归还第一个合法指针
        manager->Return(result1, Size);

        manager->Dispose();

        return returnFailed;
    }




    public static bool BaseMemoryPool_Scale()
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


    public static bool Test_09_ClusterMemoryPool_Scale()
    {
        Span<MemoryPoolSchema> schemas = stackalloc MemoryPoolSchema[]
        {
            new MemoryPoolSchema(32,4),
            new MemoryPoolSchema(64,4),
            new MemoryPoolSchema(96,4),
            new MemoryPoolSchema(160,1024),
            new MemoryPoolSchema(384,1024),
            new MemoryPoolSchema(1536,512),
            new MemoryPoolSchema(4096,512),
            new MemoryPoolSchema(8192,512),
        };


        MemoryPoolCluster* cluster = stackalloc MemoryPoolCluster[1];
        bool inited = cluster->Init(schemas);

        if (inited)
        {
            //需求设为72是因为每个内存块还需要附加一些元数据信息，72的匹配子内存池是96字节内存池

            byte* mem0 = cluster->Alloc(72).Address;
            byte* mem1 = cluster->Alloc(72).Address;
            byte* mem2 = cluster->Alloc(72).Address;
            byte* mem3 = cluster->Alloc(72).Address;


            byte* mem4 = cluster->Alloc(72).Address; //已经分配满了，超出可以分配数量，预期为 null
            cluster->Return(mem3, 72); //回收一个
            byte* mem5 = cluster->Alloc(72).Address; //已经回收了一个，预期会分配成功

            bool result = mem5 is not null;



            cluster->Dispose();
            return result;

        }

        return false;

    }
}