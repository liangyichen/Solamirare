namespace Solamirare;


[StructLayout(LayoutKind.Sequential)]
internal struct io_cqring_offsets
{
    public uint head, tail, ring_mask, ring_entries, overflow, cqes, flags, resv1;
    public ulong resv2;
}
