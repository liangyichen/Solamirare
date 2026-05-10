
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Solamirare;



/// <summary>
/// Provides high-performance index search helpers for unmanaged sequences.
/// </summary>
public unsafe static partial class ValueTypeHelper
{


    /// <summary>
    /// 对两个集合进行交集计算，得出后者在前者中的下标
    /// </summary>
    public static int IndexOf<TSource, TValue>(Span<TSource> source, ReadOnlySpan<TValue> value)
        where TSource : unmanaged
        where TValue : unmanaged
    {
        fixed (TValue* pValue = value)
        fixed (TSource* pSource = source)
        {
            return IndexOf(pSource, (uint)source.Length, pValue, (uint)value.Length);
        }
    }


    /// <summary>
    /// 对两段内存进行交集计算，得出后者在前者中的下标
    /// <para> sourceLength 和 valueLength 对应 source 和 value 的个数（注意不是byte长度）</para>
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="source"></param>
    /// <param name="sourceLength"></param>
    /// <param name="value"></param>
    /// <param name="valueLength"></param>
    /// <returns></returns>
    public static int IndexOf<TSource, TValue>(TSource* source, uint sourceLength, TValue* value, uint valueLength)
        where TSource : unmanaged
        where TValue : unmanaged
    {
        if (source is null || value is null) return -1;

        if (valueLength == 0) return 0;

        if (sourceLength < valueLength) return -1;

        int sizeofSource = sizeof(TSource);
        int sizeOfValue = sizeof(TValue);

        int valueBytesLength = (int)valueLength * sizeOfValue;
        int sourceBytesLength = (int)sourceLength * sizeofSource;

        byte* p_source = GetBytesPointer(source);
        byte* p_value = GetBytesPointer(value);



        delegate*<byte*, int, byte*, int, int, int> func;

        if (sourceBytesLength <= 32) func = &IndexOf_Short_Bytes;
        else
        {
            if (sourceBytesLength > 65535 && valueBytesLength > 64)
                func = &IndexOf_BoyerMoore;
            else
                func = &IndexOf_Ultra;
        }

        return func(p_source, sourceBytesLength, p_value, valueBytesLength, sizeOfValue);

    }


