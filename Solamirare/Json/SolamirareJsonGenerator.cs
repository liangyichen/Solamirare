using System.Numerics;
using System.Runtime.CompilerServices;
namespace Solamirare;

public unsafe struct JsonParsingArenaBlock
{
    public JsonParsingArenaBlock* Next;
    public byte* Data;
    public uint Capacity;
    public uint Used;
}

/// <summary>
/// JSON 解析专用内存区域分配器 (Arena Allocator)
/// <para>用于批量管理解析过程中产生的短字符串内存，减少系统调用并提高缓存局部性。</para>
/// </summary>
public unsafe struct JsonParsingArena
{


    private JsonParsingArenaBlock* _head;
    private JsonParsingArenaBlock* _current;
    private const uint DefaultBlockSize = 4096 * 4; // 16KB per block

    public char* Alloc(uint charCount)
    {
        uint byteCount = charCount * sizeof(char);

        // 1. 尝试在当前块分配
        if (_current != null && _current->Used + byteCount <= _current->Capacity)
        {
            char* ptr = (char*)(_current->Data + _current->Used);
            _current->Used += byteCount;
            return ptr;
        }

        // 2. 分配新块
        uint newCapacity = byteCount > DefaultBlockSize ? byteCount : DefaultBlockSize;

        // 分配 Block 结构体和数据内存
        // 为了简化管理，Block 头和数据分开分配，或者分配在一起。这里分开分配以保持对齐简单。
        JsonParsingArenaBlock* newBlock = (JsonParsingArenaBlock*)NativeMemory.AllocZeroed((nuint)sizeof(JsonParsingArenaBlock));
        newBlock->Data = (byte*)NativeMemory.AllocZeroed(newCapacity);
        newBlock->Capacity = newCapacity;
        newBlock->Used = byteCount;
        newBlock->Next = null;

        if (_head == null) _head = newBlock;
        else _current->Next = newBlock;

        _current = newBlock;

        return (char*)newBlock->Data;
    }

    public void Dispose()
    {
        JsonParsingArenaBlock* node = _head;
        while (node != null)
        {
            JsonParsingArenaBlock* next = node->Next;
            NativeMemory.Free(node->Data);
            NativeMemory.Free(node);
            node = next;
        }
        _head = null;
        _current = null;
    }
}

/// <summary>
/// 简单 json 处理器， json 字符串类型仅限平面类型，不能有深度嵌套
/// <para>合法例子： {"name":"my name", "age":100} </para>
/// <para>非法例子： {"name":"my name", "age":100, “pet”:{"name":"wowo"}} </para>
/// </summary>
public static unsafe class SolamirareJsonGenerator
{

    const int stack_limit = 1024; // 栈内存限制于 1024 bytes

    /// <summary>
    /// 空对象
    /// </summary>
    static UnManagedString EMPTY_OBJECT_READ_ONLY;

    /// <summary>
    /// 空集合
    /// </summary>
    static UnManagedString EMPTY_COLLECTION_READ_ONLY;

    static UnManagedMemory<UnManagedString> EMPTY_ARRAY_READ_ONLY;



    static Vector<ushort> newlineVector;
    static Vector<ushort> carriageReturnVector;
    static Vector<ushort> tabVector;
    static Vector<ushort> backslashVector;
    static Vector<ushort> quoteVector;
    static Vector<ushort> commaVector; // 新增：用于逗号计数


    static readonly Type TypeByte = typeof(byte);
    static readonly Type TypeSByte = typeof(sbyte);
    static readonly Type TypeShort = typeof(short);
    static readonly Type TypeUShort = typeof(ushort);
    static readonly Type TypeInt = typeof(int);
    static readonly Type TypeUInt = typeof(uint);
    static readonly Type TypeLong = typeof(long);
    static readonly Type TypeULong = typeof(ulong);
    static readonly Type TypeFloat = typeof(float);
    static readonly Type TypeDouble = typeof(double);
    static readonly Type TypeDecimal = typeof(decimal);

    static readonly Type TypeDateTime = typeof(DateTime);

    static readonly Type TypeBoolean = typeof(bool);

    static readonly Type TypeEnum = typeof(Enum);

    static SolamirareJsonGenerator()
    {
        EMPTY_OBJECT_READ_ONLY = new UnManagedString("{}");

        EMPTY_COLLECTION_READ_ONLY = new UnManagedString("[]");

        EMPTY_ARRAY_READ_ONLY = new UnManagedMemory<UnManagedString>(0);

        //===========

        newlineVector = new Vector<ushort>('\n');
        carriageReturnVector = new Vector<ushort>('\r');
        tabVector = new Vector<ushort>('\t');
        backslashVector = new Vector<ushort>('\\');
        quoteVector = new Vector<ushort>('\"');
        commaVector = new Vector<ushort>(',');

    }


    /// <summary>
    /// 解码 json 字符串（生成新内存）
    /// （对象模式解码只能使用这个生成独立的内存块）
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static UnManagedString DecodeJsonString(ReadOnlySpan<char> input)
    {
        UnManagedString un_input = new UnManagedString(input);

        DecodeJsonString(&un_input);

        return un_input;
    }


    /// <summary>
    /// 解码 json 字符串（不要释放输入源，它会被重组为计算结果）
    /// （提示：对象模式解码不可直接使用这个函数修改源字符串，因为两次引用会造成 key 和 value 之间的下标移动互相干扰）
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static void DecodeJsonString(UnManagedString* input)
    {
        if (input->IsEmpty) return;
        char* start = input->Pointer;
        char* read = start;
        char* end = read + input->UsageSize;


        // 大多数 JSON 字符串没有转义符，快速跳过可以避免不必要的写入操作
        while (read < end && *read != '\\') read++;

        if (read >= end) return; // No escaping needed

        char* write = read;

        while (read < end)
        {
            char c = *read;
            if (c == '\\')
            {
                read++;
                if (read >= end)
                {
                    *write++ = '\\'; // 保留尾部反斜杠
                    break;
                }

                c = *read;
                switch (c)
                {
                    case '"': *write++ = '"'; break;
                    case '\\': *write++ = '\\'; break;
                    case '/': *write++ = '/'; break;
                    case 'b': *write++ = '\b'; break;
                    case 'f': *write++ = '\f'; break;
                    case 'n': *write++ = '\n'; break;
                    case 'r': *write++ = '\r'; break;
                    case 't': *write++ = '\t'; break;
                    case '\'': *write++ = '\''; break; // 补充：支持单引号转义，保持原逻辑兼容性
                    case 'u':

                        // 处理 Unicode 转义 \uXXXX
                        // read 此时指向 'u'，read[1..4] 是四个 hex 位。
                        // 需要 read+4 < end（即 read[4] 在范围内），因此边界条件是 read+4 <= end
                        
                        if (read + 4 <= end)
                        {
                            int val = 0;
                            bool success = true;
                            for (int i = 1; i <= 4; i++)
                            {
                                char h = read[i];
                                int digit = 0;
                                if (h >= '0' && h <= '9') digit = h - '0';
                                else if (h >= 'a' && h <= 'f') digit = h - 'a' + 10;
                                else if (h >= 'A' && h <= 'F') digit = h - 'A' + 10;
                                else { success = false; break; }
                                val = (val << 4) | digit;
                            }

                            if (success) { *write++ = (char)val; read += 4; }
                            else { *write++ = '\\'; *write++ = 'u'; }
                        }
                        else { *write++ = '\\'; *write++ = 'u'; }
                        break;
                    default:
                        *write++ = c; // 其他字符原样保留
                        break;
                }
            }
            else
            {
                *write++ = c;
            }
            read++;
        }

        input->ReLength((uint)(write - start));
    }

