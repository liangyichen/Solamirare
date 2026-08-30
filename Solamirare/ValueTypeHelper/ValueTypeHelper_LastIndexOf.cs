using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Solamirare;


/// <summary>
/// 值类型计算辅助
/// </summary>
/// <summary>
/// Provides reverse-search helpers for unmanaged sequences.
/// </summary>
public unsafe static partial class ValueTypeHelper
{

    /// <summary>
    /// 从集合的末尾开始查找，返回最后一个匹配元素的索引
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="source"></param>
    /// <param name="pattern"></param>
    /// <returns></returns>
    public static int LastIndexOf<TSource, TValue>(ReadOnlySpan<TSource> source, ReadOnlySpan<TValue> pattern)
        where TSource : unmanaged
        where TValue : unmanaged
    {
        fixed (TValue* pValue = pattern)
        fixed (TSource* pSource = source)
        {
            return LastIndexOf(pSource, (uint)source.Length, pValue, (uint)pattern.Length);
        }
    }



    /// <summary>
    /// 从集合的末尾开始查找，返回最后一个匹配元素的索引
    /// </summary>
    public static int LastIndexOf<TSource, TValue>(TSource* source, uint sourceLength, TValue* value, uint valueLength)
        where TSource : unmanaged
        where TValue : unmanaged
    {

        int sizeOfValue = sizeof(TValue);
        int sizeofSource = sizeof(TSource);

        int valueBytesLength = (int)valueLength * sizeOfValue;
        int sourceBytesLength = (int)sourceLength * sizeofSource;

        byte* p_source = GetBytesPointer(source);
        byte* p_value = GetBytesPointer(value);


        delegate*<byte*, int, byte*, int, int, int> func;

        if (sourceBytesLength <= 32) func = &LastIndexOf_Short_Bytes;
        else
        {
            if (sourceBytesLength > 65535 && valueBytesLength > 64)
                func = &LastIndexOf_BoyerMoore;
            else
                func = &LastIndexOf_Ultra;
        }

        return func(p_source, sourceBytesLength, p_value, valueBytesLength, sizeOfValue);

    }


