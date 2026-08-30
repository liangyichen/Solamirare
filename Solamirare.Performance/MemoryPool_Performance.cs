


[MemoryDiagnoser]
[Config(typeof(DebugBuildConfig))]
public unsafe class MemoryPool_Performance
{

    [Benchmark]
    public void BaseMemoryPool_Scale()
    {
        MemoryCubeManagerTests.BaseMemoryPool_Scale();
    }
}