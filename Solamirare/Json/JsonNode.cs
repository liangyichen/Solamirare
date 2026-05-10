namespace Solamirare;

/// <summary>
/// 表示 JSON 文档中的一个节点。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct JsonNode
{
    /// <summary>节点的值类型。</summary>
    public JsonSerializeTypes Type;

    /// <summary>节点键名。</summary>
    public UnManagedString Key;

    /// <summary>节点值。</summary>
    public UnManagedString Value;

    /// <summary>首个子节点。</summary>
    public JsonNode* FirstChild;

    /// <summary>下一个同级节点。</summary>
    public JsonNode* NextSibling;

    /// <summary>用于加速键查找的字典。</summary>
    public ValueDictionary<UnManagedString, nuint> KeyLookupDict;
}
