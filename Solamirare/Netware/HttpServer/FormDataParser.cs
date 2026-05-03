using Solamirare;
using System.Text;
using System.Numerics;
using System.Runtime.Intrinsics;


/// <summary>
/// Provides URL-encoding decode helpers for query strings and form payloads.
/// </summary>
public static unsafe class UrlEncodeAndDecoder
{


    /// <summary>
    /// URL 解码器，专门用于解析 application/x-www-form-urlencoded 或查询字符串。
    /// 此版本遵循传统解析模式：如果出现重复的键，则保留最后出现的值。
    /// 实现了以下增强功能：
    /// 1. 非法百分号编码检查。
    /// 2. 输入边界条件检查。
    /// 3. 空键和空值处理逻辑。
    /// 4. 解析和存储逻辑分离。
    /// 5. '+' 字符处理的控制。
    /// <para>result参数只需是一个分配好本身的内存段即可，不需要已经通过 new 分配，方法内部会进行 new 分配</para>
    /// <para>符号分析逻辑复杂，不可以使用 ReadonlySpan&lt;char&gt;.Split 来简单代替</para>
    /// </summary>
    /// <param name="encodedData">待解析的 URL 编码字节数据。</param>
    /// <param name="result">外部传入用于保存结果的容器</param>
    /// <param name="onMemoryPool"></param>
    public static void Decode(
        UnManagedCollection<byte>* encodedData, ValueDictionary<UnManagedMemory<byte>, UnManagedMemory<byte>>* result, bool onMemoryPool = false)
    {
        if (result is null || encodedData is null || encodedData->IsEmpty)
        {
            return;
        }
        
        
        uint current = 0;

        byte* ptr = encodedData->InternalPointer;
        
        uint totalSize = encodedData->Size;


        // 遍历输入数据，以 '&' 作为分隔符
        while (current < totalSize)
        {
            // 找到下一个 '&' 或到达末尾
            int nextAmpersand = -1;
            int remaining = (int)(totalSize - current);
            byte* searchStart = ptr + current;
            int offset = 0;

            // SIMD Search for '&'
            if (Vector256.IsHardwareAccelerated && remaining >= 32)
            {
                Vector256<byte> vAmp = Vector256.Create((byte)'&');
                for (; offset <= remaining - 32; offset += 32)
                {
                    uint mask = Vector256.Equals(Vector256.Load(searchStart + offset), vAmp).ExtractMostSignificantBits();
                    if (mask != 0)
                    {
                        nextAmpersand = offset + BitOperations.TrailingZeroCount(mask);
                        goto FoundAmp;
                    }
                }
            }
            // Fallback scalar
            for (; offset < remaining; offset++)
            {
                if (searchStart[offset] == (byte)'&')
                {
                    nextAmpersand = offset;
                    goto FoundAmp;
                }
            }

        FoundAmp:
            if (nextAmpersand == -1)
            {
                // End of string
                nextAmpersand = remaining;
            }

            // 当前键值对的 Span
            ReadOnlySpan<byte> pairSpan = encodedData->Slice(current, (uint)nextAmpersand);

            current += (uint)nextAmpersand + 1; // 移动到下一个位置

            // 3. 空键和空值的处理逻辑 (处理 '&&' 或末尾 '&')
            if (pairSpan.IsEmpty)
            {
                continue;
            }

            int equalsIndex = pairSpan.IndexOf((byte)'=');

            ReadOnlySpan<byte> keyEncoded, valueEncoded;

            if (equalsIndex == -1)
            {
                // 如果没有 '=' 符号，则整个 pairSpan 是 key，值为空字符串（例如 "key_only"）
                keyEncoded = pairSpan;
                valueEncoded = ReadOnlySpan<byte>.Empty;
            }
            else
            {
                // 3. 空键和空值的处理逻辑 (处理 '=' 开头或结尾，例如 "=value" 或 "key=")
                keyEncoded = pairSpan.Slice(0, equalsIndex);
                valueEncoded = pairSpan.Slice(equalsIndex + 1);
            }

            // 4. 分离解析和存储：调用核心解码方法
            UnManagedMemory<byte> key = DecodeSegment(keyEncoded, true);
            UnManagedMemory<byte> value = DecodeSegment(valueEncoded, true);

            
            result->Add(key, value);
        }

    }

