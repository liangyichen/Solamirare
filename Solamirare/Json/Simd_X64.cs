using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;


namespace Solamirare;


public unsafe partial struct JsonDocument
{

    // --------------------------------------------------------------------------------
    // SIMD 静态辅助方法和常量 (新增 X86 版本)
    // --------------------------------------------------------------------------------
    // 注意：fixed 关键字的使用要求方法是 unsafe

    // --- X86 Helpers (AVX2 / SSE2) ---

    // 辅助方法：创建 AVX2 (256位) 掩码向量 [0xXX, 0x00, 0xXX, 0x00, ...]
    private static Vector256<byte> CreateVector256MaskedTarget(byte targetAscii)
    {
        Span<byte> pattern = stackalloc byte[32]; // 32 字节 for Vector256
        for (int i = 0; i < pattern.Length; i++)
        {
            pattern[i] = (i % 2 == 0) ? targetAscii : (byte)0x00;
        }
        fixed (byte* p = pattern)
        {
            return Avx.LoadVector256(p);
        }
    }

    // 辅助方法：创建 SSE2 (128位) 掩码向量 [0xXX, 0x00, 0xXX, 0x00, ...]
    private static Vector128<byte> CreateVector128X86MaskedTarget(byte targetAscii)
    {
        Span<byte> pattern = stackalloc byte[16]; // 16 字节 for Vector128
        for (int i = 0; i < pattern.Length; i++)
        {
            pattern[i] = (i % 2 == 0) ? targetAscii : (byte)0x00;
        }
        fixed (byte* p = pattern)
        {
            return Sse2.LoadVector128(p);
        }
    }


    // --- 静态常量定义 (X86) ---

    // AVX2 (256 位) 常量
    private static readonly Vector256<byte> V_256_QUOTE_TARGET = Avx2.IsSupported ? CreateVector256MaskedTarget(0x22) : Vector256<byte>.Zero;
    private static readonly Vector256<byte> V_256_BACKSLASH_TARGET = Avx2.IsSupported ? CreateVector256MaskedTarget(0x5C) : Vector256<byte>.Zero;
    private static readonly Vector256<byte> V_256_LOW_BYTE_MASK = Avx2.IsSupported ? CreateVector256MaskedTarget(0xFF) : Vector256<byte>.Zero;

    // AVX2 256 位常量 (32 字节)
    // 修正：使用 Vector256.Create
    private static readonly Vector256<byte> V_256_WHITESPACE_SPACE = Avx2.IsSupported ? Vector256.Create((byte)0x20) : Vector256<byte>.Zero;
    private static readonly Vector256<byte> V_256_WHITESPACE_TAB = Avx2.IsSupported ? Vector256.Create((byte)0x09) : Vector256<byte>.Zero;
    private static readonly Vector256<byte> V_256_WHITESPACE_LF = Avx2.IsSupported ? Vector256.Create((byte)0x0A) : Vector256<byte>.Zero;
    private static readonly Vector256<byte> V_256_WHITESPACE_CR = Avx2.IsSupported ? Vector256.Create((byte)0x0D) : Vector256<byte>.Zero;


    // 用于 AdvSimd/SSE2 的 0xFF 掩码（这里使用 256 位，但通常在 AdvSimd 中使用 128 位）
    private static readonly Vector256<byte> V_256_HIGH_BYTE_MASK = Avx2.IsSupported ? Vector256.Create((byte)0xFF) : Vector256<byte>.Zero;





    // SSE2 (128 位) 常量
    private static readonly Vector128<byte> V_X86_QUOTE_TARGET = Sse2.IsSupported ? CreateVector128X86MaskedTarget(0x22) : Vector128<byte>.Zero;
    private static readonly Vector128<byte> V_X86_BACKSLASH_TARGET = Sse2.IsSupported ? CreateVector128X86MaskedTarget(0x5C) : Vector128<byte>.Zero;
    private static readonly Vector128<byte> V_X86_LOW_BYTE_MASK = Sse2.IsSupported ? CreateVector128X86MaskedTarget(0xFF) : Vector128<byte>.Zero;

    private static readonly Vector128<byte> V_X86_WHITESPACE_SPACE = Sse2.IsSupported ? Vector128.Create((byte)0x20) : Vector128<byte>.Zero;
    private static readonly Vector128<byte> V_X86_WHITESPACE_TAB = Sse2.IsSupported ? Vector128.Create((byte)0x09) : Vector128<byte>.Zero;
    private static readonly Vector128<byte> V_X86_WHITESPACE_LF = Sse2.IsSupported ? Vector128.Create((byte)0x0A) : Vector128<byte>.Zero;
    private static readonly Vector128<byte> V_X86_WHITESPACE_CR = Sse2.IsSupported ? Vector128.Create((byte)0x0D) : Vector128<byte>.Zero;

