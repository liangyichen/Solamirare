
public static unsafe class MemoryPoolClusterTest
{
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
}