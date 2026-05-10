namespace Solamirare;




/// <summary>
/// 基于线程池的异步操作。
/// </summary>
public unsafe struct ValueTask
{
    void* asyncObj;

    static AsyncWithThreadsPool pool;

    static ValueTask()
    {
        pool = new AsyncWithThreadsPool();
    }

    /// <summary>
    /// 初始化异步操作实例，从线程池获取线程。
    /// </summary>
    public ValueTask()
    {
        asyncObj = pool.Rent();
    }

    /// <summary>
    /// 异步执行回调。
    /// <para>回调函数会得到立即执行，在可以预见的回调函数一定会在后续逻辑中得到执行完毕的话，可以不需要后期调用 Wait()</para>
    /// </summary>
    /// <param name="callback">
    /// 回调函数指针。
    /// </param>
    /// <param name="userData">
    /// 用户数据指针。
    /// </param>
    public void BeginAsync(delegate* unmanaged<void*, void> callback, void* userData = null)
    {
        if (asyncObj == null) return;

        //编译器会自动移除不相关的代码，实际运行期间不会做多余的判断

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            AsyncWithThreadLinux* asyncOp = (AsyncWithThreadLinux*)asyncObj;
            AsyncCallbackContext* ctx = &asyncOp->CallbackContext;
            ctx->UserCallback = callback;
            ctx->UserData = userData;
            ctx->AsyncObj = asyncObj;
            asyncOp->BeginAsync(&AsyncCallbackWrapper, ctx);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AsyncWithThreadWindows* asyncOp = (AsyncWithThreadWindows*)asyncObj;
            AsyncCallbackContext* ctx = &asyncOp->CallbackContext;
            ctx->UserCallback = callback;
            ctx->UserData = userData;
            ctx->AsyncObj = asyncObj;
            asyncOp->BeginAsync(&AsyncCallbackWrapper, ctx);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            AsyncWithThreadMac* asyncOp = (AsyncWithThreadMac*)asyncObj;
            AsyncCallbackContext* ctx = &asyncOp->CallbackContext;
            ctx->UserCallback = callback;
            ctx->UserData = userData;
            ctx->AsyncObj = asyncObj;
            asyncOp->BeginAsync(&AsyncCallbackWrapper, ctx);
        }
    }


    [UnmanagedCallersOnly]
    private static void AsyncCallbackWrapper(void* userData)
    {
        AsyncCallbackContext* ctx = (AsyncCallbackContext*)userData;

        delegate* unmanaged<void*, void> userCallback = ctx->UserCallback;
        void* userDataPtr = ctx->UserData;
        void* asyncObj = ctx->AsyncObj;


        userCallback(userDataPtr);


        bool result = pool.Return(asyncObj);


    }


    /// <summary>
    /// 等待当前任务完成。当前调用者线程会挂起等待（零消耗），但是会阻塞
    /// </summary>
    public void Wait()
    {
        if (asyncObj != null)
        {
            //编译器会自动移除不相关的代码，实际运行期间不会做多余的判断

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                AsyncWithThreadLinux* asyncOp = (AsyncWithThreadLinux*)asyncObj;
                asyncOp->Wait();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AsyncWithThreadWindows* asyncOp = (AsyncWithThreadWindows*)asyncObj;
                asyncOp->Wait();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                AsyncWithThreadMac* asyncOp = (AsyncWithThreadMac*)asyncObj;
                asyncOp->Wait();
            }


            asyncObj = null;
        }
    }


}