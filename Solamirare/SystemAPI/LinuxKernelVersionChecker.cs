namespace Solamirare;


/// <summary>
/// Linux 内核版本解析器
/// </summary>
public static unsafe class LinuxKernelVersionChecker
{
    private const int UTSNAME_FIELD_LEN = 65;

    private const int UTSNAME_RELEASE_OFFSET = UTSNAME_FIELD_LEN * 2; // release 是第3个字段（索引2）


    // ──────────────────────────────────────────────────────────────────────────
    // uname / utsname
    // ──────────────────────────────────────────────────────────────────────────

    // struct utsname 字段布局（Linux x86-64, glibc）：
    //   每个字段 65 字节，共 6 个字段，总 390 字节。
    //   [0] sysname   offset   0  → "Linux"
    //   [1] nodename  offset  65  → hostname
    //   [2] release   offset 130  → "6.15.3-generic"
    //   [3] version   offset 195  → "#1 SMP ..."
    //   [4] machine   offset 260  → "x86_64"
    //   [5] domainname offset 325


    /// <summary>
    /// 获取当前内核版本。
    /// </summary>
    public static LinuxKernelVersion GetLinuxKernelVersion()
    {
        // utsname 结构最大 390 字节，栈上分配安全
        byte* buf = stackalloc byte[390];

        if (LinuxAPI.uname(buf) != 0)
            return new LinuxKernelVersion(0, 0, 0); // uname 失败，回退到最保守版本

        // release 字段在偏移 UTSNAME_FIELD_LEN*2（=130）处，格式为 "6.15.3-ubuntu-generic"
        byte* release = buf + UTSNAME_RELEASE_OFFSET;

        return ParseVersion(release);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 版本字符串解析（零 GC，纯指针操作）
    // 格式示例："6.15.3-ubuntu"  "5.4.0-182-generic"  "6.15.0"
    // ──────────────────────────────────────────────────────────────────────────

    static LinuxKernelVersion ParseVersion(byte* release)
    {
        int major = 0, minor = 0, patch = 0;

        // 解析 major
        byte* p = release;
        p = ParseInt(p, out major);
        if (*p != (byte)'.') return new LinuxKernelVersion(major, 0, 0);
        p++; // skip '.'

        // 解析 minor
        p = ParseInt(p, out minor);
        if (*p != (byte)'.') return new LinuxKernelVersion(major, minor, 0);
        p++; // skip '.'

        // 解析 patch（遇到非数字字符停止，如 '-'）
        ParseInt(p, out patch);

        return new LinuxKernelVersion(major, minor, patch);
    }

    /// <summary>
    /// 从 ptr 起解析连续的 ASCII 十进制数字，写入 result，返回停止位置的指针。
    /// </summary>
    static byte* ParseInt(byte* ptr, out int result)
    {
        result = 0;
        while (*ptr >= (byte)'0' && *ptr <= (byte)'9')
        {
            result = result * 10 + (*ptr - (byte)'0');
            ptr++;
        }
        return ptr;
    }
}
