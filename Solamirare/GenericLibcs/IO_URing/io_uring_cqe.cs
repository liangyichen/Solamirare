namespace Solamirare;


[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct io_uring_cqe
{
    public ulong user_data;
    public int res;
    public uint flags;
}