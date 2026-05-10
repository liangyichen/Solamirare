namespace Solamirare;


/// <summary>
/// 运行时优化开关。
/// </summary>
public ref struct IoUringOptimizationOptions
{

    /// <summary>启用 SQPOLL。</summary>
    public  bool enable_submission_queue_polling;
    /// <summary>启用 fixed buffers。</summary>
    public  bool enable_fixed_buffers_registration;
    /// <summary>启用零拷贝发送。</summary>
    public  bool enable_zero_copy_send;
    /// <summary>io_uring SQ/CQ 深度。</summary>
    public  uint queue_depth_entries;
    

    /// <summary>
    /// 构建开关（默认全 true）。
    /// </summary>
    public IoUringOptimizationOptions(
        bool enableSqPoll = true,
        bool enableFixedBuffers = true,
        bool enableZeroCopy = true,
        uint queueDepth = 256)
    {
        enable_submission_queue_polling = enableSqPoll;
        enable_fixed_buffers_registration = enableFixedBuffers;
        enable_zero_copy_send = enableZeroCopy;
        queue_depth_entries = queueDepth;

    }
}