    /// <summary>
    /// 解码 json 字符串 (Arena 模式)
    /// </summary>
    public static UnManagedString DecodeJsonString(ReadOnlySpan<char> input, JsonParsingArena* arena)
    {
        if (input.IsEmpty) return new UnManagedString();

        fixed (char* start = input)
        {
            char* read = start;
            char* end = read + input.Length;

            // 1. 快速扫描
            while (read < end && *read != '\\') read++;

            if (read >= end)
            {
                // 无转义：直接在 Arena 中分配并复制 (或者如果 Arena 支持，可以只复制)
                // 为了保持 UnManagedMemory 的生命周期由 Arena 管理，我们需要复制
                char* dest = arena->Alloc((uint)input.Length);
                uint byteLen = (uint)input.Length * sizeof(char);
                Unsafe.CopyBlock(dest, start, byteLen);
                return new UnManagedString(dest, (uint)input.Length, 0);
            }

            // 2. 有转义：分配最大可能长度
            char* writeStart = arena->Alloc((uint)input.Length);
            char* write = writeStart;
            read = start;

            while (read < end)
            {
                char c = *read;
                if (c == '\\')
                {
                    read++;
                    if (read >= end) { *write++ = '\\'; break; }
                    c = *read;
                    switch (c)
                    {
                        case '"': *write++ = '"'; break;
                        case '\\': *write++ = '\\'; break;
                        case '/': *write++ = '/'; break;
                        case 'b': *write++ = '\b'; break;
                        case 'f': *write++ = '\f'; break;
                        case 'n': *write++ = '\n'; break;
                        case 'r': *write++ = '\r'; break;
                        case 't': *write++ = '\t'; break;
                        case '\'': *write++ = '\''; break;
                        case 'u':
                        
                            if (read + 4 <= end)
                            {
                                // ... (简化 Hex 解析，复用现有逻辑或复制) ...
                                // 为简洁，这里假设 Hex 解析逻辑相同
                                int val = 0; bool success = true;
                                for (int i = 1; i <= 4; i++)
                                {
                                    char h = read[i]; int digit = 0;
                                    if (h >= '0' && h <= '9') digit = h - '0';
                                    else if (h >= 'a' && h <= 'f') digit = h - 'a' + 10;
                                    else if (h >= 'A' && h <= 'F') digit = h - 'A' + 10;
                                    else { success = false; break; }
                                    val = (val << 4) | digit;
                                }
                                if (success) { *write++ = (char)val; read += 4; }
                                else { *write++ = '\\'; *write++ = 'u'; }
                            }
                            else { *write++ = '\\'; *write++ = 'u'; }
                            break;
                        default: *write++ = c; break;
                    }
                }
                else { *write++ = c; }
                read++;
            }
            return new UnManagedString(writeStart, (uint)(write - writeStart), 0);
        }
    }


    /// <summary>
    /// 检测这段字符串表达的类型
    /// </summary>
    /// <param name="json">左右两边有双引号就是字符串，否则就是基元类型</param>
    /// <returns></returns>
    public static JsonSerializeTypes CheckJsonValueType(UnManagedString* json)
    //输入源在绝大部分情况下都是来自某段既有字符串的局部，实参的0下标并非原始字符串的0起始下标，所以形参只能做 Span, 做不了 UnManagedMemory
    {
        JsonSerializeTypes _type;

        if (json->IsEmpty)
        {
            _type = JsonSerializeTypes.Null;
        }
        else
        {
            char firstChar = json->Pointer[0];
            char lastChar = json->Pointer[json->UsageSize - 1];

            if ((firstChar == '\"' && lastChar == '\"') || (firstChar == '\'' && lastChar == '\''))
            {
                _type = JsonSerializeTypes.String;
            }
            else
            {
                _type = firstChar switch
                {
                    // 合并为一条 arm，同时包含 0-9 和 '-'。
                    
                    '-' or '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9' => JsonSerializeTypes.Number,
                    '{' when lastChar == '}' => JsonSerializeTypes.Object,
                    '[' when lastChar == ']' => JsonSerializeTypes.Array,
                    't' or 'T' => JsonSerializeTypes.Boolean,
                    'f' or 'F' => JsonSerializeTypes.Boolean,
                    'n' or 'N' => JsonSerializeTypes.Null,

                    // 例如 undefined 也可以包含在里面，以及更多未知情况
                    _ => JsonSerializeTypes.Undefined,
                };

            }
        }


        return _type;
    }


    static int EndOf(UnManagedString chars, ReadOnlySpan<char> symbols)
    {
        // 测试源： {"a\":bbb":"ccc"}   → key 是 a\":bbb，引号前有奇数个 \，是转义引号
        // 测试源： {"a\\":bbb":"ccc"}  → key 是 a\\，引号前有偶数个 \（反斜杠本身被转义），
        //                                引号是真正的边界，必须停在这里

        int length_count = 0; //统计真实的 IndexOf 下标

    SEARCH:
        int endOfKey = chars.IndexOf(symbols);

        if (endOfKey < 0) return -1;

        if (endOfKey == 0) return 0; //0 下标没有前一个字符，直接是正确的结果

        length_count += endOfKey;

        // 向前统计紧邻的连续反斜杠数量。
        //   奇数个 → 目标符号的第一个字符被转义，需要继续向后搜索。
        //   偶数个 → 反斜杠已成对（每对转义一个 \），目标符号未被转义，是真正的边界。
        
        int slashRun = 0;
        int scanBack = endOfKey - 1;
        while (scanBack >= 0 && *chars.Index(scanBack) == '\\')
        {
            slashRun++;
            scanBack--;
        }

        if ((slashRun & 1) == 1) // 奇数个反斜杠：目标符号被转义，继续查找
        {
            int next = endOfKey + 1; // 从当前匹配位置的下一个字符继续搜索
            if (next >= chars.UsageSize) return -1;
            chars = chars.Slice((uint)next);
            length_count += 1;
            goto SEARCH;
        }

        return length_count;
    }