    /// <summary>
    /// 核心解码方法：将单个 URL 编码片段解码为 UTF-8 字节序列。
    /// </summary>
    /// <param name="encodedSegment">URL 编码的字节片段。</param>
    /// <param name="treatPlusAsSpace">是否将 '+' 转换为空格。</param>
    /// <returns>解码后的字节内存。</returns>
    /// <exception cref="FormatException">如果遇到格式错误的百分号编码，则抛出。</exception>
    static UnManagedMemory<byte> DecodeSegment(ReadOnlySpan<byte> encodedSegment, bool treatPlusAsSpace)
    {
        if (encodedSegment.IsEmpty)
        {
            return UnManagedMemory<byte>.Empty;
        }

        // 预估输出缓冲区大小（不会超过原始输入长度）
        int outputLength = encodedSegment.Length;

        UnManagedMemory<byte> decodeBuffer = new UnManagedMemory<byte>((uint)outputLength, (uint)outputLength);
        
        uint writeIndex = 0;

        for (int i = 0; i < encodedSegment.Length; i++)
        {
            byte b = encodedSegment[i];

            if (b == (byte)'%')
            {
                // 1. 非法百分号编码检查: 确保后面有 2 个有效的十六进制字符
                if (i + 2 >= encodedSegment.Length ||
                    !IsHex(encodedSegment[i + 1]) ||
                    !IsHex(encodedSegment[i + 2]))
                {
                    throw new FormatException($"输入包含格式错误的百分号编码，位置：{i}。期待 '%HH' 格式。");
                }

                // 提取并解码两个十六进制字符
                int high = HexToNibble(encodedSegment[i + 1]);
                int low = HexToNibble(encodedSegment[i + 2]);

                decodeBuffer.SetValue(writeIndex++, (byte)((high << 4) | low));
                i += 2; // 跳过已处理的两个十六进制字符
            }
            // 5. 区分查询字符串和表单体（Body）的 + 处理
            else if (treatPlusAsSpace && b == (byte)'+')
            {
                decodeBuffer.SetValue(writeIndex++, (byte)' ');
            }
            else
            {
                // 其他字符直接复制
                decodeBuffer.SetValue(writeIndex++, b);
            }
        }
        
        decodeBuffer.ReLength(writeIndex);
        
        return decodeBuffer;
    }


    /// <summary>
    /// 检查字节是否为有效的十六进制字符 (0-9, a-f, A-F)。
    /// </summary>
    private static bool IsHex(byte b)
    {
        return (b >= '0' && b <= '9') ||
               (b >= 'a' && b <= 'f') ||
               (b >= 'A' && b <= 'F');
    }

    /// <summary>
    /// 将十六进制字节转换为 4 位数字。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">如果输入不是有效的十六进制字符。</exception>
    private static int HexToNibble(byte b)
    {
        if (b >= '0' && b <= '9') return b - '0';
        if (b >= 'a' && b <= 'f') return b - 'a' + 10;
        if (b >= 'A' && b <= 'F') return b - 'A' + 10;

        throw new ArgumentOutOfRangeException(nameof(b), $"无效的十六进制字符: {b}");
    }


    //========= 以下是解码为 char ============

