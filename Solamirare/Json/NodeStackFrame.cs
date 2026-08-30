namespace Solamirare;

/// <summary>
/// 表示 JSON 解析过程中用于追踪父子关系的栈帧。
/// </summary>
public unsafe struct NodeStackFrame
{
    /// <summary>当前父节点。</summary>
    public JsonNode* ParentNode;

    /// <summary>最近处理过的子节点。</summary>
    public JsonNode* PreviousChild;
}
