
using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Solamirare;


/// <summary>
/// Provides short-buffer search helpers for unmanaged sequences.
/// </summary>
public unsafe static partial class ValueTypeHelper
{


    /// <summary>
    /// Searches for the first occurrence of a byte-pattern inside a short byte buffer.
    /// </summary>
    /// <param name="pSource">Source byte pointer.</param>
    /// <param name="sourceLen">Source length in bytes.</param>
    /// <param name="pValue">Pattern byte pointer.</param>
    /// <param name="valueLen">Pattern length in bytes.</param>
    /// <param name="sizeOfValue">Original element size used for alignment filtering.</param>
    /// <returns>The first matching element index, or <c>-1</c> when no match is found.</returns>
    public static unsafe int IndexOf_Short_Bytes(byte* pSource, int sourceLen, byte* pValue, int valueLen, int sizeOfValue)
    {
        if (valueLen == 0) return 0;
        int limit = sourceLen - valueLen;
        if (limit < 0) return -1;

        // 1. 扫描首字节获取位掩码
        Vector256<byte> vSource = Vector256.Load(pSource);
        Vector256<byte> vFirst = Vector256.Create(*pValue);
        uint bitmask = Vector256.ExtractMostSignificantBits(Vector256.Equals(vSource, vFirst));

        // 2. 截断与对齐过滤 (Size=2 使用 0x55, Size=4 使用 0x11)
        if (limit < 31) bitmask &= (1u << (limit + 1)) - 1;
        if (sizeOfValue == 2) bitmask &= 0x55555555u;
        else if (sizeOfValue == 4) bitmask &= 0x11111111u;

        if (bitmask == 0) return -1;

        // --- 极限优化：提取与预处理模式串 ---
        if (valueLen <= 8)
        {
            ulong mask = VectorSearchHelper.GetMask(valueLen);
            ulong pattern = (*(ulong*)pValue) & mask;

            // 【预取优化点】：提取第一个索引，但不立即进入循环
            int k = BitOperations.TrailingZeroCount(bitmask);

            // 尝试第一个匹配 (Fast Path)
            if (((*(ulong*)(pSource + k)) & mask) == pattern) return k / sizeOfValue;

            // 抹掉已检查的位
            bitmask &= bitmask - 1;

            // 【双路扫描】：如果还有位，进入手动展开的循环
            while (bitmask != 0)
            {
                k = BitOperations.TrailingZeroCount(bitmask);

                // 预取：如果 bitmask 还有其他位，提前通知 CPU 加载下一个 k 的内存
                uint nextBitmask = bitmask & (bitmask - 1);
                if (nextBitmask != 0 && Sse.IsSupported)
                {
                    int nextK = BitOperations.TrailingZeroCount(nextBitmask);
                    // 使用 Prefetch0 将下一个可能的匹配点拉入 L1 缓存
                    Sse.Prefetch0(pSource + nextK);
                }

                if (((*(ulong*)(pSource + k)) & mask) == pattern) return k / sizeOfValue;
                bitmask = nextBitmask;
            }
        }
        else if (valueLen <= 16)
        {
            Vector128<byte> vMask = GetVector128Mask(valueLen);
            Vector128<byte> vPattern = Vector128.Load(pValue) & vMask;

            while (bitmask != 0)
            {
                int k = BitOperations.TrailingZeroCount(bitmask);

                // 预取逻辑
                uint nextMask = bitmask & (bitmask - 1);
                if (nextMask != 0 && Sse.IsSupported) Sse.Prefetch0(pSource + BitOperations.TrailingZeroCount(nextMask));

                Vector128<byte> vCurrent = Vector128.LoadUnsafe(ref pSource[k]);
                if ((vCurrent & vMask) == vPattern) return k / sizeOfValue;
                bitmask = nextMask;
            }
        }
        else // 17-32 字节
        {
            Vector256<byte> vMask = GetVector256Mask(valueLen);
            Vector256<byte> vPattern = Vector256.Load(pValue) & vMask;

            while (bitmask != 0)
            {
                int k = BitOperations.TrailingZeroCount(bitmask);

                uint nextMask = bitmask & (bitmask - 1);
                if (nextMask != 0 && Sse.IsSupported) Sse.Prefetch0(pSource + BitOperations.TrailingZeroCount(nextMask));

                Vector256<byte> vCurrent = Vector256.LoadUnsafe(ref pSource[k]);
                if ((vCurrent & vMask) == vPattern) return k / sizeOfValue;
                bitmask = nextMask;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> GetVector128Mask(int length)
    {
        // 构造一个前 length 字节为 0xFF，后 (16-length) 字节为 0x00 的向量
        // 极限做法：使用预定义的静态数组配合 Unsafe.Read
        ReadOnlySpan<byte> maskBase = new byte[32] {
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    };
        fixed (byte* p = maskBase)
        {
            // 技巧：通过移动指针位置来获取不同长度的全 1 掩码
            return Vector128.Load(p + (16 - length));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<byte> GetVector256Mask(int length)
    {
        ReadOnlySpan<byte> maskBase = new byte[64] {
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    };
        fixed (byte* p = maskBase)
        {
            return Vector256.Load(p + (32 - length));
        }
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int QuickExtract(uint bitmask, byte* pSource, byte* pValue, int valLen, int sizeOfValue, int limit)
    {
        // 预计算 8 字节快速比对逻辑
        ulong pValMask = VectorSearchHelper.GetMask(valLen);
        ulong pVal64 = (valLen <= 8) ? ((*(ulong*)pValue) & pValMask) : 0;

        // 使用之前的乘法逆元检查器
        var checker = new AlignmentChecker(sizeOfValue);

        while (bitmask != 0)
        {
            // 瞬间定位下一个匹配首字节的索引 k
            int k = BitOperations.TrailingZeroCount(bitmask);

            // 仅在满足 sizeOfValue 对齐时进行深度比对
            if (checker.IsAligned(k))
            {
                if (VectorSearchHelper.IsMatch(pSource + k, pValue, valLen, pVal64, pValMask))
                {
                    // JIT 会将 sizeOfValue 为 1,2,4,8 的除法优化为位移
                    return k / sizeOfValue;
                }
            }

            // 抹掉当前处理位，寻找下一个匹配点
            bitmask &= bitmask - 1;
        }
        return -1;
    }

    private static unsafe int IndexOf_Short_Safe_Fallback(byte* pSource, int sourceLen, byte* pValue, int valLen, int sizeOfValue)
    {
        byte firstByte = *pValue;
        int limit = sourceLen - valLen;
        var checker = new AlignmentChecker(sizeOfValue);

        ulong pValMask = VectorSearchHelper.GetMask(valLen);
        ulong pVal64 = (valLen <= 8) ? ((*(ulong*)pValue) & pValMask) : 0;

        for (int i = 0; i <= limit; i++)
        {
            if (pSource[i] == firstByte && checker.IsAligned(i))
            {
                if (VectorSearchHelper.IsMatch(pSource + i, pValue, valLen, pVal64, pValMask))
                    return i / sizeOfValue;
            }
        }
        return -1;
    }





}