    /// <summary>
    /// 将单个 URL 编码片段（来自 Query 或 Form 的零拷贝切片）解码为 UTF-8 字符序列，
    /// 返回堆分配的 <c>UnManagedMemory&lt;char&gt;</c>，由调用方负责释放。
    /// <br/>
    /// 解码分两步：
    ///   1. 将 %XX 转义序列还原为原始 UTF-8 字节，'+' 还原为空格；
    ///   2. 用 <see cref="System.Text.Encoding.UTF8"/> 将字节序列转换为 char 序列。
    /// <para>
    /// Decodes a single URL-encoded segment (a zero-copy slice from Query or Form) into a UTF-8
    /// character sequence, returning a heap-allocated <c>UnManagedMemory&lt;char&gt;</c> whose
    /// lifetime is managed by the caller.
    /// Decoding proceeds in two steps:
    ///   1. Unescape %XX sequences back to raw UTF-8 bytes, converting '+' to space;
    ///   2. Transcode the byte sequence to chars via <see cref="System.Text.Encoding.UTF8"/>.
    /// </para>
    /// </summary>
    /// <param name="encoded">
    /// URL 编码的原始字节片段，例如 <c>%E4%B8%AD%E6%96%87</c> 或 <c>hello+world</c>。
    /// <br/>
    /// URL-encoded raw byte segment, e.g. <c>%E4%B8%AD%E6%96%87</c> or <c>hello+world</c>.
    /// </param>
    /// <param name="treatPlusAsSpace">
    /// 是否将 '+' 解码为空格（表单体传 true，纯 Query 字符串传 false）。
    /// <br/>
    /// Whether to decode '+' as a space (pass true for form bodies, false for pure query strings).
    /// </param>
    /// <returns>
    /// 解码后的字符内存；若输入为空则返回 <c>UnManagedMemory&lt;char&gt;.Empty</c>。
    /// <br/>
    /// Decoded character memory; returns <c>UnManagedMemory&lt;char&gt;.Empty</c> if input is empty.
    /// </returns>
    public static UnManagedString DecodeToChars(UnManagedCollection<byte>* encoded, bool treatPlusAsSpace = true)
    {
        if (encoded is null || encoded->IsEmpty)
            return UnManagedString.Empty;

        // ── 第一步：%XX 反转义 → 原始 UTF-8 字节 ────────────────────────────
        // Step 1: unescape %XX sequences back to raw UTF-8 bytes.
        //
        // 解码后字节数 ≤ 原始字节数（%XX 三字节变一字节，其余不变），直接以原始长度作为上限分配栈缓冲。
        // Decoded byte count <= input length (%XX collapses 3 bytes to 1, others unchanged),
        // so the input length is a safe upper bound for the stack buffer.
        int inputLen = (int)encoded->Size;
        byte* src    = encoded->InternalPointer;

        // 使用栈缓冲区存放解码后的 UTF-8 字节，避免堆分配（上限 4096 字节走栈，超过则堆分配）
        // Use a stack buffer for the decoded UTF-8 bytes to avoid heap allocation;
        // fall back to heap allocation when the input exceeds 4096 bytes.
        const int StackLimit = 4096;

        byte* utf8Buf;
        UnManagedMemory<byte> heapBuf = default; // 仅在超出栈限制时使用 / only used when exceeding stack limit

        if (inputLen <= StackLimit)
        {
            byte* stackBuf = stackalloc byte[inputLen];
            utf8Buf = stackBuf;
        }
        else
        {
            heapBuf = new UnManagedMemory<byte>((uint)inputLen, (uint)inputLen);
            utf8Buf = heapBuf.Pointer;
        }

        int utf8Len = 0;

        for (int i = 0; i < inputLen; i++)
        {
            byte b = src[i];

            if (b == (byte)'%')
            {
                // 非法编码检查：后续必须有两个合法十六进制字符
                // Malformed encoding guard: must be followed by two valid hex digits
                if (i + 2 >= inputLen || !IsHex(src[i + 1]) || !IsHex(src[i + 2]))
                    throw new FormatException($"URL 编码格式错误，位置：{i}。期待 '%HH' 格式。");

                utf8Buf[utf8Len++] = (byte)((HexToNibble(src[i + 1]) << 4) | HexToNibble(src[i + 2]));
                i += 2; // 跳过已处理的两个十六进制字符 / skip the two consumed hex digits
            }
            else if (treatPlusAsSpace && b == (byte)'+')
            {
                utf8Buf[utf8Len++] = (byte)' ';
            }
            else
            {
                utf8Buf[utf8Len++] = b;
            }
        }

        // ── 第二步：UTF-8 字节 → char ────────────────────────────────────────
        // Step 2: transcode UTF-8 bytes to chars.
        //
        // 预先计算所需 char 数量，一次性分配，避免二次扩容。
        // Pre-calculate the required char count for a single allocation, avoiding reallocation.
        ReadOnlySpan<byte> utf8Span = new ReadOnlySpan<byte>(utf8Buf, utf8Len);
        int charCount = Encoding.UTF8.GetCharCount(utf8Span);

        if (charCount == 0)
        {
            if (!heapBuf.IsEmpty) heapBuf.Dispose();
            return UnManagedString.Empty;
        }

        UnManagedString result = new UnManagedString((uint)charCount, (uint)charCount);

        Encoding.UTF8.GetChars(utf8Span, result.AsSpan());

        // 释放超出栈限制时的临时堆缓冲
        // Release the temporary heap buffer used when input exceeded the stack limit
        if (!heapBuf.IsEmpty) heapBuf.Dispose();

        return result;
    }