    //查找对象字符串的结束符号 }
    private static int FindMatchingBrace(ReadOnlySpan<char> json, int startIndex)
    {
        int nestingLevel = 1;
        bool inString = false;



        // 从第一个 '{' 之后开始扫描
        for (int i = startIndex + 1; i < json.Length; i++)
        {
            char c = json[i];

            if (c == '"')
            {
                // 简单的转义检查：如果前一个字符不是'\'，则切换inString状态
                if (i == 0 || json[i - 1] != '\\')
                {
                    inString = !inString;
                }
            }
            else if (!inString) // 只在字符串外部处理括号
            {
                if (c == '{')
                {
                    nestingLevel++;
                }
                else if (c == '}')
                {
                    nestingLevel--;
                    if (nestingLevel == 0)
                    {
                        return i; // 找到了！
                    }
                }
            }
        }

        return -1; // 未找到
    }


    public static bool DecodeObjectString_Full(UnManagedString* source, ValueDictionary<UnManagedString, UnManagedString>* result_dictionary)
    {
        if (source is null) return false;

        if (result_dictionary is null) return false;


        //json内容中如果原义中存在这些符号，它们当前也是被转义的状态，这里的替换不会造成污染
        //这几处替换只针对json间隙里的特殊符号，例如 {"name":"my name",\t"name 2":"my name 2"\r\n}
        // IntellStringReplace.Replace(json,"\t","");
        // IntellStringReplace.Replace(json,"\r","");
        // IntellStringReplace.Replace(json, "\n", "");


        uint length = source->UsageSize;

        if (length < 2) return false; // start with {" or {}

        char endOfChar = *source->Index((int)length - 1);

        if (endOfChar != '}') return false; // 合法的 object json string 的结束必然是 }

        UnManagedString span_json = source->Slice(2, length - 2);

        bool fin = false;


    Begin:


        int endOfKey = EndOf(span_json, "\":"); //首先需要正确查找到 key 的结束点

        if (endOfKey < 0) return false;

        UnManagedString key = span_json.Slice(0, (uint)endOfKey);

        UnManagedString value;

        int endOfValue;

        //value 的第一个字符
        char value_firstChar = *span_json.Index(endOfKey + 2);

        if (value_firstChar == '"') //必然是字符串
        {
            // 从 valueStart（包含引号的位置）向后扫描，处理转义后找到真正的结束引号，
            // 再在结束引号之后确认是 , 还是 }，以此定位 endOfValue 的准确位置。
            int scanPos = endOfKey + 3; // 跳过 `":` 之后的开头 `"`，从值内容首字符开始
            endOfValue = -1;
            while (scanPos < span_json.UsageSize)
            {
                char sc = *span_json.Index(scanPos);
                if (sc == '\\')
                {
                    // 跳过转义字符（包括 \uXXXX 的四个 hex 位）
                    if (scanPos + 1 < span_json.UsageSize)
                    {
                        char next = *span_json.Index(scanPos + 1);
                        scanPos += (next == 'u') ? 6 : 2; // \uXXXX 跳 6，其余跳 2
                    }
                    else
                    {
                        scanPos++;
                    }
                    continue;
                }
                if (sc == '"')
                {
                    // 找到了真正的结束引号，endOfValue 记录这个引号的位置
                    endOfValue = scanPos; // 指向结束 "
                    break;
                }
                scanPos++;
            }
            // endOfValue 此时指向结束引号；-1 表示格式非法（引号未闭合），
            // 后续 if (endOfValue > -1) 分支会用它计算 value 和推进 span_json
        }
        else
        {
            if (value_firstChar == '{')
            {
                // 直接在 span_json 上从 endOfKey+2 的位置（即 value_firstChar == '{'）
                // 开始搜索，把 startIndex 设为 endOfKey+2，避免任何负偏移。
                // FindMatchingBrace 期望在 json[startIndex] 处遇到 '{'，并从 startIndex+1 开始
                // 向后计数嵌套层级，最终返回匹配 '}' 的绝对下标。
                
                endOfValue = FindMatchingBrace(span_json.AsSpan(), endOfKey + 2);
            }
            else
            {
                //数字模式
                endOfValue = span_json.IndexOf(','); //value没有双引号，key 与 value 之间以逗号分隔
            }
        }

        //如果 value 没有双引号

        if (endOfValue > -1)
        {
            if (value_firstChar == '"')
            {
                
                //   endOfValue 指向结束引号（含引号）的位置。
                //   value 范围 = [endOfKey+2 .. endOfValue]，包含首尾两个引号，
                //   供 CheckJsonValueType 正确识别为 String。
                //   推进 span_json：跳过结束引号(+1) 以及之后可能的逗号(+1)，
                //   即 endOfValue + 2；若 endOfValue 是最后字符则 Slice 长度为 0，循环自然终止。
                
                value = span_json.Slice((uint)(endOfKey + 2), (uint)(endOfValue - (endOfKey + 2) + 1)); // 包含结束引号 / include closing quote
                uint advance = (uint)endOfValue + 2;
                span_json = advance < span_json.UsageSize
                    ? span_json.Slice(advance)
                    : span_json.Slice(span_json.UsageSize); // 已到末尾，产生空 slice
            }
            else
            {
                // 数字/布尔模式（原逻辑不变）：endOfValue 指向逗号位置
                value = span_json.Slice((uint)endOfKey + 2, (uint)(endOfValue - (endOfKey + 2)));
                span_json = span_json.Slice((uint)endOfValue + 2);
            }
        }
        else
        {
            endOfValue = span_json.LastIndexOf('}');
            value = span_json.Slice((uint)endOfKey + 2, (uint)(endOfValue - (endOfKey + 2)));
            span_json = span_json.Slice((uint)endOfValue + 1);

            fin = true;
        }


        JsonSerializeTypes json_type = CheckJsonValueType(&value); //必须保留双引号，这里才能正确计算

        if (json_type is JsonSerializeTypes.String) value = value.Slice(1, value.UsageSize - 2); //如果它是字符串，这里再减去双引号



        UnManagedString decode_key = DecodeJsonString(key.AsSpan());

        UnManagedString decode_value = DecodeJsonString(value.AsSpan());

        decode_value.SerializeType = json_type;


        //如果对象模式，这里进行递归分解
        result_dictionary->AddOrUpdate(&decode_key, &decode_value);


        if (fin)
        {
            return true;
        }

        goto Begin;
    }






    /// <summary>
    /// 反序列化对象模式字符串，把解析结果追加到 result_dictionary
    /// </summary>
    /// <param name="source">json字符串</param>
    /// <param name="result_dictionary">用于存储结果的值类型字典</param>
    /// <returns></returns>
    public static bool DecodeObjectString_AppendToDictionary(ReadOnlySpan<char> source, ValueDictionary<UnManagedString, UnManagedString>* result_dictionary)
    {

        if (source.IsEmpty) return false;
        if (result_dictionary is null) return false;


        UnManagedString json;


        if (source.Length < stack_limit)
        {
            char* cache = stackalloc char[source.Length];
            json = new UnManagedString(cache, (uint)source.Length, 0);
            json.AddRange(source);
        }
        else
        {
            json = new UnManagedString(source);
        }

        bool result = DecodeObjectString_AppendToDictionary(&json, result_dictionary);

        json.Dispose();

        return result;

    }



