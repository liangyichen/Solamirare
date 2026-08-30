namespace Solamirare;

/// <summary>
/// 协程调试信息，记录 Resume / Yield 次数及异常检测结果。
/// <para>仅在编译时定义 COROUTINE_DEBUG 宏时有效。</para>
/// </summary>
public unsafe struct CoroutineDebugInfo
{
    /// <summary>
    /// Resume 被成功调用的次数。
    /// 正常情况下等于 YieldCount（协程未完成）或 YieldCount + 1（协程已完成）。
    /// </summary>
    public int ResumeCount;

    /// <summary>
    /// Yield 被调用的次数。
    /// 正常情况下等于 ResumeCount（未完成）或 ResumeCount - 1（已完成）。
    /// </summary>
    public int YieldCount;

    /// <summary>
    /// 对已完成的协程调用 Resume 的次数。
    /// 大于 0 说明调用方存在逻辑错误（在 IsFinished 后仍尝试 Resume）。
    /// </summary>
    public int ResumeAfterFinishedCount;

    /// <summary>
    /// Resume / Yield 计数不匹配的次数。
    /// 大于 0 说明上下文切换出现异常，调度器状态可能已损坏。
    /// </summary>
    public int MismatchCount;

    /// <summary>
    /// 输出调试信息摘要。
    /// </summary>
    public readonly string Summary =>
        $"Resume={ResumeCount} Yield={YieldCount} " +
        $"ResumeAfterFinished={ResumeAfterFinishedCount} " +
        $"Mismatch={MismatchCount}";
}