    private static readonly Vector128<byte> V_X86_HIGH_BYTE_MASK = Sse2.IsSupported ? Vector128.Create((byte)0xFF) : Vector128<byte>.Zero;





    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ParseResult ParseStringDataCopy_Avx2(UnManagedCollection<char>* json, int* index, uint contentStart)
    {
        char* jsonPtr = json->InternalPointer;
        char* currentPtr = jsonPtr + *index;
        char* endPtr = jsonPtr + json->Size;

        // AVX2 查找块：32 字节 / 16 字符
        while ((currentPtr + 32) <= endPtr)
        {
            // 1. SIMD 查找逻辑
            Vector256<byte> dataVector = Avx.LoadVector256((byte*)currentPtr);

            // 应用低字节掩码
            dataVector = Avx2.And(dataVector, V_256_LOW_BYTE_MASK);

            // 并行比较目标字符
            Vector256<byte> quoteMask = Avx2.CompareEqual(dataVector, V_256_QUOTE_TARGET);
            Vector256<byte> backslashMask = Avx2.CompareEqual(dataVector, V_256_BACKSLASH_TARGET);
            Vector256<byte> combinedMask = Avx2.Or(quoteMask, backslashMask);

            // 使用 MoveMask 获取位图
            int matchBitmask = Avx2.MoveMask(combinedMask);

            if (matchBitmask != 0)
            {
                // 找到匹配项，设置指针位置，退出 SIMD 循环
                *index = (int)(currentPtr - jsonPtr);
                break;
            }

            currentPtr += 32;
            *index += 16;
        }

        // SIMD 无法继续后，调用标量版本处理剩余部分（包括精确检查和终结）
        return ParseStringDataCopy_Scalar(json, index, contentStart);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ParseResult ParseStringDataCopy_Sse2(UnManagedCollection<char>* json, int* index, uint contentStart)
    {
        char* jsonPtr = json->InternalPointer;
        char* currentPtr = jsonPtr + *index;
        char* endPtr = jsonPtr + json->Size;

        // SSE2 查找块：16 字节 / 8 字符
        while ((currentPtr + 16) <= endPtr)
        {
            // 1. SIMD 查找逻辑
            Vector128<byte> dataVector = Sse2.LoadVector128((byte*)currentPtr);

            // 应用低字节掩码
            dataVector = Sse2.And(dataVector, V_X86_LOW_BYTE_MASK);

            // 并行比较目标字符
            Vector128<byte> quoteMask = Sse2.CompareEqual(dataVector, V_X86_QUOTE_TARGET);
            Vector128<byte> backslashMask = Sse2.CompareEqual(dataVector, V_X86_BACKSLASH_TARGET);
            Vector128<byte> combinedMask = Sse2.Or(quoteMask, backslashMask);

            // 使用 MoveMask 获取位图
            int matchBitmask = Sse2.MoveMask(combinedMask);

            if (matchBitmask != 0)
            {
                // 找到匹配项，设置指针位置，退出 SIMD 循环
                *index = (int)(currentPtr - jsonPtr);
                break;
            }

            currentPtr += 16;
            *index += 8;
        }

        // SIMD 无法继续后，调用标量版本处理剩余部分
        return ParseStringDataCopy_Scalar(json, index, contentStart);
    }



    // 【新增方法 1】: AVX2 字符串块压缩 (32 字节 / 16 字符)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryCompactStringBlock_Avx2(char* endPtr, ref char* readPtr, ref char* writePtr)
    {
        const int CHAR_COUNT = 16;
        const int BYTE_COUNT = 32;

        if (readPtr + CHAR_COUNT > endPtr) return false;

        char* currentReadPtr = readPtr;
        char* currentWritePtr = writePtr;

        // --- 1. SIMD Scan ---

        // 加载 32 字节数据
        Vector256<byte> dataVector = Avx.LoadVector256((byte*)currentReadPtr);

        // 应用低字节掩码 V_256_LOW_BYTE_MASK
        dataVector = Avx2.And(dataVector, V_256_LOW_BYTE_MASK);

        // 并行比较目标字符
        Vector256<byte> quoteMask = Avx2.CompareEqual(dataVector, V_256_QUOTE_TARGET);
        Vector256<byte> backslashMask = Avx2.CompareEqual(dataVector, V_256_BACKSLASH_TARGET);
        Vector256<byte> combinedMask = Avx2.Or(quoteMask, backslashMask);

        // 使用 MoveMask 获取位图
        int matchBitmask = Avx2.MoveMask(combinedMask);

        if (matchBitmask != 0)
        {
            // 找到匹配项，确定第一个匹配字符的索引
            int charIndex = BitOperations.TrailingZeroCount(matchBitmask) / 2;

            // 复制到匹配点之前
            if (charIndex > 0)
            {
                Avx.Store((byte*)currentWritePtr, Avx.LoadVector256((byte*)currentReadPtr)); // 批量复制 16 字符

                // 调整指针到恰好在匹配字符之前
                readPtr += charIndex;
                writePtr += charIndex;
            }
            // 如果 charIndex == 0，则第一个字符就是匹配项，指针不移动，返回 false 交给标量处理。

            return false; // 未处理完整个块，交由标量处理
        }

        // --- 2. Bulk Copy & Advance ---

        // 没有找到匹配项，批量复制整个块
        Avx.Store((byte*)currentWritePtr, Avx.LoadVector256((byte*)currentReadPtr));

        readPtr += CHAR_COUNT;
        writePtr += CHAR_COUNT;

        return true; // 成功处理整个块
    }



    // 【修复后的方法】: SSE2 字符串块压缩 (16 字节 / 8 字符)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryCompactStringBlock_Sse2(char* endPtr, ref char* readPtr, ref char* writePtr)
    {
        const int CHAR_COUNT = 8;
        const int BYTE_COUNT = 16;

        if (readPtr + CHAR_COUNT > endPtr) return false;

        char* currentReadPtr = readPtr;
        char* currentWritePtr = writePtr;

        // 1. SIMD Scan
        if (!Sse2.IsSupported) return false;

        Vector128<byte> dataVector = Sse2.LoadVector128((byte*)currentReadPtr);
        dataVector = Sse2.And(dataVector, V_X86_LOW_BYTE_MASK);

        Vector128<byte> quoteMask = Sse2.CompareEqual(dataVector, V_X86_QUOTE_TARGET);
        Vector128<byte> backslashMask = Sse2.CompareEqual(dataVector, V_X86_BACKSLASH_TARGET);
        Vector128<byte> combinedMask = Sse2.Or(quoteMask, backslashMask);

        // 使用 MoveMask 获取位图
        int matchBitmask = Sse2.MoveMask(combinedMask);

        if (matchBitmask != 0)
        {
            // 找到匹配项，确定第一个匹配字符的索引
            int charIndex = BitOperations.TrailingZeroCount(matchBitmask) / 2;

            // 复制到匹配点之前 (使用标量循环，因为 charIndex <= 7，保证安全)
            if (charIndex > 0)
            {
                for (int i = 0; i < charIndex; i++)
                {
                    currentWritePtr[i] = currentReadPtr[i];
                }
            }

            // 更新指针到第一个关键字符的位置
            readPtr += charIndex;
            writePtr += charIndex;

            return false; // 返回 false，让主循环处理这个关键字符
        }

        // 2. Bulk Copy & Advance (未找到关键字符，批量复制整个块)
        Sse2.Store((byte*)currentWritePtr, Sse2.LoadVector128((byte*)currentReadPtr));

        readPtr += CHAR_COUNT;
        writePtr += CHAR_COUNT;

        return true; // 成功处理整个块
    }


    // 【方法 B】：AVX2 版本 - AVX2 优化
    public static unsafe CompactResult CompactJson_Avx2(ReadOnlySpan<char> json, char* buffer)
    {
        fixed (char* p_json = json)
        {
            char* readPtr = p_json;
            char* endPtr = readPtr + json.Length;
            char* writePtr = buffer;

            bool isInString = false;
            bool isEscaping = false;
            char firstContentChar = '\0';
            char lastContentChar = '\0';

            while (readPtr < endPtr)
            {
                char currentChar = *readPtr;

                if (isInString)
                {
                    // 1. 【SIMD 字符串尝试】：AVX2 批量复制/查找
                    if (!isEscaping)
                    {
                        bool bulkProcessed = TryCompactStringBlock_Avx2(endPtr, ref readPtr, ref writePtr);

                        if (bulkProcessed)
                        {
                            continue; // SIMD 成功处理一个块，跳过 readPtr++
                        }

                        if (readPtr >= endPtr) break;
                        currentChar = *readPtr;
                    }

                    // 2. 标量接管/处理关键字符
                    *writePtr = currentChar;
                    writePtr++;

                    if (isEscaping)
                    {
                        isEscaping = false;
                    }
                    else if (currentChar == '\\')
                    {
                        isEscaping = true;
                    }
                    else if (currentChar == '"')
                    {
                        isInString = false;
                    }
                }
                else // 状态 B: 不在字符串内 (非字符串)
                {
                    // 【核心优化点】：SIMD 批量跳过空白符
                    bool skipped = false;
                    if (readPtr < endPtr)
                    {
                        skipped = TrySkipWhitespace_Avx2(endPtr, ref readPtr);
                    }

                    if (skipped)
                    {
                        continue; // 成功跳过一整块空白符
                    }

                    // 重新获取当前字符
                    if (readPtr >= endPtr) break;
                    currentChar = *readPtr;

                    // 1. 处理行注释 //
                    bool isComment = false;
                    if (currentChar == '/')
                    {
                        if (readPtr + 1 < endPtr && *(readPtr + 1) == '/')
                        {
                            readPtr += 2;
                            while (readPtr < endPtr && *readPtr != '\n' && *readPtr != '\r') readPtr++;
                            if (readPtr < endPtr) readPtr++;
                            isComment = true;
                        }
                    }
                    if (isComment) continue;

                    // 2. 处理空白符 (SIMD 未处理完的单个空白符)
                    if (currentChar == ' ' || currentChar == '\t' || currentChar == '\n' || currentChar == '\r')
                    {
                        // 跳过空白符
                    }
                    // 3. 处理所有内容字符
                    else
                    {
                        if (firstContentChar == '\0')
                        {
                            if (!(currentChar == '{' || currentChar == '[' || currentChar == '"' || currentChar == 't' || currentChar == 'f' || currentChar == 'n' || (currentChar >= '0' && currentChar <= '9') || currentChar == '-')) return CompactResult.Failure;
                            firstContentChar = currentChar;
                        }

                        if (currentChar == '"') isInString = true;

                        *writePtr = currentChar;
                        writePtr++;
                        lastContentChar = currentChar;
                    }
                }

                readPtr++; // 主循环推进
            }

            return FinalizeCompactJson(buffer, writePtr, isInString, firstContentChar, lastContentChar, json);
        }
    }


    public static unsafe CompactResult CompactJson_Sse2(ReadOnlySpan<char> json, char* buffer)
    {
        fixed (char* p_json = json)
        {
            char* readPtr = p_json;
            char* endPtr = readPtr + json.Length;
            char* writePtr = buffer;

            bool isInString = false;
            bool isEscaping = false;
            char firstContentChar = '\0';
            char lastContentChar = '\0';

            while (readPtr < endPtr)
            {
                char currentChar = *readPtr;

                if (isInString)
                {
                    // 1. 【SIMD 字符串尝试】：SSE2 批量复制/查找
                    if (!isEscaping)
                    {
                        bool bulkProcessed = TryCompactStringBlock_Sse2(endPtr, ref readPtr, ref writePtr);

                        if (bulkProcessed)
                        {
                            continue;
                        }

                        if (readPtr >= endPtr) break;
                        currentChar = *readPtr;
                    }

                    // 2. 标量接管/处理关键字符
                    *writePtr = currentChar;
                    writePtr++;

                    if (isEscaping)
                    {
                        isEscaping = false;
                    }
                    else if (currentChar == '\\')
                    {
                        isEscaping = true;
                    }
                    else if (currentChar == '"')
                    {
                        isInString = false;
                    }
                }
                else // 状态 B: 不在字符串内
                {
                    // 【核心优化点】：SIMD 批量跳过空白符
                    bool skipped = false;
                    if (readPtr < endPtr)
                    {
                        skipped = TrySkipWhitespace_Sse2(endPtr, ref readPtr);
                    }

                    if (skipped)
                    {
                        continue; // 成功跳过一整块空白符
                    }

                    // 重新获取当前字符
                    if (readPtr >= endPtr) break;
                    currentChar = *readPtr;

                    // 1. 处理行注释 //
                    bool isComment = false;
                    if (currentChar == '/')
                    {
                        if (readPtr + 1 < endPtr && *(readPtr + 1) == '/')
                        {
                            readPtr += 2;
                            while (readPtr < endPtr && *readPtr != '\n' && *readPtr != '\r') readPtr++;
                            if (readPtr < endPtr) readPtr++;
                            isComment = true;
                        }
                    }
                    if (isComment) continue;

                    // 2. 处理空白符 
                    if (currentChar == ' ' || currentChar == '\t' || currentChar == '\n' || currentChar == '\r')
                    {
                        // 跳过空白符
                    }
                    // 3. 处理所有内容字符
                    else
                    {
                        if (firstContentChar == '\0')
                        {
                            if (!(currentChar == '{' || currentChar == '[' || currentChar == '"' || currentChar == 't' || currentChar == 'f' || currentChar == 'n' || (currentChar >= '0' && currentChar <= '9') || currentChar == '-')) return CompactResult.Failure;
                            firstContentChar = currentChar;
                        }

                        if (currentChar == '"') isInString = true;

                        *writePtr = currentChar;
                        writePtr++;
                        lastContentChar = currentChar;
                    }
                }

                readPtr++;
            }

            return FinalizeCompactJson(buffer, writePtr, isInString, firstContentChar, lastContentChar, json);
        }
    }


    /// <summary>
    /// 修复后的 AVX2 批量跳过空白符。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TrySkipWhitespace_Avx2(char* endPtr, ref char* readPtr)
    {
        const int CHAR_COUNT = 16;
        if (readPtr + CHAR_COUNT > endPtr) return false;

        // 1. SIMD 比较
        Vector256<byte> dataVector = Avx.LoadVector256((byte*)readPtr);

        Vector256<byte> mask1 = Avx2.CompareEqual(dataVector, V_256_WHITESPACE_SPACE);
        Vector256<byte> mask2 = Avx2.CompareEqual(dataVector, V_256_WHITESPACE_TAB);
        Vector256<byte> mask3 = Avx2.CompareEqual(dataVector, V_256_WHITESPACE_LF);
        Vector256<byte> mask4 = Avx2.CompareEqual(dataVector, V_256_WHITESPACE_CR);

        Vector256<byte> whitespaceMask = Avx2.Or(Avx2.Or(mask1, mask2), Avx2.Or(mask3, mask4));

        // 找出第一个非空白符：通过 Xor 0xFF 反转掩码
        Vector256<byte> highByteMask = Vector256.Create((byte)0xFF);
        Vector256<byte> nonWhitespaceMask = Avx2.Xor(whitespaceMask, highByteMask);

        int nonWhitespaceBitmask = Avx2.MoveMask(nonWhitespaceMask);

        // 【CRITICAL FIX】：只保留偶数位（对应 char 的低字节）
        const int EVEN_BITS_MASK_32_BIT = unchecked((int)0x55555555); // 0101...0101
        nonWhitespaceBitmask &= EVEN_BITS_MASK_32_BIT;

        if (nonWhitespaceBitmask == 0)
        {
            // 整个块是空白符
            readPtr += CHAR_COUNT;
            return true;
        }

        // 找到第一个非空白符的位置 (现在 TZC 总是 0, 2, 4, ...)
        int charIndex = BitOperations.TrailingZeroCount(nonWhitespaceBitmask) / 2;

        readPtr += charIndex;
        return false;
    }



    /// <summary>
    /// 修复后的 SSE2 批量跳过空白符。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TrySkipWhitespace_Sse2(char* endPtr, ref char* readPtr)
    {
        const int CHAR_COUNT = 8;
        if (readPtr + CHAR_COUNT > endPtr) return false;

        // 1. SIMD 比较
        Vector128<byte> dataVector = Sse2.LoadVector128((byte*)readPtr);

        Vector128<byte> mask1 = Sse2.CompareEqual(dataVector, V_X86_WHITESPACE_SPACE);
        Vector128<byte> mask2 = Sse2.CompareEqual(dataVector, V_X86_WHITESPACE_TAB);
        Vector128<byte> mask3 = Sse2.CompareEqual(dataVector, V_X86_WHITESPACE_LF);
        Vector128<byte> mask4 = Sse2.CompareEqual(dataVector, V_X86_WHITESPACE_CR);

        Vector128<byte> whitespaceMask = Sse2.Or(Sse2.Or(mask1, mask2), Sse2.Or(mask3, mask4));

        // 找出第一个非空白符：通过 Xor 0xFF 反转掩码
        Vector128<byte> highByteMask = Vector128.Create((byte)0xFF);
        Vector128<byte> nonWhitespaceMask = Sse2.Xor(whitespaceMask, highByteMask);

        int nonWhitespaceBitmask = Sse2.MoveMask(nonWhitespaceMask);

        // 【CRITICAL FIX】：只保留偶数位（对应 char 的低字节）
        const int EVEN_BITS_MASK_16_BIT = 0x5555; // 0101010101010101
        nonWhitespaceBitmask &= EVEN_BITS_MASK_16_BIT;

        if (nonWhitespaceBitmask == 0)
        {
            readPtr += CHAR_COUNT;
            return true;
        }

        // 找到第一个非空白符的位置
        int charIndex = BitOperations.TrailingZeroCount(nonWhitespaceBitmask) / 2;

        readPtr += charIndex;
        return false;
    }



}