namespace Solamirare;




/// <summary>
/// 内核版本号（Major.Minor.Patch）
/// </summary>
public readonly struct LinuxKernelVersion
{
    /// <summary>主版本号。</summary>
    public readonly int Major;
    /// <summary>次版本号。</summary>
    public readonly int Minor;
    /// <summary>补丁版本号。</summary>
    public readonly int Patch;

    /// <summary>
    /// 初始化一个内核版本值。
    /// </summary>
    /// <param name="major">主版本号。</param>
    /// <param name="minor">次版本号。</param>
    /// <param name="patch">补丁版本号。</param>
    public LinuxKernelVersion(int major, int minor, int patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    /// <summary>
    /// 与 other 比较大小。返回负数/零/正数。
    /// 直接整数运算，不调用任何接口方法，无装箱风险。
    /// </summary>
    public int CompareTo(LinuxKernelVersion other)
    {
        if (Major != other.Major) return Major - other.Major;
        if (Minor != other.Minor) return Minor - other.Minor;
        return Patch - other.Patch;
    }

    /// <summary>
    /// 判断左侧版本是否大于或等于右侧版本。
    /// </summary>
    public static bool operator >=(LinuxKernelVersion a, LinuxKernelVersion b) => a.CompareTo(b) >= 0;

    /// <summary>
    /// 判断左侧版本是否小于或等于右侧版本。
    /// </summary>
    public static bool operator <=(LinuxKernelVersion a, LinuxKernelVersion b) => a.CompareTo(b) <= 0;

    /// <summary>
    /// 判断左侧版本是否大于右侧版本。
    /// </summary>
    public static bool operator >(LinuxKernelVersion a, LinuxKernelVersion b) => a.CompareTo(b) > 0;

    /// <summary>
    /// 判断左侧版本是否小于右侧版本。
    /// </summary>
    public static bool operator <(LinuxKernelVersion a, LinuxKernelVersion b) => a.CompareTo(b) < 0;


}

