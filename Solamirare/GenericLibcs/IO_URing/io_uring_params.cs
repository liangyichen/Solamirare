namespace Solamirare;


[StructLayout(LayoutKind.Sequential)]
internal struct io_uring_params
{
    public uint sq_entries; public uint cq_entries; public uint flags; public uint sq_thread_cpu;
    public uint sq_thread_idle; public uint features; public uint wq_fd; public uint resv_1;
    public uint resv_2; public uint resv_3;
    public io_sqring_offsets sq_off;
    public io_cqring_offsets cq_off;
}