    /// <summary>
    /// Searches for the last occurrence of a byte-pattern inside a byte buffer using the vectorized hot path.
    /// </summary>
    /// <param name="pSource">Source byte pointer.</param>
    /// <param name="sourceBytesLength">Source length in bytes.</param>
    /// <param name="pValue">Pattern byte pointer.</param>
    /// <param name="valueBytesLength">Pattern length in bytes.</param>
    /// <param name="sizeOfValue">Original element size used for alignment filtering.</param>
    /// <returns>The last matching element index, or <c>-1</c> when no match is found.</returns>
    public static unsafe int LastIndexOf_Ultra(byte* pSource, int sourceBytesLength, byte* pValue, int valueBytesLength, int sizeOfValue)
    {
        if (valueBytesLength == 0) return sourceBytesLength / sizeOfValue;
        int limit = sourceBytesLength - valueBytesLength;
        if (limit < 0) return -1;

        byte firstByte = *pValue;
        Vector<byte> firstByteVector = new Vector<byte>(firstByte);
        ulong pValMask = VectorSearchHelper.GetMask(valueBytesLength);
        ulong pVal64 = (valueBytesLength <= 8) ? ((*(ulong*)pValue) & pValMask) : 0;
        var checker = new AlignmentChecker(sizeOfValue);

        int V = Vector<byte>.Count; // AVX2 下为 32
        byte* p = pSource + limit;

        // --- 1. 逆向对齐启动 (Reverse Aligned Startup) ---
        // 计算当前指针 p 超过上一个对齐边界的字节数
        // 例如 p = 0x...23, V=32, 则余数是 3 字节 (0x23 - 0x20)
        int misalignedBytes = (int)((nuint)p % (nuint)V);

        // 先用标量逻辑处理掉这 misalignedBytes 个字节，使 p 达到对齐位置
        for (int j = 0; j <= misalignedBytes; j++)
        {
            byte* pCurrent = p - j;
            int currentIdx = (int)(pCurrent - pSource);
            if (currentIdx >= 0 && *pCurrent == firstByte && checker.IsAligned(currentIdx))
            {
                if (VectorSearchHelper.IsMatch(pCurrent, pValue, valueBytesLength, pVal64, pValMask))
                    return currentIdx / sizeOfValue;
            }
        }

        // 此时 p 绝对对齐到了 V 的倍数 (例如 0x...20)
        p = (byte*)((nuint)p & ~(nuint)(V - 1));

        // --- 2. 逆向高速对齐主循环 (Reverse Aligned Hot Loop) ---
        // 确保向后读 64 字节不会越过 pSource
        if (p >= pSource + 64)
        {
            byte* pSafeStart = pSource + 64;
            while (p >= pSafeStart)
            {
                // 此时 p 是对齐的，我们读取 p 之前的两个块
                // Block0: [p-32, p-1], Block1: [p-64, p-33]
                byte* pBlock0 = p - V;
                byte* pBlock1 = p - (V * 2);

                // 使用对齐读取 LoadAligned (Unsafe.Read)
                Vector<byte> v0 = Unsafe.Read<Vector<byte>>(pBlock0);
                Vector<byte> v1 = Unsafe.Read<Vector<byte>>(pBlock1);

                Vector<byte> m0 = Vector.Equals(v0, firstByteVector);
                Vector<byte> m1 = Vector.Equals(v1, firstByteVector);

                // 必须先检查高地址块 m0
                if ((m0 | m1) != Vector<byte>.Zero)
                {
                    if (m0 != Vector<byte>.Zero)
                    {
                        uint mask = GetMask(m0);
                        int res = ExtractLastMatch(mask, pBlock0, pSource, pValue, valueBytesLength, pVal64, pValMask, checker, sizeOfValue, limit);
                        if (res != -1) return res;
                    }
                    if (m1 != Vector<byte>.Zero)
                    {
                        uint mask = GetMask(m1);
                        int res = ExtractLastMatch(mask, pBlock1, pSource, pValue, valueBytesLength, pVal64, pValMask, checker, sizeOfValue, limit);
                        if (res != -1) return res;
                    }
                }
                p -= V * 2;
            }
        }

        // --- 3. 头部安全收尾 (Final Cleanup) ---
        int i = (int)(p - pSource) - 1;
        for (; i >= 0; i--)
        {
            if (pSource[i] == firstByte && checker.IsAligned(i))
                if (VectorSearchHelper.IsMatch(pSource + i, pValue, valueBytesLength, pVal64, pValMask))
                    return i / sizeOfValue;
        }
        return -1;
    }

