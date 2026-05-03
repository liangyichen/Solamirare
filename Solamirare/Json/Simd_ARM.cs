namespace Solamirare;

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

using System.Runtime.Intrinsics.Arm;



public unsafe partial struct JsonDocument
{

    // 辅助方法：动态生成带掩码的向量 [0xXX, 0x00, 0xXX, 0x00, ...]
    // 用于 Neon (128位)
    private static Vector128<byte> CreateVector128ArmMaskedTarget(byte targetAscii)
    {
        // 16 字节 for Vector128
        Span<byte> pattern = stackalloc byte[16];
        for (int i = 0; i < pattern.Length; i++)
        {
            pattern[i] = (i % 2 == 0) ? targetAscii : (byte)0x00;
        }
        // Neon 使用 LoadVector128
        fixed (byte* p = pattern)
        {
            return AdvSimd.LoadVector128(p);
        }
    }

    // 静态常量 (128 位 Neon 版本)
    private static readonly Vector128<byte> V_ARM_QUOTE_TARGET = AdvSimd.IsSupported ? CreateVector128ArmMaskedTarget(0x22) : Vector128<byte>.Zero;
    private static readonly Vector128<byte> V_ARM_BACKSLASH_TARGET = AdvSimd.IsSupported ? CreateVector128ArmMaskedTarget(0x5C) : Vector128<byte>.Zero;
    private static readonly Vector128<byte> V_ARM_LOW_BYTE_MASK = AdvSimd.IsSupported ? CreateVector128ArmMaskedTarget(0xFF) : Vector128<byte>.Zero;





    // ARM Neon 128 位常量 (16 字节)
    private static readonly Vector128<byte> V_ARM_WHITESPACE_SPACE = AdvSimd.IsSupported ? Vector128.Create((byte)0x20) : Vector128<byte>.Zero;
    private static readonly Vector128<byte> V_ARM_WHITESPACE_TAB = AdvSimd.IsSupported ? Vector128.Create((byte)0x09) : Vector128<byte>.Zero;
    private static readonly Vector128<byte> V_ARM_WHITESPACE_LF = AdvSimd.IsSupported ? Vector128.Create((byte)0x0A) : Vector128<byte>.Zero;
    private static readonly Vector128<byte> V_ARM_WHITESPACE_CR = AdvSimd.IsSupported ? Vector128.Create((byte)0x0D) : Vector128<byte>.Zero;


    // 【核心修复】：将 AdvSimd.SetAllVector128 替换为 Vector128.Create
    private static readonly Vector128<byte> V_ARM_HIGH_BYTE_MASK = AdvSimd.IsSupported ? Vector128.Create((byte)0xFF) : Vector128<byte>.Zero;







    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ParseResult ParseStringDataCopy_ArmNeon(UnManagedCollection<char>* json, int* index, uint contentStart)
    {
        char* jsonPtr = json->InternalPointer;
        char* currentPtr = jsonPtr + *index;
        char* endPtr = jsonPtr + json->Size;

        // Neon 查找块：16 字节 / 8 字符
        while ((currentPtr + 16) <= endPtr)
        {
            // 1. SIMD 查找逻辑
            Vector128<byte> dataVector = AdvSimd.LoadVector128((byte*)currentPtr);
            dataVector = AdvSimd.And(dataVector, V_ARM_LOW_BYTE_MASK);
            Vector128<byte> quoteMask = AdvSimd.CompareEqual(dataVector, V_ARM_QUOTE_TARGET);
            Vector128<byte> backslashMask = AdvSimd.CompareEqual(dataVector, V_ARM_BACKSLASH_TARGET);
            Vector128<byte> combinedMask = AdvSimd.Or(quoteMask, backslashMask);

            // 检查是否有任何匹配项 (M1/Neon)
            if (Vector128.AsUInt64(combinedMask).ToScalar() != 0 || Vector128.AsUInt64(combinedMask).GetElement(1) != 0)
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

    //ARM Neon 字符串块压缩 (16 字节 / 8 字符)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryCompactStringBlock_Neon(char* endPtr, ref char* readPtr, ref char* writePtr)
    {
        const int CHAR_COUNT = 8;
        const int BYTE_COUNT = 16;

        if (readPtr + CHAR_COUNT > endPtr) return false;

        char* currentReadPtr = readPtr;
        char* currentWritePtr = writePtr;

        // 1. SIMD Scan
        if (!AdvSimd.IsSupported) return false;

        Vector128<byte> dataVector = AdvSimd.LoadVector128((byte*)currentReadPtr);
        dataVector = AdvSimd.And(dataVector, V_ARM_LOW_BYTE_MASK);

        Vector128<byte> quoteMask = AdvSimd.CompareEqual(dataVector, V_ARM_QUOTE_TARGET);
        Vector128<byte> backslashMask = AdvSimd.CompareEqual(dataVector, V_ARM_BACKSLASH_TARGET);
        Vector128<byte> combinedMask = AdvSimd.Or(quoteMask, backslashMask);

        // 检查是否有任何匹配项 (M1/Neon)
        if (Vector128.AsUInt64(combinedMask).ToScalar() != 0 || Vector128.AsUInt64(combinedMask).GetElement(1) != 0)
        {
            // 找到匹配项：由于 Neon 没有 MoveMask，我们必须使用标量查找第一个匹配索引
            int charIndex = 0;
            for (int i = 0; i < CHAR_COUNT; i++)
            {
                char c = currentReadPtr[i];
                if (c == '"' || c == '\\')
                {
                    charIndex = i;
                    break;
                }
            }

            // 复制到匹配点之前
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
        AdvSimd.Store((byte*)currentWritePtr, AdvSimd.LoadVector128((byte*)currentReadPtr));

        readPtr += CHAR_COUNT;
        writePtr += CHAR_COUNT;

        return true; // 成功处理整个块
    }



    public static unsafe CompactResult CompactJson_Neon(ReadOnlySpan<char> json, char* buffer)
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
                    // 1. 【SIMD 字符串尝试】：Neon 批量复制/查找
                    if (!isEscaping)
                    {
                        bool bulkProcessed = TryCompactStringBlock_Neon(endPtr, ref readPtr, ref writePtr);

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
                        skipped = SkipWhitespace(endPtr, ref readPtr);
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
    /// 批量跳过空白符。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool SkipWhitespace(char* endPtr, ref char* readPtr)
    {
        // 对于 CompactJson，我们尝试跳过至少 8 个字符，如果失败则让主循环处理。
        const int CHAR_COUNT = 8;
        if (readPtr + CHAR_COUNT > endPtr) return false;

        char* currentPtr = readPtr;
        char* originalPtr = readPtr;


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


        // 如果 8 个字符都是空白符
        readPtr += CHAR_COUNT;
        return true;


    FoundNonWhitespace:
        // 如果找到了非空白符
        if (currentPtr > originalPtr)
        {
            // 至少跳过了一个字符
            readPtr = currentPtr;
            return true; // 报告成功跳过 (即使只跳过了一个字符)
        }

        // 否则，第一个字符就是非空白符
        return false; // 返回 false，让 CompactJson_Neon 主循环处理
    }





}