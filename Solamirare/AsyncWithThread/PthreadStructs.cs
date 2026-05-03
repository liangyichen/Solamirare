namespace Solamirare;

// ────────────────────────────────────────────────────────────────────────────
//  pthread 原生结构体占位定义
//  macOS（arm64 / x86_64）ABI 布局：
//    pthread_mutex_t = 56 字节
//    pthread_cond_t  = 40 字节
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// pthread_mutex_t 的托管占位结构体。
/// 大小固定为 56 字节，与 macOS libc ABI 完全匹配。
/// 不要直接读写内部字段，始终通过 MacOSAPI.pthread_mutex_* 系列函数操作。
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 56)]
public struct PthreadMutex
{
    // 内部由 libc 管理，此处仅作内存占位。
}

/// <summary>
/// pthread_cond_t 的托管占位结构体。
/// 大小固定为 40 字节，与 macOS libc ABI 完全匹配。
/// 不要直接读写内部字段，始终通过 MacOSAPI.pthread_cond_* 系列函数操作。
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 40)]
public struct PthreadCond
{
    // 内部由 libc 管理，此处仅作内存占位。
}

// ────────────────────────────────────────────────────────────────────────────
//  timespec 结构体
//  pthread_cond_timedwait 接受的是绝对 UTC 时间，而非相对超时。
//  需要用 clock_gettime(CLOCK_REALTIME) 取当前时间再加上偏移量。
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// POSIX timespec 结构体，表示一个时间点（秒 + 纳秒）。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct TimeSpec
{
    /// <summary>秒部分。</summary>
    public long Seconds;

    /// <summary>纳秒部分（0 ~ 999_999_999）。</summary>
    public long Nanoseconds;

    /// <summary>
    /// 从当前时刻起，计算 <paramref name="milliseconds"/> 毫秒后的绝对时间点。
    /// 供 pthread_cond_timedwait 使用。
    /// </summary>
    public static unsafe TimeSpec FromMillisecondsFromNow(int milliseconds)
    {
        TimeSpec ts;
        // CLOCK_REALTIME = 0 on macOS
        MacOSAPI.clock_gettime(0, &ts);
        ts.Seconds      += milliseconds / 1000;
        ts.Nanoseconds  += (milliseconds % 1000) * 1_000_000L;
        // 纳秒进位
        if (ts.Nanoseconds >= 1_000_000_000L)
        {
            ts.Seconds     += 1;
            ts.Nanoseconds -= 1_000_000_000L;
        }
        return ts;
    }
}