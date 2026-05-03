namespace Solamirare;

/// <summary>
/// 协程的寄存器快照，保存上下文切换时的 CPU 状态。
/// <para>
/// 布局由汇编文件 coroutine_switch_arm64.S 严格对应，字段顺序和偏移量不可随意修改。
/// 跨平台扩展时，不同平台的 CoroutineContext 可能布局不同，
/// 但对上层代码透明，上层只持有指针，不直接访问内部字段。
/// </para>
/// </summary>
public unsafe struct MacOSCoroutineContext
{
    // ARM64 callee-saved 寄存器，顺序与汇编偏移量严格对应
    // offset  0
    /// <summary>Saved ARM64 callee-saved register X19.</summary>
    public ulong X19;
    // offset  8
    /// <summary>Saved ARM64 callee-saved register X20.</summary>
    public ulong X20;
    // offset 16
    /// <summary>Saved ARM64 callee-saved register X21.</summary>
    public ulong X21;
    // offset 24
    /// <summary>Saved ARM64 callee-saved register X22.</summary>
    public ulong X22;
    // offset 32
    /// <summary>Saved ARM64 callee-saved register X23.</summary>
    public ulong X23;
    // offset 40
    /// <summary>Saved ARM64 callee-saved register X24.</summary>
    public ulong X24;
    // offset 48
    /// <summary>Saved ARM64 callee-saved register X25.</summary>
    public ulong X25;
    // offset 56
    /// <summary>Saved ARM64 callee-saved register X26.</summary>
    public ulong X26;
    // offset 64
    /// <summary>Saved ARM64 callee-saved register X27.</summary>
    public ulong X27;
    // offset 72
    /// <summary>Saved ARM64 callee-saved register X28.</summary>
    public ulong X28;
    // offset 80：帧指针
    /// <summary>Saved frame pointer register.</summary>
    public ulong Fp;
    // offset 88：链接寄存器，存返回地址或入口函数地址
    /// <summary>Saved link register.</summary>
    public ulong Lr;
    // offset 96：栈指针
    /// <summary>Saved stack pointer.</summary>
    public ulong Sp;
}
