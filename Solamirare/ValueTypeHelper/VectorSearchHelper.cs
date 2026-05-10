using System.Runtime.CompilerServices;

namespace Solamirare;


internal static unsafe class VectorSearchHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool IsMatch(byte* pSource, byte* pValue, int valueBytesLength, ulong pVal64, ulong mask)
    {
        // 1. 保持你最得意的 8 字节快速路径
        if (valueBytesLength <= 8)
        {
            return (*(ulong*)pSource & mask) == pVal64;
        }

        // 2. 这里的 for 循环建议替换为下面这一行
        // 理由：SequenceEqual 在底层会用 AVX2/NEON 一次比对 32 字节，比 for 循环快 10 倍以上
        return new ReadOnlySpan<byte>(pSource, valueBytesLength)
               .SequenceEqual(new ReadOnlySpan<byte>(pValue, valueBytesLength));
    }

    // 2. 获取模式串掩码
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetMask(int valueBytesLength)
    {
        return valueBytesLength switch
        {
            1 => 0xFF,
            2 => 0xFFFF,
            3 => 0xFFFFFF,
            4 => 0xFFFFFFFF,
            5 => 0xFFFFFFFFFF,
            6 => 0xFFFFFFFFFFFF,
            7 => 0xFFFFFFFFFFFFFF,
            8 => 0xFFFFFFFFFFFFFFFF,
            _ => 0
        };
    }
}