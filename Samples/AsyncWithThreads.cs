

/// <summary>
/// 由线程池驱动的通用异步
/// </summary>
public unsafe class AsyncWithThreadsTest
{

    [UnmanagedCallersOnly]
    static void OnComplete(void* userData)
    {
        int* value = (int*)userData;

        Console.WriteLine($"✓ 完成！值: {*value}");
    }


    public static void Run()
    {
        Solamirare.ValueTask asyncOp = new Solamirare.ValueTask();

        int* data = stackalloc int[1];
        *data = 999;

        asyncOp.BeginAsync(&OnComplete, data);

        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine($"Main Thread Work : {i}");
        }

        //asyncOp.Wait(); //根据业务需求决定是否执行 Wait()

        *data = 666;

        Solamirare.ValueTask asyncOp2 = new Solamirare.ValueTask();

        asyncOp2.BeginAsync(&OnComplete, data);


        for (int i = 5; i < 10; i++)
        {
            Console.WriteLine($"Main Thread Work : {i}");
        }

        //asyncOp2.Wait(); //根据业务需求决定是否执行 Wait()

        Console.ReadLine();
    }
}
