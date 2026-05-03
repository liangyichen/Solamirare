using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;


namespace Solamirare;


public unsafe partial struct JsonDocument
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAsciiWhitespace(char c)
    {
        // 仅匹配 9, 10, 13, 32
        if (c > 32) return false;
        return (0x100002600UL & (1UL << (int)c)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SkipWhitespace(UnManagedCollection<char>* json, int* index)
    {
        //对于 json 的中间空白，8 的长度已经足够
        const int UNROLL_SIZE = 8;
        char* currentPtr = json->InternalPointer + *index;
        char* endPtr = json->InternalPointer + json->Size;

        // --- 优化的空白符检查 ---
        // JSON 中有效的空白符的 ASCII 值： 9, 10, 13, 32
        // JIT 优化的检查方法：
        // 检查 c 是否是 ' ' (32)，或者 (c - 9) 是否在 [0, 4] 范围内 (即 c 在 [9, 13]) 且非 11/12。
        // 使用位掩码，可以强制 JIT 尝试使用更少的指令来判断这四个值。

        // 核心优化函数：判断 c 是否为 {9, 10, 13, 32} 之一
        // 0x100002600u 是一个魔法数字，其位 9, 10, 13, 32 处为 1。
        // (c & 0x1F) 将 c 限制在 0-31 范围内，仅适用于 c=9, 10, 13, 32。
        // 为了健壮性，使用一个在 .NET Runtime 中常见的、易于 JIT 优化的形式：

        // --- 以循环展开的算法来做每次迭代中比较 8 字节 ---
        while (currentPtr + UNROLL_SIZE <= endPtr)
        {
            // 检查第 1 个字符
            if (!IsAsciiWhitespace(*currentPtr)) goto FoundNonWhitespace;
            currentPtr++;

            // 检查第 2 个字符
            if (!IsAsciiWhitespace(*currentPtr)) goto FoundNonWhitespace;
            currentPtr++;

            // 检查第 3 个字符
            if (!IsAsciiWhitespace(*currentPtr)) goto FoundNonWhitespace;
            currentPtr++;

            // 检查第 4 个字符
            if (!IsAsciiWhitespace(*currentPtr)) goto FoundNonWhitespace;
            currentPtr++;

            // 检查第 5 个字符
            if (!IsAsciiWhitespace(*currentPtr)) goto FoundNonWhitespace;
            currentPtr++;

            // 检查第 6 个字符
            if (!IsAsciiWhitespace(*currentPtr)) goto FoundNonWhitespace;
            currentPtr++;

            // 检查第 7 个字符
            if (!IsAsciiWhitespace(*currentPtr)) goto FoundNonWhitespace;
            currentPtr++;

            // 检查第 8 个字符
            if (!IsAsciiWhitespace(*currentPtr)) goto FoundNonWhitespace;
            currentPtr++;
        }

        // --- Tail/Scalar Check (处理剩余不足 8 个的字符) ---
        while (currentPtr < endPtr)
        {
            if (!IsAsciiWhitespace(*currentPtr))
            {
                goto FoundNonWhitespace;
            }
            currentPtr++;
        }

    FoundNonWhitespace:
        // 更新 index 到第一个非空白符的位置
        *index = (int)(currentPtr - json->InternalPointer);
    }



    // 主调度方法
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ParseResult ParseStringDataCopy(UnManagedCollection<char>* json, int* index)
    {
        // 初始状态设置
        *index += 1; // 消费起始引号 '"'
        uint contentStart = (uint)*index;

        // 调度：AVX2 优先级最高 (适用于 X86/X64 芯片)
        if (Avx2.IsSupported)
        {
            return ParseStringDataCopy_Avx2(json, index, contentStart);
        }

        // 调度：ARM AdvSimd (Neon) 再次之 (适用于 M1/ARM 芯片)
        if (AdvSimd.IsSupported)
        {
            return ParseStringDataCopy_ArmNeon(json, index, contentStart);
        }

        // 调度：SSE2 优先级次之 (适用于 X86/X64 芯片)
        if (Sse2.IsSupported)
        {
            return ParseStringDataCopy_Sse2(json, index, contentStart);
        }

        // 调度：纯标量版本作为最终回退
        return ParseStringDataCopy_Scalar(json, index, contentStart);
    }
}




[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
public unsafe partial struct JsonDocument
{

    /// <summary>
    /// 根节点
    /// </summary>
    public JsonNode* Root { get; set; }

    // --- 迭代解析栈 ---\
    private NodeStackFrame* _parentStack; // 非托管内存作为栈

    private ValueLinkedList<JsonNode> _allNodes;

    private uint _nodesAllocated;
    private uint _stackCapacity;
    private uint _stackPointer; // 充当当前深度

    uint allocMemorySize;

    bool _parseFalse; // 解析失败标志

    /// <summary>
    /// 最大深度
    /// </summary>
    public uint MaxDepth { get; private set; }
    // --- END 迭代解析栈 ---

    /// <summary>
    /// 是否解析成功
    /// </summary>
    public bool ParseSuccess
    {
        get
        {
            return !_parseFalse;
        }
    }

    // 信号解析失败的私有辅助方法
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SignalParseFailure()
    {
        _parseFalse = true;
    }


    /// <summary>
    /// 
    /// </summary>
    public JsonDocument() : this(16)
    {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="maxDepth">目前仅允许 16 层深度，将来版本升级算法后才会有可能增加更高的深度支持， 如果输入大于 8，会被强制赋值为 8 ，如果输入 0， 会被强制赋值为 2</param>
    public JsonDocument(uint maxDepth = 16)
    {

        if (maxDepth > 16) maxDepth = 16;
        if (maxDepth < 2) maxDepth = 2;


        _allNodes = new ValueLinkedList<JsonNode>();

        _nodesAllocated = 0;

        _stackCapacity = maxDepth;
        _stackPointer = 0;
        MaxDepth = 0;
        _parseFalse = false; // 确保初始化

        allocMemorySize = (uint)(maxDepth * sizeof(NodeStackFrame));

        _parentStack = (NodeStackFrame*)NativeMemory.AllocZeroed(allocMemorySize);


        Root = CreateContainerNode(JsonSerializeTypes.Object);

        if (Root is null)
        {
            _parseFalse = true;
            NativeMemory.Free(_parentStack);
        }


    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="json"></param>
    /// <param name="maxDepth">目前仅允许 16 层深度，将来版本升级算法后才会有可能增加更高的深度支持， 如果输入大于 16，会被强制赋值为 16 ，如果输入 0， 会被强制赋值为 2</param>
    public JsonDocument(ReadOnlySpan<char> json, uint maxDepth = 16) : this(maxDepth)
    {

        if (_parseFalse) return;

        int index = 0;
        char* buffer;
        bool heapAlloc;
        uint bytesSize = (uint)(sizeof(char) * json.Length);

        if (json.Length > 256)
        {
            buffer = (char*)NativeMemory.AllocZeroed(bytesSize);
            heapAlloc = true;
        }
        else
        {
            // 在栈上分配，假设 json.Length < 256
            char* _buffer = stackalloc char[json.Length];
            buffer = _buffer;
            heapAlloc = false;
        }

        // --- 接收 CompactResult ---
        CompactResult result = CompactJson(json, buffer);

        UnManagedCollection<char> _jsonCompact = result.CompactedData; // 获取数据句柄

        if (!result.IsSuccess) // <--- 检查 CompactResult 的状态
        {
            _parseFalse = true;
            // 确保后续解析不会越界
            if (heapAlloc) NativeMemory.Free(buffer);
            Dispose(); // CompactJson 失败，立即清理根节点和栈
            return;
        }

        // --- End 接收 CompactResult ---

        // 确定 Root 节点类型（跳过初始空白符）
        SkipWhitespace(&_jsonCompact, &index);

        // 我们只需要确保 JSON 字符串不为空，并调用 Parse 进行处理。
        // ParseValue 会负责设置 Root 的类型。
        if (index < _jsonCompact.Size)
        {
            _parseFalse = false;

            // 移除提前预设 Root->Type 的逻辑
            Parse(Root, &_jsonCompact, &index);
        }
        else
        {
            // JSON 字符串为空或只包含空白符
            _parseFalse = true;
        }

        // 【关键修正】：检查内部解析是否失败，如果失败，立即进行清理
        if (_parseFalse)
        {
            // 清理临时 buffer
            if (heapAlloc) NativeMemory.Free(buffer);
            // 清理已分配的节点 (_allNodes) 和栈 (_parentStack)
            Dispose();
            return; // 失败退出
        }


        // 成功退出：释放临时 buffer 
        if (heapAlloc) NativeMemory.Free(buffer);
    }


    private void Parse(JsonNode* root, UnManagedCollection<char>* json, int* index)
    {
        PushStack(root, null);
        if (_parseFalse) return; // 【深度检查】如果 PushStack 失败（深度溢出），立即退出

        ParseValue(root, json, index);
        PopStack();
    }

    /// <summary>
    /// 把当前文档序列化为 json 字符串
    /// </summary>
    /// <returns></returns>
    public UnManagedString Serialize()
    {
        UnManagedString buffer = new UnManagedString();

        if (Root != null && !_parseFalse)
        {
            serialize(Root, &buffer);
        }

        return buffer;
    }

    void serialize(JsonNode* node, UnManagedString* buffer)
    {
        // 序列化逻辑 (保持不变)
        switch (node->Type)
        {
            case JsonSerializeTypes.Object:
                buffer->Add('{');
                JsonNode* objChild = node->FirstChild;
                bool isFirstObject = true;
                while (objChild != null)
                {
                    if (!isFirstObject) buffer->Add(',');

                    buffer->Add('"');
                    buffer->AddRange(&objChild->Key.Prototype);
                    buffer->Add('"');

                    buffer->Add(':');
                    serialize(objChild, buffer);

                    isFirstObject = false;
                    objChild = objChild->NextSibling;
                }
                buffer->Add('}');
                break;
            case JsonSerializeTypes.Array:
                buffer->Add('[');
                JsonNode* arrChild = node->FirstChild;
                bool isFirstArray = true;
                while (arrChild != null)
                {
                    if (!isFirstArray) buffer->Add(',');
                    serialize(arrChild, buffer);
                    isFirstArray = false;
                    arrChild = arrChild->NextSibling;
                }
                buffer->Add(']');
                break;
            case JsonSerializeTypes.String:
                buffer->Add('"');
                buffer->AddRange((UnManagedCollection<char>*)&node->Value);
                buffer->Add('"');
                break;
            case JsonSerializeTypes.Number:
            case JsonSerializeTypes.Boolean:
            case JsonSerializeTypes.Null:
                buffer->AddRange((UnManagedCollection<char>*)&node->Value);
                break;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JsonNode* AllocateNode()
    {
        // 1. 在集合中追加一个空节点
        JsonNode dummy = default; // 局部变量清零
        _allNodes.Append(&dummy);

        // 2. 获取该节点的非托管指针
        JsonNode* p = _allNodes.Index((int)_nodesAllocated);
        _nodesAllocated += 1;

        // 3. 【防御性清零】
        // 将整个 JsonNode 结构的内存块直接抹除为 0
        // 这保证了 Key.Pointer 和 Value.Pointer 均为 null，字典指针也为 null
        Unsafe.InitBlock(p, 0, (uint)sizeof(JsonNode));

        // 4. 设置默认的安全状态
        p->Type = JsonSerializeTypes.Null; // 默认为 Null 类型，防止未定义行为

        return p;
    }

    // --- 非托管栈操作 ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PushStack(JsonNode* parent, JsonNode* previousChild)
    {
        // 当栈溢出时，不再抛出异常，而是标记失败并退出
        if (_stackPointer >= _stackCapacity)
        {
            SignalParseFailure();
            return; // 立即返回，不执行任何栈操作
        }

        _parentStack[_stackPointer].ParentNode = parent;
        _parentStack[_stackPointer].PreviousChild = previousChild;

        _stackPointer++;
        if (_stackPointer > MaxDepth) MaxDepth = _stackPointer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PopStack()
    {
        if (_stackPointer > 0)
        {
            _stackPointer--;
        }
    }

    private NodeStackFrame* PeekStack()
    {
        if (_stackPointer <= 0)
            return null;
        return &_parentStack[_stackPointer - 1];
    }
    // --- END 非托管栈操作 ---

    #region ImportAndConstructionHelpers

    /// <summary>
    /// 创建一个值节点
    /// </summary>
    public JsonNode* CreateValueNode(UnManagedString valueSlice, JsonSerializeTypes type)
    {
        JsonNode* node = AllocateNode();

        node->Type = type;

        if (type == JsonSerializeTypes.Object || type == JsonSerializeTypes.Array || valueSlice.IsEmpty)
        {
            node->Value = UnManagedString.Empty;
        }
        else
        {
            node->Value = valueSlice;
        }
        return node;
    }

    public JsonNode* CreateValueNode(JsonSerializeTypes type)
    {
        return CreateValueNode(UnManagedString.Empty, type);
    }


    /// <summary>
    /// 创建一个容器节点 (Object/Array)
    /// </summary>
    public JsonNode* CreateContainerNode(JsonSerializeTypes type)
    {
        if (type != JsonSerializeTypes.Object && type != JsonSerializeTypes.Array)
        {
            //throw new ArgumentException("Type must be Object or Array for a container node.");
            return null;
        }
        return CreateValueNode(type);
    }

    public JsonNode* AddChild(ReadOnlySpan<char> key, ReadOnlySpan<char> value, JsonSerializeTypes type)
    {
        return AddChild(Root, key, value, type);
    }


    /// <summary>
    /// 添加子节点到容器
    /// </summary>
    public JsonNode* AddChild(JsonNode* parent, ReadOnlySpan<char> key, ReadOnlySpan<char> value, JsonSerializeTypes type)
    {

        if (parent->Type != JsonSerializeTypes.Object && parent->Type != JsonSerializeTypes.Array)
        {
            return null;
        }

        // 创建值和键的内存副本
        UnManagedString valueCopy = value.CopyToUnManagedMemory();

        JsonNode* child = CreateValueNode(valueCopy, type);

        // 1. 设置 Key
        child->Key = key.CopyToUnManagedMemory();

        // 2. 查找最后一个兄弟节点
        JsonNode* current = parent->FirstChild;
        if (current == null)
        {
            parent->FirstChild = child;
        }
        else
        {
            while (current->NextSibling != null)
            {
                current = current->NextSibling;
            }
            current->NextSibling = child;
        }
        return child;
    }

    #endregion

    #region WhitespaceAndParseLogic


    private void ParseValue(JsonNode* node, UnManagedCollection<char>* json, int* index)
    {
        SkipWhitespace(json, index);

        // **【安全检查 1】** 检查是否到达 EOF 
        if (*index >= json->Size)
        {
            SignalParseFailure(); // 令牌缺失，解析失败
            return;
        }

        char firstChar = *(json->InternalPointer + *index);

        switch (firstChar)
        {
            case '{':
                node->Type = JsonSerializeTypes.Object;
                ParseObject(node, json, index);
                if (_parseFalse) return; // 容器解析失败，传播失败状态
                break;

            case '[':
                node->Type = JsonSerializeTypes.Array;
                ParseArray(node, json, index);
                if (_parseFalse) return; // 容器解析失败，传播失败状态
                break;

            case '"':
                node->Type = JsonSerializeTypes.String;

                ParseResult result = ParseStringDataCopy(json, index); // <--- 使用新结构体

                if (result.IsFailure)
                {
                    SignalParseFailure();
                }
                // 允许空字符串值
                else if (result.IsEmptyString)
                {
                    // 成功解析为 ""，node->Value 为 UnManagedCollection<char>.Empty (result.Data 已经是 Empty)
                    node->Value = UnManagedString.Empty;
                }
                else // 非空字符串成功
                {
                    // result.Data 包含了字符串内容副本
                    node->Value = result.Data;
                }

                break;

            case 't': // true
            case 'f': // false
                node->Type = JsonSerializeTypes.Boolean;
                ParseBoolean(node, json, index); // 现在是 ParseBoolean
                if (node->Value.IsEmpty) // 检查 ParseBoolean 的结果
                {
                    SignalParseFailure();
                }
                break;

            case 'n': // null
                node->Type = JsonSerializeTypes.Null;
                ParseNull(node, json, index);
                if (node->Value.IsEmpty) // 检查 ParseNull 的结果
                {
                    SignalParseFailure();
                }
                break;

            case '-': // 负数
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9': // 数字
                node->Type = JsonSerializeTypes.Number;
                ParseNumber(node, json, index);
                if (node->Value.IsEmpty) // 检查 ParseNumber 的结果
                {
                    SignalParseFailure();
                }
                break;

            default:
                // **【安全检查 2】** 遇到未知或无效的起始字符
                SignalParseFailure();
                break;
        }
    }

    private unsafe void ParseObject(JsonNode* parentNode, UnManagedCollection<char>* json, int* index)
    {
        // 1. 进入栈深度检查
        PushStack(parentNode, null);
        if (_parseFalse) return;

        *index += 1; // 消耗起始符 '{'

        NodeStackFrame* frame = PeekStack();

        // 2. 延迟初始化字典：确保字典仅在需要时分配，且避免重复初始化
        const uint INITIAL_CAPACITY = 8;
        parentNode->KeyLookupDict = new ValueDictionary<UnManagedString, nuint>(INITIAL_CAPACITY);


        while (*index < json->Size)
        {
            SkipWhitespace(json, index);

            // 检查是否意外到达 EOF
            if (*index >= json->Size) goto Fail;

            char* readPtr = json->InternalPointer + *index;

            // 3. 检查对象闭合
            if (*readPtr == '}')
            {
                *index += 1;
                PopStack();
                return;
            }

            // 4. 键 (Key) 必须以引号开头
            if (*readPtr != '"') goto Fail;

            // 5. 解析 Key 字符串
            ParseResult keyResult = ParseStringDataCopy(json, index);
            if (keyResult.IsFailure || keyResult.IsEmptyString)
            {
                // 如果解析失败，确保清理已分配的片段内存
                if (!keyResult.Data.IsEmpty) keyResult.Data.Dispose();
                goto Fail;
            }

            // --- 内存安全修复：重复键检查 ---
            // 必须在赋值给 childNode 之前检查，否则旧 Key 内存会因覆盖而泄漏
            if (parentNode->KeyLookupDict.ContainsKey(keyResult.Data))
            {
                keyResult.Data.Dispose();
                goto Fail;
            }

            // 6. 分配并防御性初始化子节点
            // AllocateNode 内部已通过 Unsafe.InitBlock 清零，确保指针均为 null
            JsonNode* childNode = AllocateNode();
            childNode->Key = keyResult.Data;

            // 7. 链接子节点到双向链表 (用于保持 JSON 原始序列化顺序)
            if (frame->PreviousChild == null) { frame->ParentNode->FirstChild = childNode; }
            else { frame->PreviousChild->NextSibling = childNode; }
            frame->PreviousChild = childNode;

            // --- 核心修复：直接将节点地址数值存入字典 ---
            // 1. 确保 Key 在字典中占位（如果不存在则添加）
            // 这里假设你的字典在 Add 或索引器 Setter 时会处理内存分配
            nuint dummy = 0;
            if (!parentNode->KeyLookupDict.AddOrUpdate(childNode->Key, dummy))
            {
                // 字典添加失败（可能是扩容失败），视为解析错误以保证数据一致性
                goto Fail;
            }

            // 2. 获取该 Key 在字典内部对应的堆内存槽位指针
            // 这里的索引器必须返回 TValue* (即 nuint*)
            nuint* pSlot = parentNode->KeyLookupDict[childNode->Key];

            if (pSlot != null)
            {
                // 直接操作槽位所在的堆内存，存入 childNode 的地址数值
                *pSlot = (nuint)childNode;
            }

            // 8. 解析冒号 ':'
            SkipWhitespace(json, index);
            if (*index >= json->Size || *(json->InternalPointer + *index) != ':')
            {
                goto Fail;
            }
            *index += 1; // 消耗 ':'

            // 9. 递归解析值 (Value)
            ParseValue(childNode, json, index);
            if (_parseFalse) goto Fail; // 递归失败向上冒泡

            // 10. 检查后续的分隔符 ',' 或结束符 '}'
            SkipWhitespace(json, index);
            if (*index >= json->Size) goto Fail;

            char nextChar = *(json->InternalPointer + *index);
            if (nextChar == ',')
            {
                *index += 1;
                continue; // 继续解析下一个成员
            }
            if (nextChar == '}')
            {
                *index += 1;
                PopStack();
                return;
            }

            goto Fail; // 既非 ',' 也非 '}'，结构错误
        }

    Fail:
        SignalParseFailure();
        PopStack();
    }

    
    /// <summary>
    /// 描述当前对象状态的哈希码。
    /// </summary>
    public ulong StatusCode
    {
        get
        {
            fixed(JsonDocument* p = &this)
            {
                ulong result = Fingerprint.MemoryFingerprint(p);

                return result;
            }
        }
    }

    private void ParseArray(JsonNode* parentNode, UnManagedCollection<char>* json, int* index)
    {
        PushStack(parentNode, null);
        if (_parseFalse) goto FailAndNoPop; // 【深度检查】PushStack 失败，跳转到 FailAndNoPop

        *index += 1; // Consume '['

        NodeStackFrame* frame = PeekStack();

        while (*index < json->Size)
        {
            // 检查全局失败标志，如果已经失败，立即退出
            if (_parseFalse) goto Fail;

            SkipWhitespace(json, index); // 1. 跳过元素前的空白

            // **【安全检查 0.1】** 检查是否到达 EOF 
            if (*index >= json->Size) goto Fail;

            char* readPtr = json->InternalPointer + *index;

            // 1. Check for closing ']'
            if (*readPtr == ']')
            {
                *index += 1;
                PopStack();
                return;
            }

            // 2. Create and link child node
            JsonNode* childNode = AllocateNode();

            if (frame->PreviousChild == null) { frame->ParentNode->FirstChild = childNode; }
            else { frame->PreviousChild->NextSibling = childNode; }
            frame->PreviousChild = childNode;

            // 3. Parse Value (recursively populates the childNode)
            ParseValue(childNode, json, index);

            // 检查递归调用是否失败
            if (_parseFalse) goto Fail;

            // 4. Consume ',' or ']' (Lookahead Optimization)
            SkipWhitespace(json, index);

            // **【安全检查 1.1】** 检查是否到达 EOF 
            if (*index >= json->Size) goto Fail;

            char nextChar = *(json->InternalPointer + *index);

            if (nextChar == ',')
            {
                *index += 1;
                continue;
            }

            if (nextChar == ']') // 检查数组闭合
            {
                *index += 1; // 消耗 ']'
                PopStack();
                return;
            }

            // 既不是 ',' 也不是 ']'，结构错误
            goto Fail;
        }

    Fail:
        SignalParseFailure(); // 设置全局失败标志
        PopStack();
        return;

    FailAndNoPop: // 【新增标签】专门处理 PushStack 失败的情况
        SignalParseFailure(); // 确保标志被设置
        return; // 直接返回，因为 PushStack 失败时没有 Pop 的必要
    }




    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ParseResult ParseStringDataCopy_Scalar(UnManagedCollection<char>* json, int* index, uint contentStart)
    {
        // 从传入的 index 状态继续解析
        char* jsonPtr = json->InternalPointer;
        char* currentPtr = jsonPtr + *index;
        char* endPtr = jsonPtr + json->Size;

        uint slashCount = 0;
        bool foundEnd = false;

        const int UNROLL_SIZE = 8;

        // --- 主循环：标量展开 / 精确检查 ---
        while (*index < json->Size)
        {
            bool keyCharFound = false;

            // 1. 【循环展开阶段】：这是主要的标量优化
            if (slashCount == 0 && (currentPtr + UNROLL_SIZE) <= endPtr)
            {
                for (int i = 0; i < UNROLL_SIZE; i++)
                {
                    if (*currentPtr == '"' || *currentPtr == '\\')
                    {
                        keyCharFound = true;
                        break;
                    }
                    currentPtr++;
                }

                if (!keyCharFound)
                {
                    *index = (int)(currentPtr - jsonPtr);
                    continue;
                }
            }

            // 2. 【精确标量检查阶段】：处理转义序列、闭合引号、剩余字符
            *index = (int)(currentPtr - jsonPtr);

            do
            {
                if (*index >= json->Size) goto Fail;

                char currentChar = *(jsonPtr + *index);

                if (currentChar == '"' && (slashCount % 2 == 0))
                {
                    foundEnd = true;
                    break;
                }

                if (currentChar == '\\')
                {
                    slashCount++;
                }
                else
                {
                    slashCount = 0;
                }

                *index += 1;
                currentPtr = jsonPtr + *index;

            } while (*index < json->Size && slashCount != 0);

            if (foundEnd) break;
            if (*index >= json->Size) break;
        }

        if (!foundEnd) goto Fail;

        // --- 长度计算和零 GC 拷贝逻辑 ---
        return FinalizeParseStringDataCopy(json, index, contentStart, currentPtr);

    Fail:
        return ParseResult.Failure;
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ParseResult FinalizeParseStringDataCopy(UnManagedCollection<char>* json, int* index, uint contentStart, char* currentPtr)
    {
        // 长度计算
        *index = (int)(currentPtr - json->InternalPointer);
        uint length = (uint)(*index - contentStart);
        *index += 1; // 消费结束引号 '"'

        if (length == 0)
        {
            return ParseResult.EmptyStringSuccess;
        }


        char* jsonPtr = json->InternalPointer;
        ReadOnlySpan<char> sourceSpan = new ReadOnlySpan<char>(jsonPtr + contentStart, (int)length);
        UnManagedString result = sourceSpan.CopyToUnManagedMemory();


        return ParseResult.Success(result);
    }


    /// <summary>
    /// 【核心优化】零 GC 数字解析：查找数字边界并进行原始字符串的拷贝。
    /// </summary>
    private void ParseNumber(JsonNode* node, UnManagedCollection<char>* json, int* index)
    {
        uint start = (uint)*index;
        char* jsonPtr = json->InternalPointer;
        char* readPtr = jsonPtr + start;
        nuint jsonLength = json->Size;

        // 1. 初始检查：处理负号
        if (*readPtr == '-')
        {
            readPtr++;
            *index += 1;
        }

        // --- 查找整数部分 ---

        // 强制检查：必须至少有一个数字
        if (*index >= (int)jsonLength || (*readPtr < '0' || *readPtr > '9'))
        {
            // 只有负号或非数字开头，解析失败
            SignalParseFailure();
            node->Value = UnManagedString.Empty;
            return;
        }

        // 查找连续数字直到遇到非数字字符
        while (*index < (int)jsonLength && *readPtr >= '0' && *readPtr <= '9')
        {
            readPtr++;
            *index += 1;
        }

        // --- 查找小数部分 (可选) ---
        if (*index < (int)jsonLength && *readPtr == '.')
        {
            readPtr++;
            *index += 1; // 消耗 '.'

            // 强制检查：小数点后必须有数字
            if (*index >= (int)jsonLength || (*readPtr < '0' || *readPtr > '9'))
            {
                // 小数点后没有数字，解析失败
                SignalParseFailure();
                node->Value = UnManagedString.Empty;
                return;
            }

            // 查找连续数字
            while (*index < (int)jsonLength && *readPtr >= '0' && *readPtr <= '9')
            {
                readPtr++;
                *index += 1;
            }
        }

        // --- 查找科学记数法部分 (可选) ---
        if (*index < (int)jsonLength && (*readPtr == 'e' || *readPtr == 'E'))
        {
            readPtr++;
            *index += 1; // 消耗 'e' 或 'E'

            // 处理正负号 (+ 或 -)
            if (*index < (int)jsonLength && (*readPtr == '+' || *readPtr == '-'))
            {
                readPtr++;
                *index += 1;
            }

            // 强制检查：指数符号后必须有数字
            if (*index >= (int)jsonLength || (*readPtr < '0' || *readPtr > '9'))
            {
                // 指数部分格式错误，解析失败
                SignalParseFailure();
                node->Value = UnManagedString.Empty;
                return;
            }

            // 查找连续数字
            while (*index < (int)jsonLength && *readPtr >= '0' && *readPtr <= '9')
            {
                readPtr++;
                *index += 1;
            }
        }

        // --- 最终拷贝：将整个数字字符串复制到非托管堆 ---

        uint finalEnd = (uint)*index;
        uint length = finalEnd - start;

        // 零 GC 拷贝到非托管堆
        ReadOnlySpan<char> sourceSpan = new ReadOnlySpan<char>(jsonPtr + start, (int)length);
        node->Value = sourceSpan.CopyToUnManagedMemory();

        // 假设您在 ParseValue 中处理了 node->Type = JsonType.Number
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ParseBoolean(JsonNode* node, UnManagedCollection<char>* json, int* index)
    {
        uint start = (uint)*index;
        UnManagedString result = UnManagedString.Empty;

        char* startPtr = json->InternalPointer + start;
        char* endPtr = json->InternalPointer + json->Size;
        nuint remainingLength = (nuint)(endPtr - startPtr);

        // -----------------------------------------------------------------
        // 检查 true (4 字符)
        // -----------------------------------------------------------------
        if (remainingLength >= 4 && *startPtr == 't')
        {
            if (startPtr[1] == 'r' && startPtr[2] == 'u' && startPtr[3] == 'e')
            {

                ReadOnlySpan<char> sourceSpan = new ReadOnlySpan<char>(startPtr, 4);
                result = sourceSpan.CopyToUnManagedMemory();
                node->Type = JsonSerializeTypes.Boolean;
                *index += 4;
            }
        }
        // -----------------------------------------------------------------
        // 检查 false (5 字符)
        // -----------------------------------------------------------------
        else if (remainingLength >= 5 && *startPtr == 'f')
        {
            if (startPtr[1] == 'a' && startPtr[2] == 'l' && startPtr[3] == 's' && startPtr[4] == 'e')
            {

                ReadOnlySpan<char> sourceSpan = new ReadOnlySpan<char>(startPtr, 5);
                result = sourceSpan.CopyToUnManagedMemory();
                node->Type = JsonSerializeTypes.Boolean;
                *index += 5;

            }
        }

        node->Value = result;
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ParseNull(JsonNode* node, UnManagedCollection<char>* json, int* index)
    {
        uint start = (uint)*index;
        UnManagedString result = UnManagedString.Empty;

        char* startPtr = json->InternalPointer + start;
        char* endPtr = json->InternalPointer + json->Size;
        nuint remainingLength = (nuint)(endPtr - startPtr);

        // 检查 'null' (4 字符)
        if (remainingLength >= 4)
        {

            if (*startPtr == 'n' && startPtr[1] == 'u' && startPtr[2] == 'l' && startPtr[3] == 'l')
            {

                ReadOnlySpan<char> sourceSpan = new ReadOnlySpan<char>(startPtr, 4);
                result = sourceSpan.CopyToUnManagedMemory();
                *index += 4;

            }
        }

        node->Value = result;
    }

    #endregion


    static bool NodesDispose(int index, JsonNode* node, void* caller)
    {
        // 1. 清理 Key 和 Value 的非托管内存
        node->Key.Dispose();
        node->Value.Dispose();

        // 2. 清理 KeyLookupDict 内部的非托管内存 (仅限 Object 节点)
        if (node->Type == JsonSerializeTypes.Object)
        {
            node->KeyLookupDict.Dispose(); // 假设 ValueDictionary 实现了 Dispose
        }

        return true;
    }


    public void Dispose()
    {
        // 确保 _allNodes 内存被清理
        if (!_allNodes.IsEmpty)
        {
            _allNodes.ForEach(&NodesDispose, null);
        }
        _allNodes.Dispose();

        if (_parentStack != null)
        {
            NativeMemory.Free(_parentStack);
            _parentStack = null; // 【关键修正】：清理后设置为 null，防止二次释放
        }

        _stackPointer = 0; // 重置栈指针
        _stackCapacity = 0; // 重置容量
    }
}