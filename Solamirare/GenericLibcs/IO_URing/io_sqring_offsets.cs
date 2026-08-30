namespace Solamirare;



[StructLayout(LayoutKind.Sequential)]
internal struct io_sqring_offsets
{
    public uint head; public uint tail; public uint ring_mask; public uint ring_entries;
    public uint flags; public uint dropped; public uint array; public uint resv1; public ulong resv2;
}
