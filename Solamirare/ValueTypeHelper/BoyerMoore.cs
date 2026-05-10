using System.Runtime.CompilerServices;

namespace Solamirare;


public unsafe static partial class ValueTypeHelper
{

    private const int ALPHABET_SIZE = 256;


    /// <summary>
    /// 初始化用于 LastIndexOf 的正向坏字符跳跃表。
    /// </summary>
    /// <param name="pValue">搜索模式的起始指针</param>
    /// <param name="valueBytesLength">搜索模式的字节长度</param>
    /// <param name="skipTable">指向 256 个 int 的栈分配数组指针</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InitializeSkipTable(byte* pValue, int valueBytesLength, int* skipTable)
    {
        int defaultSkip = valueBytesLength;

        // 1. 初始化所有条目为默认跳跃长度
        for (int i = 0; i < ALPHABET_SIZE; i++)
        {
            skipTable[i] = defaultSkip;
        }

        // 2. 根据模式中的字符出现位置更新跳跃值
        // 从模式头部开始，直到倒数第二个字符
        for (int i = 0; i < valueBytesLength - 1; i++)
        {
            // 对于重复字符，保留最右边的出现位置的跳跃值
            skipTable[pValue[i]] = valueBytesLength - 1 - i;
        }
    }

    /// <summary>
    /// 初始化用于 LastIndexOf 的反向坏字符跳跃表。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InitializeReverseSkipTable(byte* pValue, int valueBytesLength, int* skipTable)
    {
        // 默认跳跃值为整个模式串长度
        int defaultSkip = valueBytesLength;

        for (int i = 0; i < ALPHABET_SIZE; i++)
        {
            skipTable[i] = defaultSkip;
        }

        // 从模式串末尾开始向头部扫描，直到第二个字符（索引 1）
        // 记录每个字符距离模式串头部的偏移量
        for (int i = valueBytesLength - 1; i > 0; i--)
        {
            // 越靠左的字符，其对应的跳跃值越小
            skipTable[pValue[i]] = i;
        }
    }

    // --- 1. Boyer-Moore/Horspool 算法 (长模式加速) ---
    /// <summary>
    /// Boyer-Moore/Horspool 算法实现
    /// </summary>
    internal static int IndexOf_BoyerMoore(byte* pSource, int sourceBytesLength, byte* pValue, int valueBytesLength, int sizeOfValue)
    {
        if (valueBytesLength > sourceBytesLength) return -1;

        int* skipTable = stackalloc int[ALPHABET_SIZE];
        InitializeSkipTable(pValue, valueBytesLength, skipTable);

        byte lastByte = *(pValue + valueBytesLength - 1);
        int i = valueBytesLength - 1;

        while (i < sourceBytesLength)
        {
            byte sourceChar = *(pSource + i);

            if (sourceChar == lastByte)
            {
                byte* pMatchStart = pSource + i - (valueBytesLength - 1);
                int j = 0;
                bool fullMatch = true;

                // 1. 逐级对比逻辑
                if (valueBytesLength >= 16)
                {
                    // 安全起见，只在长度确实够大时用 long 比较，或者直接改用内存比较函数
                    if (*(long*)pMatchStart != *(long*)pValue) fullMatch = false;
                    else j = 8; // 从 8 开始继续比较
                }

                if (fullMatch)
                {
                    for (; j < valueBytesLength; j++)
                    {
                        if (pMatchStart[j] != pValue[j])
                        {
                            fullMatch = false;
                            break;
                        }
                    }
                }

                if (fullMatch)
                {
                    return (i - (valueBytesLength - 1)) / sizeOfValue;
                }

                // 匹配失败后的跳转：Horspool 算法通常根据当前末尾字符跳转
                // 修正：确保此处跳转后，外面不再重复跳转
                i += skipTable[sourceChar];
            }
            else
            {
                // 不匹配时的跳转
                i += skipTable[sourceChar];
            }
        }
        return -1;
    }


    /// <summary>
    /// Boyer-Moore/Horspool 算法的 LastIndexOf 实现
    /// </summary>
    internal static int LastIndexOf_BoyerMoore(byte* pSource, int sourceBytesLength, byte* pValue, int valueBytesLength, int sizeOfValue)
    {
        if (valueBytesLength > sourceBytesLength) return -1;
        if (valueBytesLength == 0) return sourceBytesLength / sizeOfValue;

        // 1. 栈分配反向跳跃表
        int* skipTable = stackalloc int[ALPHABET_SIZE];
        InitializeReverseSkipTable(pValue, valueBytesLength, skipTable);

        byte firstByte = *pValue;
        // 从右往左扫描，初始位置在第一个可能的匹配起点
        int i = 0;
        int limit = sourceBytesLength - valueBytesLength;

        // 我们从最右侧开始
        int cursor = limit;

        while (cursor >= 0)
        {
            byte sourceChar = *(pSource + cursor);

            // 如果当前字节匹配模式串的首字节
            if (sourceChar == firstByte)
            {
                // 验证全量匹配
                bool fullMatch = true;

                // 快速验证：如果是长模式串，先对比末尾 8 字节（防止无效的逐字节扫描）
                if (valueBytesLength >= 16)
                {
                    if (*(long*)(pSource + cursor + valueBytesLength - 8) != *(long*)(pValue + valueBytesLength - 8))
                    {
                        fullMatch = false;
                    }
                }

                if (fullMatch)
                {
                    // 逐字节对比（由于首字节已匹配，从 1 开始）
                    for (int j = 1; j < valueBytesLength; j++)
                    {
                        if (pSource[cursor + j] != pValue[j])
                        {
                            fullMatch = false;
                            break;
                        }
                    }
                }

                if (fullMatch)
                {
                    // 命中！进行 sizeOfValue 对齐逻辑返回
                    return cursor / sizeOfValue;
                }

                // 匹配失败，根据首字节在模式串中的位置向左跳跃
                cursor -= skipTable[sourceChar];
            }
            else
            {
                // 不匹配，向左跳跃
                cursor -= skipTable[sourceChar];
            }
        }

        return -1;
    }
}