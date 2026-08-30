namespace Solamirare;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// 提供 HTTP 日期头的 RFC 1123 格式生成工具。
/// </summary>
public static unsafe class HttpDateGenerator
{
    // 严格按照 HTTP RFC 1123 规范要求的英文缩写。
    private static readonly string[] DayNames =
        { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

    private static readonly string[] MonthNames =
        { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };


    /// <summary>
    /// 将整数值格式化为指定位数的字符，写入 Span
    /// </summary>
    /// <param name="span">目标 Span，必须至少有 count 位空间。</param>
    /// <param name="value">要写入的数值。</param>
    /// <param name="count">所需的位数（例如，2 用于 HH:mm:ss，4 用于 yyyy）。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteDigits(Span<char> span, int value, int count)
    {
        // 从右向左写入数字
        for (int i = 1; i <= count; i++)
        {
            int digit = value % 10;
            // 将数字转换为对应的 ASCII 字符 ('0' + digit)
            span[count - i] = (char)('0' + digit);
            value /= 10;
        }
    }


    /// <summary>
    /// 构建 GMT 时间字符串 字符串 (RFC 1123, GMT)。
    /// </summary>
    /// <param name="now"></param>
    /// <returns></returns>
    public static UnManagedString GMTSting(this DateTime now)
    {
        UnManagedString mem = new UnManagedString(29, 29);

        GMTSting(now, &mem.Prototype);

        return mem;
    }



    /// <summary>
    /// 构建 GMT 时间字符串 字符串 (RFC 1123, GMT)。
    /// </summary>
    /// <param name="now"></param>
    /// <param name="output">传入的内存段长度必须是29</param>
    static void GMTSting(this DateTime now, UnManagedCollection<char>* output)
    {
        // "ddd, dd MMM yyyy HH:mm:ss GMT" 的固定长度是 29

        if (output is null || output->Size != 29)
            return;

        DateTime nowUtc = DateTime.UtcNow;

        char* p = output->InternalPointer;

        // 写入 ", " 在位置 3
        p[3] = ',';
        p[4] = ' ';

        // 写入 " " 在位置 7, 11, 16, 25
        p[7] = ' ';
        p[11] = ' ';
        p[16] = ' ';
        p[25] = ' ';

        // 写入 ":" 在位置 19, 22
        p[19] = ':';
        p[22] = ':';

        // 写入 "GMT" 在位置 26-28
        p[26] = 'G';
        p[27] = 'M';
        p[28] = 'T';

        // 4. 写入动态日期部分

        // 4.1. 星期 (ddd) - 索引 0-2
        // 将 DayNames 字符串的 Span 拷贝到目标 Span
        ReadOnlySpan<char> dayAbbr = DayNames[(int)nowUtc.DayOfWeek];
        dayAbbr.CopyTo(output->Slice(0, 3));

        // 4.2. 日期 (dd) - 索引 5-6
        WriteDigits(output->Slice(5, 2), nowUtc.Day, 2);

        // 4.3. 月份 (MMM) - 索引 8-10
        ReadOnlySpan<char> monthAbbr = MonthNames[nowUtc.Month - 1];
        monthAbbr.CopyTo(output->Slice(8, 3));

        // 4.4. 年份 (yyyy) - 索引 12-15
        WriteDigits(output->Slice(12, 4), nowUtc.Year, 4);

        // 4.5. 小时 (HH) - 索引 17-18
        WriteDigits(output->Slice(17, 2), nowUtc.Hour, 2);

        // 4.6. 分钟 (mm) - 索引 20-21
        WriteDigits(output->Slice(20, 2), nowUtc.Minute, 2);

        // 4.7. 秒钟 (ss) - 索引 23-24
        WriteDigits(output->Slice(23, 2), nowUtc.Second, 2);

    }

    /// <summary>
    /// 构建 GMT 时间字节序列 (RFC 1123, GMT)。
    /// 输出格式: "ddd, dd MMM yyyy HH:mm:ss GMT"，固定 29 个 ASCII 字节。
    /// <br/>
    /// Writes the RFC 1123 GMT date/time as a fixed-length 29-byte ASCII sequence.
    /// </summary>
    /// <param name="now">当前时间（内部统一取 UtcNow）/ Current time (UtcNow is used internally)</param>
    /// <param name="output">
    /// 指向目标 <c>UnManagedMemory&lt;byte&gt;</c> 的指针，
    /// 其 <c>UsageSize</c> 必须恰好为 29。
    /// <br/>
    /// Pointer to the target buffer; <c>UsageSize</c> must be exactly 29.
    /// </param>
    public static void GMTBytes(this DateTime now, UnManagedMemory<byte>* output)
    {
        // "ddd, dd MMM yyyy HH:mm:ss GMT" 的固定长度是 29 字节（纯 ASCII，char == byte）
        if (output is null || output->UsageSize != 29)
            return;

        DateTime nowUtc = DateTime.UtcNow;

        byte* p = output->Pointer;

        // ── 固定字符（位置不随日期变化）─────────────────────────────────────────
        p[3] = (byte)',';   // ','
        p[4] = (byte)' ';  // ' '
        p[7] = (byte)' ';  // ' '
        p[11] = (byte)' ';  // ' '
        p[16] = (byte)' ';  // ' '
        p[19] = (byte)':';  // ':'
        p[22] = (byte)':';  // ':'
        p[25] = (byte)' ';  // ' '
        p[26] = (byte)'G';  // 'G'
        p[27] = (byte)'M';  // 'M'
        p[28] = (byte)'T';  // 'T'

        // ── 动态日期部分 ──────────────────────────────────────────────────────────

        // 星期缩写 (ddd) — 索引 0-2
        ReadOnlySpan<byte> dayAbbr = DayNamesBytes((int)nowUtc.DayOfWeek);
        dayAbbr.CopyTo(output->Slice(0, 3));

        // 日期 (dd) — 索引 5-6
        WriteDigitsBytes(output->Slice(5, 2), nowUtc.Day, 2);

        // 月份缩写 (MMM) — 索引 8-10
        ReadOnlySpan<byte> monthAbbr = MonthNamesBytes(nowUtc.Month - 1);
        monthAbbr.CopyTo(output->Slice(8, 3));

        // 年份 (yyyy) — 索引 12-15
        WriteDigitsBytes(output->Slice(12, 4), nowUtc.Year, 4);

        // 小时 (HH) — 索引 17-18
        WriteDigitsBytes(output->Slice(17, 2), nowUtc.Hour, 2);

        // 分钟 (mm) — 索引 20-21
        WriteDigitsBytes(output->Slice(20, 2), nowUtc.Minute, 2);

        // 秒钟 (ss) — 索引 23-24
        WriteDigitsBytes(output->Slice(23, 2), nowUtc.Second, 2);
    }

    // ── 星期缩写表（ASCII 字节） ──────────────────────────────────────────────────
    private static ReadOnlySpan<byte> DayNamesBytes(int index) => index switch
    {
        0 => "Sun"u8,
        1 => "Mon"u8,
        2 => "Tue"u8,
        3 => "Wed"u8,
        4 => "Thu"u8,
        5 => "Fri"u8,
        _ => "Sat"u8,
    };

    // ── 月份缩写表（ASCII 字节，index = Month - 1） ───────────────────────────────
    private static ReadOnlySpan<byte> MonthNamesBytes(int index) => index switch
    {
        0 => "Jan"u8,
        1 => "Feb"u8,
        2 => "Mar"u8,
        3 => "Apr"u8,
        4 => "May"u8,
        5 => "Jun"u8,
        6 => "Jul"u8,
        7 => "Aug"u8,
        8 => "Sep"u8,
        9 => "Oct"u8,
        10 => "Nov"u8,
        _ => "Dec"u8,
    };

    // ── 将整数以十进制 ASCII 字节写入目标 Span ────────────────────────────────────
    // 与原 WriteDigits 逻辑完全一致，仅目标类型由 char 改为 byte。
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteDigitsBytes(UnManagedCollection<byte> target, int value, int width)
    {
        for (int i = width - 1; i >= 0; i--)
        {
            *target[i] = (byte)('0' + value % 10);
            value /= 10;
        }
    }
}