    /// <summary>
    /// 反序列化对象模式字符串，把解析结果追加到 result_dictionary
    /// </summary>
    /// <param name="source">json字符串</param>
    /// <param name="result_dictionary">用于存储结果的值类型字典</param>
    /// <returns></returns>
    public static bool DecodeObjectString_AppendToDictionary(UnManagedMemory<char>* source, ValueDictionary<UnManagedMemory<char>, UnManagedMemory<char>>* result_dictionary)
    {
        if (source is null) return false;
        if (result_dictionary is null) return false;

        // 优化：O(N) 线性扫描解析，移除 O(N^2) 的 Slice/IndexOf 循环
        
        uint length = source->UsageSize;
        if (length < 2) return false; // start with {" or {}

        char endOfChar = source->Pointer[length - 1];

        if (endOfChar != '}') return false; // 合法的 object json string 的结束必然是 }

        char* start = source->Pointer;
        char* end = start + length - 1; // 指向最后的 '}'
        char* current = start + 1; // 跳过开头的 '{'

        while (current < end)
        {
            // 1. 跳过 Key 前的空白
            while (current < end && *current <= ' ') current++;
            if (current >= end) break;

            // 2. 解析 Key (必须是字符串)
            if (*current != '"') return false;
            
            char* keyStart = current;
            current++; // 跳过 '"'
            
            // 查找 Key 的结束引号
            while (current < end)
            {
                if (*current == '"')
                {
                    // 检查转义: 偶数个反斜杠意味着引号未被转义
                    int slashCount = 0;
                    char* p = current - 1;
                    while (p > keyStart && *p == '\\') { slashCount++; p--; }
                    if ((slashCount & 1) == 0) break;
                }
                current++;
            }
            if (current >= end) return false;
            
            char* keyEnd = current;
            current++; // 跳过 Key 的结束 '"'

            // 3. 解析冒号
            while (current < end && *current <= ' ') current++;
            if (current >= end || *current != ':') return false;
            current++; // 跳过 ':'

            // 4. 跳过 Value 前的空白
            while (current < end && *current <= ' ') current++;
            if (current >= end) return false;

            // 5. 解析 Value
            char* valueStart = current;
            char* valueEnd = current;
            JsonSerializeTypes json_type = JsonSerializeTypes.Undefined;
            
            char c = *current;
            if (c == '"')
            {
                json_type = JsonSerializeTypes.String;
                current++;
                while (current < end)
                {
                    if (*current == '"')
                    {
                        int slashCount = 0;
                        char* p = current - 1;
                        while (p > valueStart && *p == '\\') { slashCount++; p--; }
                        if ((slashCount & 1) == 0) break;
                    }
                    current++;
                }
                if (current >= end) return false;
                valueEnd = current;
                current++; // 跳过 '"'
            }
            else if (c == '{')
            {
                json_type = JsonSerializeTypes.Object;
                int depth = 1;
                current++;
                while (current < end && depth > 0)
                {
                    if (*current == '{') depth++;
                    else if (*current == '}') depth--;
                    else if (*current == '"') // 跳过对象内的字符串，防止字符串内含 }
                    {
                        current++;
                        while (current < end)
                        {
                            if (*current == '"')
                            {
                                int slashCount = 0;
                                char* p = current - 1;
                                while (p > valueStart && *p == '\\') { slashCount++; p--; }
                                if ((slashCount & 1) == 0) break;
                            }
                            current++;
                        }
                    }
                    current++;
                }
                if (depth > 0) return false;
                valueEnd = current - 1;
            }
            else if (c == '[')
            {
                json_type = JsonSerializeTypes.Array;
                int depth = 1;
                current++;
                while (current < end && depth > 0)
                {
                    if (*current == '[') depth++;
                    else if (*current == ']') depth--;
                    else if (*current == '"')
                    {
                        current++;
                        while (current < end)
                        {
                            if (*current == '"')
                            {
                                int slashCount = 0;
                                char* p = current - 1;
                                while (p > valueStart && *p == '\\') { slashCount++; p--; }
                                if ((slashCount & 1) == 0) break;
                            }
                            current++;
                        }
                    }
                    current++;
                }
                if (depth > 0) return false;
                valueEnd = current - 1;
            }
            else
            {
                // Number, Boolean, Null
                while (current < end && *current != ',' && *current != '}' && *current > ' ') current++;
                valueEnd = current - 1;
                
                if (c == 't' || c == 'f') json_type = JsonSerializeTypes.Boolean;
                else if (c == 'n') json_type = JsonSerializeTypes.Null;
                else json_type = JsonSerializeTypes.Number;
            }

            // 6. 提取并解码 Key 和 Value
            ReadOnlySpan<char> keySpan = new ReadOnlySpan<char>(keyStart + 1, (int)(keyEnd - keyStart - 1));
            UnManagedMemory<char> decode_key = DecodeJsonString(keySpan);

            ReadOnlySpan<char> valueSpan;
            if (json_type == JsonSerializeTypes.String)
                valueSpan = new ReadOnlySpan<char>(valueStart + 1, (int)(valueEnd - valueStart - 1));
            else
                valueSpan = new ReadOnlySpan<char>(valueStart, (int)(valueEnd - valueStart + 1));

            UnManagedMemory<char> decode_value = DecodeJsonString(valueSpan);
            decode_value.SerializeType = json_type;

            if (!result_dictionary->Add(&decode_key, &decode_value))
            {
                decode_key.Dispose();
                decode_value.Dispose();
                result_dictionary->Clear();
                return false;
            }

            // 7. 处理逗号或结束
            while (current < end && *current <= ' ') current++;
            if (current >= end) return true;
            
            if (*current == ',') current++;
            else if (*current == '}') return true;
            else return false;
        }

        return true;
    }






    /// <summary>
    /// 序列化对象，输出 {"name":"my name",......} 形式，输出到指定的内存地址
    /// </summary>
    /// <param name="source"></param>
    /// <param name="output"></param>
    /// <returns>执行结果描述</returns>
    public static SerializeResult SerializeObject(ValueDictionary<UnManagedString, UnManagedString>* source, UnManagedString* output)
    {

        if (source is null || output is null || source->IsEmpty) return new SerializeResult { Status = SerializeResultEnum.Null_Or_Empty };

        uint loopCount = source->Count;

        if (output->Capacity < 2) //最少要保证有空间容纳 "{}"，以及如果这是一个空对象，在此也能初始化容量
        {
            if (output->OnStack)
                return new SerializeResult { Status = SerializeResultEnum.Failed_StackReSize }; //但是如果是栈内存就没办法了，只能通知外部放弃操作
            else
                output->Resize(2);
        }


        output->Zero();


        output->Add('{');


        loopKeyValueResult loopKeyValueResult = new loopKeyValueResult
        {
            Output = output,
            Result = new SerializeResult { Status = SerializeResultEnum.UnDefined }
        };


        source->ForEach(&loopKeyValue, &loopKeyValueResult);

        if (loopKeyValueResult.Result.Status != SerializeResultEnum.UnDefined)
        {
            return loopKeyValueResult.Result;
        }

        output->SetValue(output->UsageSize - 1, '}');

        return new SerializeResult { Status = SerializeResultEnum.OK };

    }













