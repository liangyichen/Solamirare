
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;





public class DebugBuildConfig : ManualConfig
{
    public DebugBuildConfig()
    {
        
        Job job = Job.Default
        
            .WithCustomBuildConfiguration("Debug")
            .WithLaunchCount(1)
            .WithWarmupCount(0)
            .WithIterationCount(1)
            ;


        AddJob(job);
    }
}




/// <summary>
/// IO 的测试比较特殊：因为涉及到 P/Invoke ，在 macOS 上面 BenchmarkDotNet 的默认 Release 再次优化编译会破坏内存（Windows 与 Fedora 则正常）
/// <para>所以必须运行于非优化的模式下</para>
/// <para>使用 BenchmarkDotNet 监控 IO 操作的主要目的并不是查看性能，而是了解是否存在 GC 分配，这不会影响</para>
/// </summary>
[MemoryDiagnoser]
[Config(typeof(DebugBuildConfig))]
public unsafe class UnManagedIO_Performance
{


    [Benchmark]
    public void ReadTextFile()
    {
        IO_Test.ReadTextFile();
    }


    [Benchmark]
    public void WriteTextToFile()
    {
        IO_Test.WriteTextToFile();
    }
    

    [Benchmark]
    public void FileExists()
    {
        IO_Test.FileExists();
    }
    

    [Benchmark]
    public void DeleteFile()
    {
        IO_Test.DeleteFile();
    }
}