    /// <summary>
    /// Searches for the first occurrence of a byte-pattern inside a byte buffer using the vectorized hot path.
    /// </summary>
    /// <param name="pSource">Source byte pointer.</param>
    /// <param name="sourceBytesLength">Source length in bytes.</param>
    /// <param name="pValue">Pattern byte pointer.</param>
    /// <param name="valueBytesLength">Pattern length in bytes.</param>
    /// <param name="sizeOfValue">Original element size used for alignment filtering.</param>
    /// <returns>The first matching element index, or <c>-1</c> when no match is found.</returns>
public static unsafe int IndexOf_Ultra(byte* pSource, int sourceBytesLength, byte* pValue, int valueBytesLength, int sizeOfValue)
{
    if (valueBytesLength == 0) return 0;
    int limit = sourceBytesLength - valueBytesLength;
    if (limit < 0) return -1;

    byte firstByte = *pValue;
    Vector<byte> firstByteVector = new Vector<byte>(firstByte);
    ulong pValMask = VectorSearchHelper.GetMask(valueBytesLength);
    ulong pVal64 = (valueBytesLength <= 8) ? ((*(ulong*)pValue) & pValMask) : 0;
    var checker = new AlignmentChecker(sizeOfValue);
    
    int V = Vector<byte>.Count; // AVX2 为 32
    byte* p = pSource;

    // --- 1. 对齐启动 (Aligned Startup) ---
    // 计算当前指针到下一个 V 字节对齐边界的偏移
    // 例如 p 是 0x...03, V=32, 则 offset = 29
    int alignmentOffset = (int)((nuint)p % (nuint)V);
    if (alignmentOffset != 0)
    {
        int bytesToAlignment = V - alignmentOffset;
        // 在对齐之前的这段小数据，直接用标量逻辑或微型块逻辑处理
        // 注意：不能超过 limit
        int startupLimit = Math.Min(bytesToAlignment, limit + 1);
        for (int j = 0; j < startupLimit; j++)
        {
            if (pSource[j] == firstByte && checker.IsAligned(j))
            {
                if (VectorSearchHelper.IsMatch(pSource + j, pValue, valueBytesLength, pVal64, pValMask))
                    return j / sizeOfValue;
            }
        }
        p += bytesToAlignment;
    }

    // --- 2. 高速对齐主循环 (Aligned Hot Loop) ---
    // 此时 p 绝对对齐到了 V (32字节) 边界
    if (pSource + sourceBytesLength - p >= 64)
    {
        byte* pSafeLimit = pSource + sourceBytesLength - 64;

        while (p <= pSafeLimit)
        {
            // 【极限优化】：既然地址已对齐，使用 Read 而非 ReadUnaligned
            // 在某些硬件架构上，对齐读取拥有更高的吞吐量
            Vector<byte> v0 = Unsafe.Read<Vector<byte>>(p); 
            Vector<byte> v1 = Unsafe.Read<Vector<byte>>(p + V);

            Vector<byte> m0 = Vector.Equals(v0, firstByteVector);
            Vector<byte> m1 = Vector.Equals(v1, firstByteVector);

            if ((m0 | m1) != Vector<byte>.Zero) // 快速合并检查，减少跳转
            {
                if (m0 != Vector<byte>.Zero)
                {
                    int res = ExtractMatch(m0, p, pSource, pValue, valueBytesLength, pVal64, pValMask, checker, sizeOfValue, limit);
                    if (res != -1) return res;
                }
                if (m1 != Vector<byte>.Zero)
                {
                    int res = ExtractMatch(m1, p + V, pSource, pValue, valueBytesLength, pVal64, pValMask, checker, sizeOfValue, limit);
                    if (res != -1) return res;
                }
            }
            p += V * 2;
        }
    }

    // --- 3. 安全收尾 (Final Cleanup) ---
    int i = (int)(p - pSource);
    for (; i <= limit; i++)
    {
        if (pSource[i] == firstByte && checker.IsAligned(i))
            if (VectorSearchHelper.IsMatch(pSource + i, pValue, valueBytesLength, pVal64, pValMask)) 
                return i / sizeOfValue;
    }
    return -1;
}
 




    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ExtractMatch(
        Vector<byte> matches,
        byte* pCurrent,
        byte* pStart,
        byte* pValue,
        int valLen,
        ulong pVal64,
        ulong pValMask,
        AlignmentChecker checker, // 用于快速对齐判断
        int sizeOfValue,          // 用于最后计算索引 (idx / sizeOfValue)
        int limit)
    {
        uint bitmask;

        // 跨平台掩码提取逻辑：x64 为 32 位，ARM64 为 16 位
        if (Vector<byte>.Count == 32)
            bitmask = Vector256.ExtractMostSignificantBits(matches.AsVector256());
        else
            bitmask = Vector128.ExtractMostSignificantBits(matches.AsVector128());

        while (bitmask != 0)
        {
            // 定位第一个匹配的 '1'
            int k = BitOperations.TrailingZeroCount(bitmask);
            int idx = (int)(pCurrent - pStart) + k;

            // 核心优化点：使用乘法逆元或位掩码进行对齐检查
            if (idx <= limit && checker.IsAligned(idx))
            {
                // 模式串深度验证
                if (VectorSearchHelper.IsMatch(pStart + idx, pValue, valLen, pVal64, pValMask))
                {
                    // 这个除法只在成功找到匹配项并准备退出函数时执行一次
                    // 
                    return idx / sizeOfValue;
                }
            }

            // 抹掉已处理位
            bitmask &= bitmask - 1;
        }
        return -1;
    }



    internal struct AlignmentChecker
    {
        private readonly uint _multiplier;
        private readonly uint _threshold;
        private readonly bool _isPowerOfTwo;
        private readonly uint _mask;

        public AlignmentChecker(int sizeOfValue)
        {
            uint d = (uint)sizeOfValue;
            _isPowerOfTwo = (d & (d - 1)) == 0;
            if (_isPowerOfTwo)
            {
                _mask = d - 1;
                _multiplier = 0;
                _threshold = 0;
            }
            else
            {
                // 乘法逆元预计算逻辑：判定 n % d == 0 等价于 (n * M) <= T
                // 这是一个经典的编译器优化技巧
                _multiplier = GetMultiplier(d);
                _threshold = uint.MaxValue / d;
                _mask = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAligned(int index)
        {
            if (_isPowerOfTwo) return ((uint)index & _mask) == 0;
            // 关键点：将取模转化为一次乘法
            return ((uint)index * _multiplier) <= _threshold;
        }

        private static uint GetMultiplier(uint d)
        {
            // 计算 d 的乘法逆元 (针对 32 位无符号整数)
            uint m = 1;
            for (int i = 0; i < 32; i++) m *= 2 - d * m;
            return m;
        }
    }

}
