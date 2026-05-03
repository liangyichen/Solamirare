namespace Solamirare;

// --- 内部结构定义 (严格对齐内核) ---
[StructLayout(LayoutKind.Sequential)]
public struct io_uring_sqe
{
    public byte opcode; public byte flags; public ushort ioprio; public int fd;
    public ulong off; public ulong addr; public uint len; public uint rw_flags;
    public ulong user_data; public ushort buf_index; public ushort personality;
    public ushort file_index; public ulong __pad2_1; public ulong __pad2_2;
}