    ref struct loopKeyValueResult
    {
        public UnManagedString* Output;

        public SerializeResult Result;
    }


    static bool loopKeyValue(int index, UnManagedString* key, UnManagedString* value, void* caller)
    {
        loopKeyValueResult* output = (loopKeyValueResult*)caller;

        SerializeResult key_success = SerializeString(key, output->Output, ':', false, true);  //key 肯定有双引号，肯定需要右侧结束符号


        //默认逻辑： Photon两个参数都是字符串，默认会有双引号。 除非已经特别注明不属于 SerializeTypes.String
        JsonSerializeTypes string_type_code = JsonSerializeTypes.String;
        bool nativeString = value->SerializeType != string_type_code;


        SerializeResult value_success = SerializeString(value, output->Output, ',', nativeString, true);


        if (!key_success.Success)
        {
            output->Result = key_success;
            return false;
        }

        if (!value_success.Success)
        {
            output->Result = value_success;
            return false;
        }


        return true;
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="result"></param>
    /// <param name="indexs"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void processSchema(UnManagedString* source, UnManagedString* result, UnManagedMemory<uint>* indexs)
    {
        if (indexs->UsageSize < 1) return;

        char* p_source = source->Pointer;
        char* p_result_base = result->Pointer;
        uint current_result_offset = result->UsageSize;

        uint* p_indexs = indexs->Pointer;
        uint index_count = indexs->UsageSize;

        uint subStringOddCount = 0; //统计已经处理多少个字符

        for (int i = 0; i < index_count; i++)
        {
            //如果特殊字符之前存在正常字符，把它们一次性处理完毕
            uint specialCharIdx = p_indexs[i];
            uint slice_unsearch = specialCharIdx - subStringOddCount;

            if (slice_unsearch > 0)
            {
                Unsafe.CopyBlock(p_result_base + current_result_offset, p_source + subStringOddCount, (uint)(slice_unsearch * sizeof(char)));
                current_result_offset += slice_unsearch;
                subStringOddCount += slice_unsearch;
            }

            // 复制正常字符完毕
            //===========

            char specialChar = p_source[subStringOddCount]; // p_mainAddress points to source + subStringOddCount

            //  10:\n  13:\r  9:\t  92:\\  34:"
            if (specialChar < 93)
            {
                p_result_base[current_result_offset++] = '\\';

                switch (specialChar)
                {
                    case '\n':
                        p_result_base[current_result_offset++] = 'n';
                        break;
                    case '\r':
                        p_result_base[current_result_offset++] = 'r';
                        break;
                    case '\t':
                        p_result_base[current_result_offset++] = 't';
                        break;
                    case '\\':
                        p_result_base[current_result_offset++] = '\\';
                        break;
                    case '\"':
                        p_result_base[current_result_offset++] = '\"';
                        break;

                    //上面的符号是需要转换成字母的

                    default: continue;
                }

                subStringOddCount += 1;
            }
        }


        //在处理完所有特殊字符后，还剩余多少正常字符未处理
        uint least = source->UsageSize - subStringOddCount;
        if (least > 0)
        {
            //最后剩余未处理的正常字符
            Unsafe.CopyBlock(p_result_base + current_result_offset, p_source + subStringOddCount, (uint)(least * sizeof(char)));
            current_result_offset += least;
        }

        result->ReLength(current_result_offset);
    }



    /// <summary>
    /// 
    /// </summary>
    static delegate*<UnManagedString*, UnManagedString*, UnManagedMemory<uint>*, void> p_processSchema = &processSchema;

    /// <summary>
    /// 构建集合
    /// </summary>
    /// <param name="source"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    public static SerializeResult SerializeCollection(ValueLinkedList<UnManagedString>* source, UnManagedString* output)
    {
        //需要一个基于字符串的节点描述

        if (source->IsEmpty || source->NodesCount <= 0) { return new SerializeResult { Status = SerializeResultEnum.Null_Or_Empty }; }


        if (output->Capacity < 2) //最少要保证有空间容纳 "[]"，以及如果这是一个空对象，在此也能初始化容量
        {
            if (output->OnStack)
                return new SerializeResult { Status = SerializeResultEnum.Failed_StackReSize }; //但是如果是栈内存就没办法了，只能放弃
            else
                output->Resize(2);
        }

        output->Zero(); // 之前可能已经被使用，以后也可以再次利用，但是必须在此保证指针归 0 位， 否则 json 就会是非法的

        output->Add('[');

        for (int i = 0; i < source->NodesCount; i++)
        {
            UnManagedString* source_item = source->Index(i);

            //默认逻辑： Photon两个参数都是字符串，默认会有双引号。 除非已经特别注明不属于 SerializeTypes.String
            bool nativeString = source_item->SerializeType != JsonSerializeTypes.String;

            SerializeResult process_item = SerializeString(source_item, output, ',', nativeString, true);

            if (!process_item.Success)
                return process_item;
        }


        output->SetValue(output->UsageSize - 1, ']');

        return new SerializeResult { Status = SerializeResultEnum.OK };
    }

    /// <summary>
    /// 构建集合
    /// </summary>
    /// <param name="source"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    public static SerializeResult SerializeCollection(UnManagedMemory<UnManagedString>* source, UnManagedString* output)
    {
        //需要一个基于字符串的节点描述

        if (source->IsEmpty || source->UsageSize <= 0) { return new SerializeResult { Status = SerializeResultEnum.Null_Or_Empty }; }


        if (output->Capacity < 2) //最少要保证有空间容纳 "[]"，以及如果这是一个空对象，在此也能初始化容量
        {
            if (output->OnStack)
                return new SerializeResult { Status = SerializeResultEnum.Failed_StackReSize }; //但是如果是栈内存就没办法了，只能放弃
            else
                output->Resize(2);
        }

        output->Zero(); // 之前可能已经被使用，以后也可以再次利用，但是必须在此保证指针归 0 位， 否则 json 就会是非法的

        output->Add('[');

        for (int i = 0; i < source->UsageSize; i++)
        {

            UnManagedString* source_item = &source->Pointer[i];

            //默认逻辑： Photon两个参数都是字符串，默认会有双引号。 除非已经特别注明不属于 SerializeTypes.String
            bool nativeString = source_item->SerializeType != JsonSerializeTypes.String;

            SerializeResult process_item = SerializeString(source_item, output, ',', nativeString, true);

            if (!process_item.Success)
                return process_item;
        }


        output->SetValue(output->UsageSize - 1, ']');

        return new SerializeResult { Status = SerializeResultEnum.OK };
    }




    /// <summary>
    /// 判断泛型T是否是数字类型
    /// </summary>
    /// <returns></returns>
    static bool IsNumericType(Type t)
    {

        return t == TypeByte || t == TypeSByte ||
               t == TypeShort || t == TypeUShort ||
               t == TypeInt || t == TypeUInt ||
               t == TypeLong || t == TypeULong ||
               t == TypeFloat || t == TypeDouble ||
               t == TypeDecimal || t == TypeDateTime ||
               t == TypeBoolean || t == TypeEnum || t.IsEnum
               ;
    }






    /// <summary>
    /// 构建基元类型集合（例如 int, double ,enum, bool 基本值类型等集合，不含 string 和自定义 struct）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="output">如果只是简单获取值，外部一个空UnManagedRam对象即可，例如 UnManagedMemory.CreateEmpty()</param>
    /// <returns></returns>
    public static SerializeResult SerializeBaseCollection<T>(UnManagedMemory<T>* source, UnManagedString* output)
    where T : unmanaged, Enum
    {
        //需要一个基于字符串的节点描述

        if (source->IsEmpty || source->UsageSize <= 0) { return new SerializeResult { Status = SerializeResultEnum.Null_Or_Empty }; }

        Type t = typeof(T);

        if (!IsNumericType(t)) { return new SerializeResult { Status = SerializeResultEnum.FailedTypes }; }


        if (output->Capacity < 2) //最少要保证有空间容纳 "[]"，以及如果这是一个空对象，在此也能初始化容量
        {
            if (output->OnStack)
                return new SerializeResult { Status = SerializeResultEnum.Failed_StackReSize }; //但是如果是栈内存就没办法了，只能放弃
            else
                output->Resize(2);
        }

        output->Zero(); // 之前可能已经被使用，以后也可以再次利用，但是必须在此保证指针归 0 位， 否则 json 就会是非法的

        output->Add('[');

        for (int i = 0; i < source->UsageSize; i++)
        {
            T* valueNode = &source->Pointer[i];

            UnManagedString source_item = default; // BUG8 修复：明确初始化，防止 default 分支走漏时传入垃圾指针

            bool nativeType = true;

            //==========

            switch (t)
            {
                case Type _ when t == TypeInt:
                    {
                        int charNode = *(int*)valueNode;
                        source_item = charNode.IntToUnmanagedString();
                        break;
                    }
                case Type _ when t == TypeLong:
                    {
                        long charNode = *(long*)valueNode;
                        source_item = charNode.LongToUnmanagedString();
                        break;
                    }
                case Type _ when t == TypeDecimal:
                    {
                        decimal charNode = *(decimal*)valueNode;
                        source_item = charNode.DecimalToUnmanagedString();
                        break;
                    }
                case Type _ when t == TypeByte:
                    {
                        byte charNode = *(byte*)valueNode;
                        source_item = charNode.ByteToUnmanagedString();
                        break;
                    }
                case Type _ when t == TypeSByte:
                    {
                        sbyte charNode = *(sbyte*)valueNode;
                        source_item = charNode.SbyteToUnmanagedString();
                        break;
                    }
                case Type _ when t == TypeShort:
                    {
                        short charNode = *(short*)valueNode;
                        source_item = charNode.ShortToUnmanagedString();
                        break;
                    }
                case Type _ when t == TypeUShort:
                    {
                        ushort charNode = *(ushort*)valueNode;
                        source_item = charNode.UshortToUnmanagedString();
                        break;
                    }
                case Type _ when t == TypeUInt:
                    {
                        uint charNode = *(uint*)valueNode;
                        source_item = charNode.UintToUnmanagedString();
                        break;
                    }
                case Type _ when t == TypeULong:
                    {
                        ulong charNode = *(ulong*)valueNode;
                        source_item = charNode.UlongToUnmanagedString();
                        break;
                    }
                case Type _ when t == TypeFloat:
                    {
                        float charNode = *(float*)valueNode;
                        source_item = charNode.FloatToUnmanagedString();
                        break;
                    }
                case Type _ when t == TypeDouble:
                    {
                        double charNode = *(double*)valueNode;
                        source_item = charNode.DoubleToUnmanagedString();
                        break;
                    }
                case Type _ when t == TypeDateTime:
                    {
                        DateTime charNode = *(DateTime*)valueNode;
                        source_item = charNode.DateTimeToUnmanagedString();
                        nativeType = false;
                        break;
                    }
                case Type _ when t == TypeBoolean:
                    {
                        bool charNode = *(bool*)valueNode;
                        source_item = charNode.BoolToUnmanagedString();
                        break;
                    }
                    
                case Type _ when t.IsEnum:
                    {
                        source_item = UnManagedMemoryHelper.ParseFromEnum(*valueNode);
                        nativeType = false;
                        break;
                    }
                default:
                
                    return new SerializeResult { Status = SerializeResultEnum.FailedTypes };
            }

            SerializeResult process_item = SerializeString(&source_item, output, ',', nativeType, true);

            if (!process_item.Success)
                return process_item;
        }

        output->SetValue(output->UsageSize - 1, ']');

        return new SerializeResult { Status = SerializeResultEnum.OK };
    }


    /// <summary>
    /// 类型判断
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public static JsonSerializeTypes checkTypes(UnManagedString* target)
    {
        //0:48 1:49 2:50 3:51 4:52 5:53 6:54 7:55 8:56 9:57 [:91 ]:93 F:70 N:78 T:84 f:102 n:110 t:116


        char firstChar = target->Pointer[0];
        char lastChar = target->Pointer[target->UsageSize - 1];
        JsonSerializeTypes t;


        if (firstChar == '"' && lastChar == '"')
        {
            t = JsonSerializeTypes.String;
        }
        else
        {
            t = firstChar switch
            {
                >= '0' and <= '9' => JsonSerializeTypes.Number,
                '-' => JsonSerializeTypes.Number,           // 负数 / negative numbers
                '{' when lastChar == '}' => JsonSerializeTypes.Object,
                '[' when lastChar == ']' => JsonSerializeTypes.Array,
                't' or 'T' => JsonSerializeTypes.Boolean,
                'f' or 'F' => JsonSerializeTypes.Boolean,
                'n' or 'N' => JsonSerializeTypes.Null,
                _ => JsonSerializeTypes.Any,
            };
        }

        return t;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static uint Predicate_Symbols(UnManagedString* text, UnManagedMemory<uint>* cacheIndexs)
    {
        if (text is null || cacheIndexs is null) return 0;

        if (text->UsageSize < 1) return 0;

        ReadOnlySpan<char> source = text->AsSpan();
        uint predicate_symbolsCount = 0;

        uint* p_cacheIndexs = cacheIndexs->Pointer;
        uint idx_count = 0;

        if (Vector.IsHardwareAccelerated)
        {
            int vectorSize = Vector<ushort>.Count;
            int i = 0;
            for (; i <= source.Length - vectorSize; i += vectorSize)
            {
                ReadOnlySpan<ushort> vector_item = MemoryMarshal.Cast<char, ushort>(source.Slice(i, vectorSize));

                Vector<ushort> sourceVector = new Vector<ushort>(vector_item);

                Vector<ushort> result = Vector.BitwiseOr(
                    Vector.BitwiseOr(Vector.BitwiseOr(Vector.BitwiseOr(
                        Vector.Equals(sourceVector, newlineVector),
                        Vector.Equals(sourceVector, carriageReturnVector)),
                        Vector.Equals(sourceVector, tabVector)),
                        Vector.Equals(sourceVector, backslashVector)),
                        Vector.Equals(sourceVector, quoteVector));

                for (int j = 0; j < vectorSize; j++)
                {
                    if (result[j] != 0)
                    {
                        predicate_symbolsCount += 1;

                        p_cacheIndexs[idx_count++] = (uint)i + (uint)j;
                    }
                }
            }

            i = source.Length - (source.Length % vectorSize);
            for (; i < source.Length; i++)
            {
                char c = source[i];
                if (c == '\n' || c == '\r' || c == '\t' || c == '\\' || c == '\"')
                {
                    predicate_symbolsCount += 1;

                    p_cacheIndexs[idx_count++] = (uint)i;
                }
            }
        }
        else
        {
            return Predicate_Symbols_Common(text, cacheIndexs);
        }

        cacheIndexs->ReLength(idx_count);
        return text->UsageSize + predicate_symbolsCount;

    }



    /// <summary>
    /// 字符串哪些位置的字符是特殊符号，以及返回字符串在处理特殊符号后的总长度
    /// </summary>
    /// <param name="text"></param>
    /// <param name="cacheIndexs">如果是堆内存，建议传入一个Pointer为空的对象，这样可以保证只有在检测到特殊符号才会去创建内存</param>
    /// <returns>处理特殊符号后的字符串总长度</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static uint Predicate_Symbols_Common(UnManagedString* text, UnManagedMemory<uint>* cacheIndexs)
    {
        if (text is null || cacheIndexs is null) return 0;
        if (text->UsageSize < 1) return 0;

        //预测所有特殊符号的数量
        uint predicate_symbolsCount = 0;

        uint* p_cacheIndexs = cacheIndexs->Pointer;
        uint idx_count = 0;

        for (int i = 0; i < text->UsageSize; i++)
        {
            char c = text->Pointer[i];

            if (c < 93) //这些字符的最大数字值是 92
            {
                if (c == '\n' || c == '\r' || c == '\t' || c == '\\' || c == '\"')
                {
                    predicate_symbolsCount += 1;

                    p_cacheIndexs[idx_count++] = (uint)i;
                }
            }
        }
        cacheIndexs->ReLength(idx_count);

        //预测整段字符串处理特殊符号后的最终长度
        //详细的算法是： text.NodesCount -= predicate_symbolsCount, text.NodesCount += predicate_symbolsCount * 2
        // * 2 是每个符号最后都会替换为  \n  这样的二个字长
        //这里简化计算，直接加上就行了：

        return text->UsageSize + predicate_symbolsCount;

    }




    /// <summary>
    /// 序列化单一字符串（回调模式）
    /// </summary>
    /// <param name="text"></param>
    /// <param name="output"></param>
    /// <param name="endSymbol"></param>
    /// <param name="baseString">是否基本字符串，这些字符串不需要添加双引号， 例如 123, true</param>
    /// <param name="appendEndSymbol">无双引号的情况下是否需要添加分隔符</param>
    /// <returns>是否成功序列化</returns>
    public static SerializeResult SerializeString(UnManagedString* text, UnManagedString* output, char endSymbol = '\0', bool baseString = true, bool appendEndSymbol = false)
    {
        // 优化 1: SIMD 加速的两遍扫描 (Two-Pass)
        // Pass 1: 计算转义后的长度

        uint extraLength = 0;
        uint textLen = text->UsageSize;
        char* ptr = text->Pointer;

        if (textLen > 0)
        {
            uint i = 0;
            // SIMD Pass 1
            if (Vector.IsHardwareAccelerated && textLen >= Vector<ushort>.Count)
            {
                int vCount = Vector<ushort>.Count;
                for (; i <= textLen - vCount; i += (uint)vCount)
                {
                    Vector<ushort> v = Unsafe.Read<Vector<ushort>>(ptr + i);

                    // 检查是否包含任何特殊字符
                    Vector<ushort> hasSpecial = Vector.BitwiseOr(
                        Vector.BitwiseOr(Vector.Equals(v, newlineVector), Vector.Equals(v, carriageReturnVector)),
                        Vector.BitwiseOr(Vector.Equals(v, tabVector),
                        Vector.BitwiseOr(Vector.Equals(v, backslashVector), Vector.Equals(v, quoteVector)))
                    );

                    if (!hasSpecial.Equals(Vector<ushort>.Zero))
                    {
                        // 块内有特殊字符，回退到标量统计
                        for (int k = 0; k < vCount; k++)
                        {
                            char c = ptr[i + k];
                            if (c < 93 && (c == '\n' || c == '\r' || c == '\t' || c == '\\' || c == '\"'))
                                extraLength++;
                        }
                    }
                }
            }

            // Scalar Tail Pass 1
            for (; i < textLen; i++)
            {
                char c = ptr[i];
                if (c < 93 && (c == '\n' || c == '\r' || c == '\t' || c == '\\' || c == '\"'))
                {
                    extraLength++;
                }
            }
        }

        uint totalLength = textLen + extraLength;
        if (!baseString) totalLength += 2; // ""
        if (appendEndSymbol) totalLength += 1; // endSymbol

        // 确保容量
        uint remain = output->UsageSize + totalLength;
        remain += remain / 2;
        remain = remain % 2 == 0 ? remain : remain + 1;

        bool validateSize = output->EnsureCapacity(remain);
        if (!validateSize)
            return new SerializeResult { Status = SerializeResultEnum.Failed_StackReSize };

        // 写入数据
        if (!baseString)
        {
            output->Add('"');
        }

        if (textLen > 0)
        {
            // 直接写入 output，避免中间 buffer
            char* dest = output->Pointer + output->UsageSize;
            uint i = 0;

            // SIMD Pass 2
            if (Vector.IsHardwareAccelerated && textLen >= Vector<ushort>.Count)
            {
                int vCount = Vector<ushort>.Count;
                for (; i <= textLen - vCount; i += (uint)vCount)
                {
                    Vector<ushort> v = Unsafe.Read<Vector<ushort>>(ptr + i);

                    Vector<ushort> hasSpecial = Vector.BitwiseOr(
                        Vector.BitwiseOr(Vector.Equals(v, newlineVector), Vector.Equals(v, carriageReturnVector)),
                        Vector.BitwiseOr(Vector.Equals(v, tabVector),
                        Vector.BitwiseOr(Vector.Equals(v, backslashVector), Vector.Equals(v, quoteVector)))
                    );

                    if (hasSpecial.Equals(Vector<ushort>.Zero))
                    {
                        // 块内无特殊字符，直接批量拷贝
                        Unsafe.Write(dest, v);
                        dest += vCount;
                    }
                    else
                    {
                        // 块内有特殊字符，标量处理
                        for (int k = 0; k < vCount; k++)
                        {
                            char c = ptr[i + k];
                            if (c < 93 && (c == '\n' || c == '\r' || c == '\t' || c == '\\' || c == '\"'))
                            {
                                *dest++ = '\\';
                                switch (c) { case '\n': *dest++ = 'n'; break; case '\r': *dest++ = 'r'; break; case '\t': *dest++ = 't'; break; case '\\': *dest++ = '\\'; break; case '\"': *dest++ = '"'; break; }
                            }
                            else { *dest++ = c; }
                        }
                    }
                }
            }

            // Scalar Tail Pass 2
            for (; i < textLen; i++)
            {
                char c = ptr[i];
                if (c < 93 && (c == '\n' || c == '\r' || c == '\t' || c == '\\' || c == '\"'))
                {
                    *dest++ = '\\';
                    switch (c)
                    {
                        case '\n': *dest++ = 'n'; break;
                        case '\r': *dest++ = 'r'; break;
                        case '\t': *dest++ = 't'; break;
                        case '\\': *dest++ = '\\'; break;
                        case '\"': *dest++ = '"'; break;
                    }
                }
                else
                {
                    *dest++ = c;
                }
            }

            // 以 dest 相对于 output 起始的实际位移作为新的 UsageSize，完全不依赖计数。
            output->ReLength((uint)(dest - output->Pointer));
        }

        if (!baseString)
        {
            output->Add('"');
            output->Add(endSymbol);//需要双引号的情况肯定会有分隔符号，不需要判断
        }
        else
        {
            if (appendEndSymbol)
            {
                output->Add(endSymbol);
            }
        }

        output->SerializeType = JsonSerializeTypes.String; //Json必须是字符串类型

        return new SerializeResult { Status = SerializeResultEnum.OK };

    }


    /// <summary>
    /// 序列化单一字符串
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static UnManagedString SerializeString(UnManagedString* text)
    {

        UnManagedString unm_string = new UnManagedString();

        SerializeString(text, &unm_string, '\0', true, false);


        return unm_string;
    }



    /// <summary>
    /// 解码集合类型的 json 字符串，返回每一串字符串原始值（注意，如果传入第三个参数，指针会被改变）
    /// </summary>
    /// <param name="json"></param>
    /// <param name="result"></param>
    /// <param name="memoryOutSide">使用外部内存存储 result 的里层 UnManagedMemory 指针指向，注意！如果传入值，方法执行后指针将会被累加，累加的数量是解码后所有节点的长度总和。 将以外部指针指向地址来存储数据，外部需要自行保证具备足够空间</param>
    /// <returns>返回解码后所有节点的长度总和</returns>
    public static uint DecodeJsonCollection(UnManagedString* json, UnManagedMemory<UnManagedString>* result, void** memoryOutSide = null)
    {

        bool existEndsSymbol = json->Pointer[json->UsageSize - 1] is ']';  // 最后的字符必须是 ]

        //即使一个空数组也必须是[]，两个字符。 如果连两个字符都没有的话就不用继续往下计算了
        if (json->UsageSize < 2 || !existEndsSymbol || result is null)
        {
            return 0;
        }

        // 2 作为起始是跳过 [" ， 因为数组第一个字符是 [， 第二个字符是 key 肯定有引号
        UnManagedString slice_sub = json->Slice(2, (uint)json->UsageSize - 2);

        uint charsCount = 0;

        while (!slice_sub.IsEmpty)
        {
            UnManagedString result_item;

            decodeJsonCollectionLoop(&slice_sub, &result_item);

            UnManagedString node;

            if (memoryOutSide is null)
            {
                node = result_item.Clone();
            }
            else
            {
                node = result_item.Clone((char*)*memoryOutSide);
                *memoryOutSide = (char*)*memoryOutSide + node.UsageSize;
            }

            result->Add(&node);
            charsCount += node.UsageSize;

            // result_item 是镜像器，不需要释放
        }

        return charsCount;

    }




    /// <summary>
    /// （处理完成后，source 会变成空值）
    /// </summary>
    /// <param name="source"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    static void decodeJsonCollectionLoop(UnManagedString* source, UnManagedString* value)
    {
        // 逐字符扫描，跳过转义序列，在裸露的 " 处识别元素边界。
        // 注意：DecodeJsonCollection 的 Slice(2, UsageSize-2) 已消费了每个元素的开头 "，
        // 所以 source 中第一个裸露的 " 就是当前元素的结束引号。
        

        int srcLen = (int)source->UsageSize;
        int closeQuotePos = -1;

        for (int i = 0; i < srcLen; i++)
        {
            char c = *source->Index(i);

            if (c == '\\')
            {
                // 跳过转义序列：\uXXXX 跳 5 位（u+4个hex），其余跳 1 位（转义字符本身）
                // Skip escape sequence: \uXXXX advances 5 extra chars; others advance 1.
                if (i + 1 < srcLen)
                {
                    char next = *source->Index(i + 1);
                    i += (next == 'u') ? 5 : 1;
                }
                continue;
            }

            if (c == '"')
            {
                // 找到了当前元素的结束引号
                // Found the closing quote of the current element.
                closeQuotePos = i;
                break;
            }
        }

        if (closeQuotePos < 0)
        {
            // 格式非法：找不到结束引号，输出空值并清空 source，终止循环
            // Malformed input: no closing quote found; emit empty value and clear source.
            *value = source->Slice(0, 0);
            *source = source->Slice(source->UsageSize);
            return;
        }

        // value = source[0 .. closeQuotePos)，即结束引号之前的原始内容
        // value = source[0 .. closeQuotePos), i.e. the raw content before the closing quote.
        *value = source->Slice(0, (uint)closeQuotePos);

        // 推进 source：跳过结束引号(+1)，再判断下一个字符
        //   ","  → 还有更多元素：跳过 " , "（共 2 个额外字符）后继续
        //   "]   → 这是最后一个元素：跳过 " ]（共 2 个额外字符）后 source 变空
        //   其他 → 格式异常，直接清空 source 终止循环
        
        uint afterQuote = (uint)closeQuotePos + 1; // 结束引号后一位 / position after closing quote
        if (afterQuote + 1 < source->UsageSize)
        {
            char sep = *source->Index((int)afterQuote);     // 应为 , 或 ]
            if (sep == ',' || sep == ']')
            {
                // 跳过 sep（逗号或 ]）以及紧随的下一个元素的开头 "（如果有）
                // Skip sep (, or ]) and the opening quote of the next element (if any).
                uint advance = afterQuote + 1; // +1 跳过 sep 本身 / +1 to skip sep itself
                if (advance < source->UsageSize && *source->Index((int)advance) == '"')
                    advance++; // 跳过下一个元素的开头引号 / skip next element's opening quote
                *source = source->Slice(advance);
            }
            else
            {
                *source = source->Slice(source->UsageSize); // 格式异常，终止 / malformed, stop
            }
        }
        else
        {
            // afterQuote 之后没有更多字符（或只剩一个 ]），source 清空
            // Nothing meaningful after the closing quote; clear source.
            *source = source->Slice(source->UsageSize);
        }

        DecodeJsonString(value);
    }


}