    //========= 以下是编码 ============



    /// <summary>
    /// 计算单个片段经过 URL 编码后所需的精确字符长度。
    /// 此函数使用 stackalloc 实现零堆分配。
    /// </summary>
    private static int CalculateEncodedLength(UnManagedString* segmentSpan, bool usePlusForSpace)
    {
        if (segmentSpan is null || segmentSpan->IsEmpty)
            return 0;

        // 使用 stackalloc byte[4] 来保证零堆分配
        Span<byte> utf8Bytes = stackalloc byte[4];

        int requiredLength = 0;
        int current = 0;

        while (current < segmentSpan->UsageSize)
        {
            // 1. 判断当前字符是否是代理对，确定 char 单元的数量 (1 或 2)
            int charUnits = 1;
            if (current + 1 < segmentSpan->UsageSize && char.IsHighSurrogate(*segmentSpan->Index(current)))
            {
                if (char.IsLowSurrogate(*segmentSpan->Index(current + 1)))
                {
                    charUnits = 2;
                }
            }

            ReadOnlySpan<char> slice = segmentSpan->Slice((uint)current, (uint)charUnits);

            // 2. 将当前 char(s) 转换为 UTF-8 字节，写入栈上空间
            int byteCount = Encoding.UTF8.GetBytes(slice, utf8Bytes);

            // 3. 根据 UTF-8 字节计算编码后长度
            for (int b = 0; b < byteCount; b++)
            {
                byte currentByte = utf8Bytes[b];

                if (IsUnreserved(currentByte))
                {
                    requiredLength += 1; // 1 char
                }
                else if (currentByte == (byte)' ')
                {
                    requiredLength += usePlusForSpace ? 1 : 3; // '+' (1 char) 或 '%20' (3 chars)
                }
                else
                {
                    requiredLength += 3; // %HH (3 chars)
                }
            }

            // 4. 推进 current 索引
            current += charUnits;
        }

        return requiredLength;
    }




    /// <summary>
    /// 将键值对字典编码为 URL 编码的字符串。
    /// </summary>
    public static UnManagedString Encode(
        ValueDictionary<UnManagedString, UnManagedString>* data)
    {
        if (data is null || data->IsEmpty)
        {
            return UnManagedString.Empty;
        }

        //========== 预计算长度

        int totalLength = 0;

        data->ForEach(&loopToPreCalulateLength, &totalLength);

        totalLength += 1; //&

        if (totalLength == 1)
            return UnManagedString.Empty;

        //===========================================

        UnManagedString result = new UnManagedString((uint)totalLength, 0);

        data->ForEach(&loopUrlDictionary, &result);

        return result;
    }

    static bool loopToPreCalulateLength(int index, UnManagedString* key, UnManagedString* value, void* caller)
    {
        int* size = (int*)caller;

        int totalLength = *size;

        // 累加键的编码长度
        totalLength += CalculateEncodedLength(key, true);

        // 累加 '=' 和 '&'
        totalLength += 2;

        // 累加值的编码长度
        totalLength += CalculateEncodedLength(value, true);


        *size = totalLength;

        return true;
    }


    static bool loopUrlDictionary(int index, UnManagedString* key, UnManagedString* value, void* caller)
    {
        UnManagedString keyEncode = EncodeSegment(key, true);

        UnManagedString valueEncode = EncodeSegment(value, true);

        UnManagedString* result = (UnManagedString*)caller;

        if (!result->IsEmpty) //结果已经在这之前存在数据，说明已经保存有前置键值对，这时候需要先添加 '&'
        {
            result->Add('&');
        }

        result->AddRange(&keyEncode);

        result->Add('=');

        result->AddRange(&valueEncode);

        return true;
    }

