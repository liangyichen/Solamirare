using System.Numerics;
using System.Runtime.CompilerServices;
namespace Solamirare;



/// <summary>
/// 平面 json 处理器， json 字符串类型仅限平面类型，不能有深度嵌套
/// <para>合法例子： {"name":"my name", "age":100} </para>
/// <para>非法例子： {"name":"my name", "age":100, “pet”:{"name":"wowo"}} </para>
/// </summary>
public static unsafe class JsonFlatProcessor
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

    static JsonFlatProcessor()
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
    /// <param name="memoryPoolUseForKeyValues"></param>
    /// <returns></returns>
    public static UnManagedString DecodeJsonString(ReadOnlySpan<char> input, MemoryPoolCluster* memoryPoolUseForKeyValues = null)
    {
        UnManagedString un_input = new UnManagedString(input, memoryPoolUseForKeyValues);

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
        if (input is null || input->IsEmpty) return;

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
    /// 反序列化对象模式字符串，把解析结果追加到 ValueDictionary
    /// </summary>
    /// <param name="source">json字符串</param>
    /// <param name="result_dictionary">用于存储结果的字典</param>
    /// <param name="memoryPoolUseForKeyValues">用于保存键值对指向内存段的内存池</param>
    /// <returns></returns>
    public static bool DecodeObjectString_AppendToDictionary(ReadOnlySpan<char> source, ValueDictionary<UnManagedString, UnManagedString>* result_dictionary, MemoryPoolCluster* memoryPoolUseForKeyValues = null)
    {

        if (source.IsEmpty || result_dictionary is null) return false;


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

        bool result = DecodeObjectString_AppendToDictionary(&json, result_dictionary, memoryPoolUseForKeyValues);

        json.Dispose();

        return result;

    }



    /// <summary>
    /// 反序列化对象模式字符串，把解析结果追加到 ValueDictionary
    /// </summary>
    /// <param name="source">json字符串</param>
    /// <param name="result_dictionary">用于存储结果的字典</param>
    /// <param name="memoryPoolUseForKeyValues">用于保存键值对指向内存段的内存池</param>
    /// <returns></returns>
    public static bool DecodeObjectString_AppendToDictionary(UnManagedString* source, ValueDictionary<UnManagedString, UnManagedString>* result_dictionary, MemoryPoolCluster* memoryPoolUseForKeyValues = null)
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
            UnManagedMemory<char> decode_key = DecodeJsonString(keySpan, memoryPoolUseForKeyValues);

            ReadOnlySpan<char> valueSpan;
            if (json_type == JsonSerializeTypes.String)
                valueSpan = new ReadOnlySpan<char>(valueStart + 1, (int)(valueEnd - valueStart - 1));
            else
                valueSpan = new ReadOnlySpan<char>(valueStart, (int)(valueEnd - valueStart + 1));

            UnManagedMemory<char> decode_value = DecodeJsonString(valueSpan, memoryPoolUseForKeyValues);
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


        //默认逻辑： key 和 value 两个参数都是字符串，默认会有双引号。 除非已经特别注明不属于 SerializeTypes.String
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
    /// 构建集合JSON
    /// </summary>
    /// <param name="source"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    public static SerializeResult SerializeCollection(ValueLinkedList<UnManagedString>* source, UnManagedString* output)
    {
        //需要一个基于字符串的节点描述

        if (source is null || output is null || source->IsEmpty || source->NodesCount <= 0) { return new SerializeResult { Status = SerializeResultEnum.Null_Or_Empty }; }


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

            //默认逻辑： key 和 value 两个参数都是字符串，默认会有双引号。 除非已经特别注明不属于 SerializeTypes.String
            bool nativeString = source_item->SerializeType != JsonSerializeTypes.String;

            SerializeResult process_item = SerializeString(source_item, output, ',', nativeString, true);

            if (!process_item.Success)
                return process_item;
        }


        output->SetValue(output->UsageSize - 1, ']');

        return new SerializeResult { Status = SerializeResultEnum.OK };
    }




    /// <summary>
    /// 构建集合JSON
    /// </summary>
    /// <param name="source"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    public static SerializeResult SerializeCollection(UnManagedMemory<UnManagedString>* source, UnManagedString* output)
    {
        //需要一个基于字符串的节点描述

        if (source is null || output is null || source->IsEmpty || source->UsageSize <= 0) { return new SerializeResult { Status = SerializeResultEnum.Null_Or_Empty }; }

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

            //默认逻辑： key 和 value 两个参数都是字符串，默认会有双引号。 除非已经特别注明不属于 SerializeTypes.String
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

        if (source is null || output is null || source->IsEmpty || source->UsageSize <= 0) { return new SerializeResult { Status = SerializeResultEnum.Null_Or_Empty }; }

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

            UnManagedString source_item = default; // 明确初始化，防止 default 分支走漏时传入垃圾指针

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
        if (text is null || output is null) return new SerializeResult { Status = SerializeResultEnum.Null_Or_Empty };

        // SIMD 加速的两遍扫描 (Two-Pass)
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
    /// <param name="memoryPool"></param>
    /// <returns></returns>
    public static UnManagedString SerializeString(UnManagedString* text, MemoryPoolCluster* memoryPool = null)
    {
        if (text is null) return UnManagedString.Empty;

        UnManagedString unm_string = new UnManagedString(memoryPool);

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
        if (json is null || result is null) return 0;

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
        if (source is null || value is null) return;

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