using System.Runtime.CompilerServices;

namespace Solamirare;

/// <summary>
/// 提供 JSON 紧凑化功能。
/// </summary>
public unsafe partial struct JsonDocument
{
    /// <summary>
    /// 压缩原始 JSON 字符串，去除无意义空白与注释。
    /// </summary>
    /// <param name="json">待压缩的 JSON 字符串。</param>
    /// <param name="buffer">输出缓冲区。</param>
    public static unsafe CompactResult CompactJson(ReadOnlySpan<char> json, char* buffer)
    {
        if (System.Runtime.Intrinsics.X86.Avx2.IsSupported)
        {
            return CompactJson_Avx2(json, buffer);
        }
        else if (System.Runtime.Intrinsics.X86.Sse2.IsSupported)
        {
            return CompactJson_Sse2(json, buffer);
        }
        else if (System.Runtime.Intrinsics.Arm.AdvSimd.IsSupported)
        {
            return CompactJson_Neon(json, buffer);
        }
        else
            return CompactJson_Scalar(json, buffer);
    }

    /// <summary>
    /// 使用标量实现压缩 JSON 字符串。
    /// </summary>
    /// <param name="json">待压缩的 JSON 字符串。</param>
    /// <param name="buffer">输出缓冲区。</param>
    public static unsafe CompactResult CompactJson_Scalar(ReadOnlySpan<char> json, char* buffer)
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
                else
                {
                    bool isComment = false;

                    if (currentChar == '/')
                    {
                        if (readPtr + 1 < endPtr && *(readPtr + 1) == '/')
                        {
                            readPtr += 2;
                            while (readPtr < endPtr && *readPtr != '\n' && *readPtr != '\r')
                            {
                                readPtr++;
                            }
                            if (readPtr < endPtr) readPtr++;
                            isComment = true;
                        }
                    }

                    if (isComment)
                    {
                        continue;
                    }

                    if (currentChar == ' ' || currentChar == '\t' || currentChar == '\n' || currentChar == '\r')
                    {
                    }
                    else
                    {
                        if (firstContentChar == '\0')
                        {
                            if (!(currentChar == '{' || currentChar == '[' ||
                                currentChar == '"' ||
                                currentChar == 't' || currentChar == 'f' || currentChar == 'n' ||
                                (currentChar >= '0' && currentChar <= '9') || currentChar == '-'))
                            {
                                return CompactResult.Failure;
                            }
                            firstContentChar = currentChar;
                        }

                        if (currentChar == '"')
                        {
                            isInString = true;
                        }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static CompactResult FinalizeCompactJson(char* buffer, char* writePtr, bool isInString, char firstContentChar, char lastContentChar, ReadOnlySpan<char> json)
    {
        nuint usageSize = (nuint)(writePtr - buffer);

        if (isInString) return CompactResult.Failure;

        if (usageSize == 0 && json.Length > 0 && firstContentChar != '\0')
        {
            return CompactResult.Failure;
        }

        if (firstContentChar == '{' && lastContentChar != '}') return CompactResult.Failure;
        if (firstContentChar == '[' && lastContentChar != ']') return CompactResult.Failure;

        var mirror = new UnManagedCollection<char>(buffer, (uint)usageSize);
        return CompactResult.Success(mirror);
    }
}