    // 跨平台 Mask 获取
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint GetMask(Vector<byte> matches)
    {
        if (Vector<byte>.Count == 32)
            return Vector256.ExtractMostSignificantBits(matches.AsVector256());
        return Vector128.ExtractMostSignificantBits(matches.AsVector128());
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int ExtractLastMatch(
        uint bitmask,             // 修正：直接接收提取好的掩码
        byte* pCurrent,
        byte* pStart,
        byte* pValue,
        int valLen,
        ulong pVal64,
        ulong pValMask,
        AlignmentChecker checker,
        int sizeOfValue,
        int limit)
    {
        while (bitmask != 0)
        {
            // 核心：使用 LeadingZeroCount 定位掩码中最右侧（地址最高）的 1
            int k = 31 - BitOperations.LeadingZeroCount(bitmask);
            int idx = (int)(pCurrent - pStart) + k;

            // 验证对齐与模式串
            if (idx <= limit && checker.IsAligned(idx))
            {
                if (VectorSearchHelper.IsMatch(pStart + idx, pValue, valLen, pVal64, pValMask))
                {
                    return idx / sizeOfValue;
                }
            }

            // 重要：抹掉当前最高位的 1，以便寻找下一个（更早的）匹配点
            bitmask ^= (1u << k);
        }
        return -1;
    }



    /// <summary>
    /// Searches for the last occurrence of a byte-pattern inside a short byte buffer.
    /// </summary>
    /// <param name="pSource">Source byte pointer.</param>
    /// <param name="sourceLen">Source length in bytes.</param>
    /// <param name="pValue">Pattern byte pointer.</param>
    /// <param name="valueLen">Pattern length in bytes.</param>
    /// <param name="sizeOfValue">Original element size used for alignment filtering.</param>
    /// <returns>The last matching element index, or <c>-1</c> when no match is found.</returns>
    public static unsafe int LastIndexOf_Short_Bytes(byte* pSource, int sourceLen, byte* pValue, int valueLen, int sizeOfValue)
    {
        if (valueLen == 0) return sourceLen / sizeOfValue;
        int limit = sourceLen - valueLen;
        if (limit < 0) return -1;

        // 1. SIMD 预扫描 (AVX2)
        Vector256<byte> vSource = Vector256.Load(pSource);
        Vector256<byte> vFirst = Vector256.Create(*pValue);
        uint bitmask = Vector256.ExtractMostSignificantBits(Vector256.Equals(vSource, vFirst));

        // 2. 范围截断 (0 到 limit 位)
        if (limit < 31) bitmask &= (1u << (limit + 1)) - 1;

        // 3. 无分支对齐过滤 (char: 0x55, int: 0x11)
        if (sizeOfValue == 2) bitmask &= 0x55555555u;
        else if (sizeOfValue == 4) bitmask &= 0x11111111u;

        if (bitmask == 0) return -1;

        // --- 4. 逆向全寄存器匹配路径 ---
        if (valueLen <= 8)
        {
            ulong mask = VectorSearchHelper.GetMask(valueLen);
            ulong pattern = (*(ulong*)pValue) & mask;

            // 【预取与提取】：从最高位 (MSB) 开始
            int k = 31 - BitOperations.LeadingZeroCount(bitmask);

            // Fast-Path: 尝试最后一个匹配点
            if (((*(ulong*)(pSource + k)) & mask) == pattern) return k / sizeOfValue;

            bitmask ^= (1u << k); // 抹掉最高位

            while (bitmask != 0)
            {
                k = 31 - BitOperations.LeadingZeroCount(bitmask);

                // 预取：计算下一个更高位的 1 (即内存中更靠前的匹配点)
                uint nextMask = bitmask ^ (1u << k);
                if (nextMask != 0 && Sse.IsSupported)
                {
                    int nextK = 31 - BitOperations.LeadingZeroCount(nextMask);
                    Sse.Prefetch0(pSource + nextK);
                }

                if (((*(ulong*)(pSource + k)) & mask) == pattern) return k / sizeOfValue;
                bitmask = nextMask;
            }
        }
        else if (valueLen <= 16)
        {
            Vector128<byte> vMask = GetVector128Mask(valueLen);
            Vector128<byte> vPattern = Vector128.Load(pValue) & vMask;

            while (bitmask != 0)
            {
                int k = 31 - BitOperations.LeadingZeroCount(bitmask);

                uint nextMask = bitmask ^ (1u << k);
                if (nextMask != 0 && Sse.IsSupported) Sse.Prefetch0(pSource + (31 - BitOperations.LeadingZeroCount(nextMask)));

                Vector128<byte> vCurrent = Vector128.LoadUnsafe(ref pSource[k]);
                if ((vCurrent & vMask) == vPattern) return k / sizeOfValue;
                bitmask = nextMask;
            }
        }
        else // 17 - 32 字节
        {
            Vector256<byte> vMask = GetVector256Mask(valueLen);
            Vector256<byte> vPattern = Vector256.Load(pValue) & vMask;

            while (bitmask != 0)
            {
                int k = 31 - BitOperations.LeadingZeroCount(bitmask);

                uint nextMask = bitmask ^ (1u << k);
                if (nextMask != 0 && Sse.IsSupported) Sse.Prefetch0(pSource + (31 - BitOperations.LeadingZeroCount(nextMask)));

                Vector256<byte> vCurrent = Vector256.LoadUnsafe(ref pSource[k]);
                if ((vCurrent & vMask) == vPattern) return k / sizeOfValue;
                bitmask = nextMask;
            }
        }

        return -1;
    }




}
