using System.Runtime.InteropServices;

namespace Solamirare;

/// <summary>
/// Linux x86-64 协程上下文，封装 ucontext_t 结构体。
/// <para>
/// 使用固定大小的 Raw 数组覆盖整个 ucontext_t，
/// 通过偏移量访问 uc_stack 和 uc_link 字段。
/// Linux x86-64 上 ucontext_t 约 936 字节，多预留空间确保安全。
/// </para>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct LinuxCoroutineContext
{
    /// <summary>
    /// ucontext_t 的原始字节，多预留空间确保覆盖完整结构体。
    /// </summary>
    public fixed byte Raw[1024];

    // ── 偏移常量（Linux x86-64）────────────────────────────────────────────────
    // uc_link   @ offset 8
    // uc_stack.ss_sp   @ offset 24
    // uc_stack.ss_size @ offset 32
    private const int OffsetLink   = 8;  // uc_link 在 ucontext_t 中的偏移
    private const int OffsetSsSp   = 16; // uc_stack.ss_sp 在 ucontext_t 中的偏移 (x86-64)
    private const int OffsetSsSize = 32; // uc_stack.ss_size 在 ucontext_t 中的偏移 (x86-64)

    /// <summary>
    /// 栈底指针（uc_stack.ss_sp）。
    /// makecontext 要求此处指向分配的栈内存低地址端。
    /// </summary>
    public void* StackPointer
    {
        get { fixed (byte* p = Raw) return *(void**)(p + OffsetSsSp); }
        set { fixed (byte* p = Raw) *(void**)(p + OffsetSsSp) = value; }
    }

    /// <summary>栈大小（uc_stack.ss_size），单位为字节。</summary>
    public nuint StackSize
    {
        get { fixed (byte* p = Raw) return *(nuint*)(p + OffsetSsSize); }
        set { fixed (byte* p = Raw) *(nuint*)(p + OffsetSsSize) = value; }
    }

    /// <summary>
    /// 后继上下文指针（uc_link）。
    /// 入口函数返回后自动切换到此上下文。
    /// 设为 null 时由我们手动处理返回。
    /// </summary>
    public LinuxCoroutineContext* Link
    {
        get { fixed (byte* p = Raw) return *(LinuxCoroutineContext**)(p + OffsetLink); }
        set { fixed (byte* p = Raw) *(LinuxCoroutineContext**)(p + OffsetLink) = value; }
    }
}