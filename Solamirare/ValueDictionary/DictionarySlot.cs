namespace Solamirare;


/// <summary>
/// Dictionary 节点数据槽，存储 Key 和 Value
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
[StructLayout(LayoutKind.Auto, CharSet = CharSet.Ansi, Pack = 8)]
public struct DictionarySlot<TKey, TValue>
    where TKey : unmanaged
    where TValue : unmanaged
{
    /// <summary>
    /// Gets or sets the key stored in the slot.
    /// </summary>
    public TKey Key;

    /// <summary>
    /// Gets or sets the value stored in the slot.
    /// </summary>
    public TValue Value;

    /// <summary>
    /// Gets or sets the cached hash code for the key.
    /// </summary>
    public int HashCode;
}