    /// <summary>
    /// 核心编码方法：将单个字符串片段编码为 URL 编码格式。
    /// 采用“单缓冲区零分配”策略。
    /// </summary>
    static UnManagedString EncodeSegment(UnManagedString* segmentSpan, bool usePlusForSpace)
    {
        if (segmentSpan is null || segmentSpan->IsEmpty)
            return UnManagedString.Empty;


        // 临时栈上空间：用于存储当前字符的 UTF-8 字节（最多 4 字节，如 Emoji）
        const int MaxUtf8SizePerChar = 4;
        Span<byte> utf8Bytes = stackalloc byte[MaxUtf8SizePerChar];



        // --- 第一遍扫描：计算精确的最终 char 长度 ---

        uint requiredLength = (uint)CalculateEncodedLength(segmentSpan, true);

        if (requiredLength == 0) return UnManagedString.Empty;


        // 租用 char[] 缓冲区，执行最终写入
        UnManagedString rentedChars = new UnManagedString(requiredLength, requiredLength);

        Span<char> charSpan = rentedChars.AsSpan(0, requiredLength);
        int writeIndex = 0;

        // --- 第二遍扫描：写入最终的 URL 编码字符 ---
        int current = 0; // 重置 current 索引
        while (current < segmentSpan->UsageSize)
        {
            // 1. 判断当前字符是否是代理对，确定 char 单元的数量
            int charUnits = 1;
            if (current + 1 < segmentSpan->UsageSize && char.IsHighSurrogate(*segmentSpan->Index(current)))
            {
                if (char.IsLowSurrogate(*segmentSpan->Index(current + 1)))
                {
                    charUnits = 2;
                }
            }

            ReadOnlySpan<char> slice = segmentSpan->Slice((uint)current, (uint)charUnits);

            // 2. 将当前 char(s) 转换为 UTF-8 字节，写入栈上空间
            int byteCount = Encoding.UTF8.GetBytes(slice, utf8Bytes);

            // 3. 遍历 UTF-8 字节并进行编码
            for (int b = 0; b < byteCount; b++)
            {
                byte currentByte = utf8Bytes[b];

                if (IsUnreserved(currentByte))
                {
                    charSpan[writeIndex++] = (char)currentByte;
                }
                else if (currentByte == (byte)' ')
                {
                    if (usePlusForSpace)
                    {
                        charSpan[writeIndex++] = '+';
                    }
                    else
                    {
                        // %20
                        charSpan[writeIndex++] = '%';
                        charSpan[writeIndex++] = '2';
                        charSpan[writeIndex++] = '0';
                    }
                }
                else
                {
                    // 百分号编码: %HH
                    charSpan[writeIndex++] = '%';

                    byte highNibble = (byte)(currentByte >> 4);
                    byte lowNibble = (byte)(currentByte & 0xF);

                    charSpan[writeIndex++] = NibbleToHexChar(highNibble);
                    charSpan[writeIndex++] = NibbleToHexChar(lowNibble);
                }
            }

            // 4. 推进 current 索引
            current += charUnits;
        }


        return rentedChars;



    }

    /// <summary>
    /// 检查字节是否为 URL 编码规范中的未保留字符。
    /// </summary>
    private static bool IsUnreserved(byte b)
    {
        return (b >= (byte)'a' && b <= (byte)'z') ||
               (b >= (byte)'A' && b <= (byte)'Z') ||
               (b >= (byte)'0' && b <= (byte)'9') ||
               b == (byte)'-' ||
               b == (byte)'.' ||
               b == (byte)'_' ||
               b == (byte)'*';
    }

    /// <summary>
    /// 将 4 位数字转换为对应的十六进制字符 (0-9, A-F)。
    /// </summary>
    private static char NibbleToHexChar(byte nibble)
    {
        return (char)(nibble < 10 ? nibble + '0' : nibble - 10 + 'A');
    }

}
