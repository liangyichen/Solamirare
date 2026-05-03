using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;


namespace Solamirare
{

    /// <summary>
    /// Provides high-performance helpers for unmanaged value-type sequences.
    /// </summary>
    public unsafe static partial class ValueTypeHelper
    {


        /// <summary>
        /// Counts the number of occurrences of a value sequence inside a source sequence.
        /// </summary>
        /// <typeparam name="TSource">Source element type.</typeparam>
        /// <typeparam name="TValue">Pattern element type.</typeparam>
        /// <param name="source">Source sequence pointer.</param>
        /// <param name="sourceLength">Source element count.</param>
        /// <param name="value">Pattern sequence pointer.</param>
        /// <param name="valueLength">Pattern element count.</param>
        /// <returns>The number of matches, or <c>-1</c> when the input is invalid.</returns>
        public static int Count<TSource, TValue>(TSource* source, uint sourceLength, TValue* value, uint valueLength)
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


            if (valueBytesLength == 0 || sourceBytesLength < valueBytesLength) return 0;

            // 分流逻辑：32 字节为界
            if (sourceBytesLength <= 32)
            {
                return Count_Short_Bytes(p_source, sourceBytesLength, p_value, valueBytesLength, sizeOfValue);
            }

            return Count_Ultra(p_source, sourceBytesLength, p_value, valueBytesLength, sizeOfValue);
        }

        /// <summary>
        /// 短模式：32 字节内统计
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int Count_Short_Bytes(byte* pSource, int sourceLen, byte* pValue, int valueLen, int sizeOfValue)
        {
            int limit = sourceLen - valueLen;

            // 1. 获取首字节匹配掩码
            Vector256<byte> vSource = Vector256.Load(pSource);
            Vector256<byte> vFirst = Vector256.Create(*pValue);
            uint bitmask = Vector256.ExtractMostSignificantBits(Vector256.Equals(vSource, vFirst));

            // 2. 截断无效位与对齐过滤
            bitmask &= (1u << (limit + 1)) - 1;
            if (sizeOfValue == 2) bitmask &= 0x55555555u;
            else if (sizeOfValue == 4) bitmask &= 0x11111111u;

            if (bitmask == 0) return 0;

            int count = 0;
            // 3. 验证模式串余下部分
            if (valueLen <= 8)
            {
                ulong mask = VectorSearchHelper.GetMask(valueLen);
                ulong pattern = (*(ulong*)pValue) & mask;

                while (bitmask != 0)
                {
                    int k = BitOperations.TrailingZeroCount(bitmask);
                    if (((*(ulong*)(pSource + k)) & mask) == pattern) count++;
                    bitmask &= bitmask - 1; // 抹掉已处理位
                }
            }
            else // 9-32 字节使用 Vector 比较
            {
                // 对于 Count 来说，如果模式串较长，重叠匹配的可能性极低（除非模式串本身有高度重复后缀）
                // 这里我们依然采用逐个位校验
                while (bitmask != 0)
                {
                    int k = BitOperations.TrailingZeroCount(bitmask);
                    if (IsMatch_Long(pSource + k, pValue, valueLen)) count++;
                    bitmask &= bitmask - 1;
                }
            }
            return count;
        }
        internal static unsafe int Count_Ultra(byte* pSource, int sourceLen, byte* pValue, int valueLen, int sizeOfValue)
        {
            int totalCount = 0;
            int limit = sourceLen - valueLen;
            int i = 0;

            byte firstByte = *pValue;
            Vector256<byte> vFirst = Vector256.Create(firstByte);

            // 只有当搜索的是真正的单字节（byte）时，PopCount 才是绝对安全的
            bool canUsePopCount = (valueLen == 1);

            // 1. 主循环：4路展开提高吞吐量
            for (; i <= limit - 128; i += 128)
            {
                uint m0 = Vector256.ExtractMostSignificantBits(Vector256.Equals(Vector256.Load(pSource + i), vFirst));
                uint m1 = Vector256.ExtractMostSignificantBits(Vector256.Equals(Vector256.Load(pSource + i + 32), vFirst));
                uint m2 = Vector256.ExtractMostSignificantBits(Vector256.Equals(Vector256.Load(pSource + i + 64), vFirst));
                uint m3 = Vector256.ExtractMostSignificantBits(Vector256.Equals(Vector256.Load(pSource + i + 96), vFirst));

                if (sizeOfValue == 2)
                {
                    m0 &= 0x55555555u; m1 &= 0x55555555u; m2 &= 0x55555555u; m3 &= 0x55555555u;
                }
                else if (sizeOfValue == 4)
                {
                    m0 &= 0x11111111u; m1 &= 0x11111111u; m2 &= 0x11111111u; m3 &= 0x11111111u;
                }

                if (canUsePopCount)
                {
                    totalCount += BitOperations.PopCount(m0) + BitOperations.PopCount(m1) + BitOperations.PopCount(m2) + BitOperations.PopCount(m3);
                }
                else
                {
                    if (m0 != 0) totalCount += ProcessMask(pSource + i, m0, pValue, valueLen);
                    if (m1 != 0) totalCount += ProcessMask(pSource + i + 32, m1, pValue, valueLen);
                    if (m2 != 0) totalCount += ProcessMask(pSource + i + 64, m2, pValue, valueLen);
                    if (m3 != 0) totalCount += ProcessMask(pSource + i + 96, m3, pValue, valueLen);
                }
            }

            // 2. 剩余 32 字节块处理
            for (; i <= limit - 32; i += 32)
            {
                uint m = Vector256.ExtractMostSignificantBits(Vector256.Equals(Vector256.Load(pSource + i), vFirst));
                if (sizeOfValue == 2) m &= 0x55555555u;
                else if (sizeOfValue == 4) m &= 0x11111111u;

                if (canUsePopCount) totalCount += BitOperations.PopCount(m);
                else totalCount += ProcessMask(pSource + i, m, pValue, valueLen);
            }

            // 3. 碎字节处理
            if (i <= limit)
            {
                totalCount += Count_Short_Bytes(pSource + i, sourceLen - i, pValue, valueLen, sizeOfValue);
            }

            return totalCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ProcessMask(byte* ptr, uint mask, byte* pValue, int valueLen)
        {
            int c = 0;
            while (mask != 0)
            {
                int k = BitOperations.TrailingZeroCount(mask);
                // 对于 char, int 等多字节类型，必须校验完整模式
                if (IsMatch_Long(ptr + k, pValue, valueLen)) c++;
                mask &= mask - 1;
            }
            return c;
        }



        // 辅助匹配方法：采用极限分支预测优化
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool IsMatch_Long(byte* p1, byte* p2, int len)
        {
            if (len <= 8)
            {
                ulong mask = VectorSearchHelper.GetMask(len);
                return (*(ulong*)p1 & mask) == (*(ulong*)p2 & mask);
            }

            // 大于 8 字节使用向量比对或 SequenceEqual
            return SequenceEqual_VectorT(p1, p2, len);
        }


    }


    /// <summary>
    /// 值类型计算辅助
    /// </summary>
    /// <summary>
    /// Provides comparison and hashing helpers for unmanaged value-type sequences.
    /// </summary>
    public unsafe static partial class ValueTypeHelper
    {



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool SafeCompareEqual(byte* p1, byte* p2, int len)
        {
            for (int i = 0; i < len; i++) if (p1[i] != p2[i]) return false;
            return true;
        }

        /// <summary>
        /// Checks whether the source bytes start with the specified prefix using the short fast path.
        /// </summary>
        /// <param name="pSource">Source byte pointer.</param>
        /// <param name="pValue">Prefix byte pointer.</param>
        /// <param name="len">Prefix length in bytes.</param>
        /// <returns><see langword="true"/> when the prefix matches; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool StartsWith_ShortPath(byte* pSource, byte* pValue, int len)
        {
            // 1-8 字节：使用 ulong 掩码单次比对 (0 分支)
            if (len <= 8)
            {
                ulong mask = VectorSearchHelper.GetMask(len);
                return (*(ulong*)pSource & mask) == (*(ulong*)pValue & mask);
            }

            // 9-16 字节：两次 ulong Overlapping (0 碎字节分支)
            if (len <= 16)
            {
                if (*(ulong*)pSource != *(ulong*)pValue) return false;
                return *(ulong*)(pSource + len - 8) == *(ulong*)(pValue + len - 8);
            }

            // 17-32 字节：两次 Vector128 Overlapping (AVX/SSE)
            // 即使 len=23，通过 [0..15] 和 [len-16...len-1] 两次比对即可完全覆盖
            if (Unsafe.ReadUnaligned<Vector128<byte>>(pSource) != Unsafe.ReadUnaligned<Vector128<byte>>(pValue))
                return false;

            return Unsafe.ReadUnaligned<Vector128<byte>>(pSource + len - 16) == Unsafe.ReadUnaligned<Vector128<byte>>(pValue + len - 16);
        }

        /// <summary>
        /// Checks whether the source bytes start with the specified prefix.
        /// </summary>
        /// <param name="pSource">Source byte pointer.</param>
        /// <param name="sourceLen">Source length in bytes.</param>
        /// <param name="pValue">Prefix byte pointer.</param>
        /// <param name="valueLen">Prefix length in bytes.</param>
        /// <returns><see langword="true"/> when the prefix matches; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool StartsWith(byte* pSource, int sourceLen, byte* pValue, int valueLen)
        {
            // 1. 基础防御
            if ((uint)valueLen > (uint)sourceLen) return false;
            if (valueLen == 0) return true;

            // 2. 极致分流
            // 32 字节是现代 CPU 一个缓存行的一半，也是 YMM 寄存器的宽度，是理想的分水岭
            if (valueLen <= 32)
            {
                return StartsWith_ShortPath(pSource, pValue, valueLen);
            }

            // 3. 长模式直接复用已优化的 SequenceEqual 逻辑
            return SequenceEqual_VectorT(pSource, pValue, valueLen);
        }

        /// <summary>
        /// Checks whether the source bytes end with the specified suffix.
        /// </summary>
        /// <param name="pSource">Source byte pointer.</param>
        /// <param name="sourceLen">Source length in bytes.</param>
        /// <param name="pValue">Suffix byte pointer.</param>
        /// <param name="valueLen">Suffix length in bytes.</param>
        /// <returns><see langword="true"/> when the suffix matches; otherwise <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool EndsWith(byte* pSource, int sourceLen, byte* pValue, int valueLen)
        {
            if ((uint)valueLen > (uint)sourceLen) return false;
            if (valueLen == 0) return true;

            // 计算末尾起始指针
            byte* pStart = pSource + (sourceLen - valueLen);

            if (valueLen <= 32)
            {
                return StartsWith_ShortPath(pStart, pValue, valueLen);
            }

            return SequenceEqual_VectorT(pStart, pValue, valueLen);
        }



        /// <summary>
        /// Computes a hash code for the specified span.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="target">Source span.</param>
        /// <returns>A 32-bit hash value.</returns>
        public static uint HashCode<T>(this ReadOnlySpan<T> target)
        where T : unmanaged
        {
            fixed (T* p = target)
            {
                return HashCode(p, target.Length);
            }
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong WyMix(ulong a, ulong b)
        {
            UInt128 r = (UInt128)a * b;
            return (ulong)(r >> 64) ^ (ulong)r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong WyR8(byte* p) => *(ulong*)p;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong WyR4(byte* p) => *(uint*)p;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong WyR3(byte* p, int k) =>
            ((ulong)p[0] << 16) | ((ulong)p[k >> 1] << 8) | p[k - 1];


        /// <summary>
        /// Computes a hash code for an unmanaged sequence.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="pData">Source pointer.</param>
        /// <param name="length">Element count.</param>
        /// <param name="seed">Optional hash seed.</param>
        /// <returns>A 32-bit hash value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint HashCode<T>(T* pData, int length, uint seed = 0)
        where T : unmanaged
        {
            byte* p = (byte*)pData;
            int len = length * sizeof(T);

            const ulong s0 = 0xa0761d6478bd642ful;
            const ulong s1 = 0xe7037ed1a0b428dbul;
            const ulong s2 = 0x8ebc6af09c88c6e3ul;
            const ulong s3 = 0x589965cc75374cc3ul;

            ulong h = seed ^ WyMix(seed ^ s0, s1);

            if (len <= 0) return (uint)WyMix(h, s1);

            if (len < 4)
                h = WyMix(WyR3(p, len) ^ s0 ^ h, s1 ^ seed);
            else if (len <= 8)
                h = WyMix((WyR4(p) << 32 | WyR4(p + len - 4)) ^ s0 ^ h, s1 ^ seed);
            else if (len <= 16)
                h = WyMix(WyR8(p) ^ s0 ^ h, WyR8(p + len - 8) ^ s1 ^ seed);
            else if (len <= 24)
                h = WyMix(WyR8(p) ^ s0 ^ h, WyR8(p + 8) ^ s1 ^ seed) ^ WyMix(WyR8(p + len - 8) ^ s2 ^ h, seed ^ s3);
            else if (len <= 32)
                h = WyMix(WyR8(p) ^ s0 ^ h, WyR8(p + 8) ^ s1 ^ seed) ^ WyMix(WyR8(p + 16) ^ s2 ^ h, WyR8(p + len - 8) ^ s3 ^ seed);
            else
            {
                ulong see1 = seed, see2 = seed;
                while (len > 48)
                {
                    h = WyMix(WyR8(p) ^ s0 ^ h, WyR8(p + 8) ^ s1 ^ seed);
                    see1 = WyMix(WyR8(p + 16) ^ s2 ^ see1, WyR8(p + 24) ^ s3 ^ see1);
                    see2 = WyMix(WyR8(p + 32) ^ s0 ^ see2, WyR8(p + 40) ^ s1 ^ see2);
                    p += 48; len -= 48;
                }
                h ^= see1 ^ see2;
                if (len > 32) { h = WyMix(WyR8(p) ^ s0 ^ h, WyR8(p + 8) ^ s1 ^ seed); h = WyMix(WyR8(p + 16) ^ s2 ^ h, WyR8(p + len - 8) ^ s3 ^ seed); }
                else if (len > 16) { h = WyMix(WyR8(p) ^ s0 ^ h, WyR8(p + 8) ^ s1 ^ seed); h = WyMix(WyR8(p + len - 8) ^ s2 ^ h, seed ^ s3); }
                else if (len > 8) { h = WyMix(WyR8(p) ^ s0 ^ h, WyR8(p + len - 8) ^ s1 ^ seed); }
                else { h = WyMix((WyR4(p) << 32 | WyR4(p + len - 4)) ^ s0 ^ h, s1 ^ seed); }
            }

            return (uint)WyMix(h ^ (ulong)len, s1);
        }









        /// <summary>
        /// 将 T* 指针转换为 byte*，零分配地模拟 MemoryMarshal.AsBytes 的语义。
        /// 此方法通过 ref 间接转换，向 JIT 提供了更明确的字节流视图，
        /// 以解决直接 (byte*)T* 转换导致的计算错误问题。
        /// </summary>
        /// <param name="ptr">原始非托管类型指针 (例如 int*)</param>
        /// <returns>修正后的 byte* 指针</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte* GetBytesPointer<T>(T* ptr)
            where T : unmanaged
        {
            // 1. 获取 ptr 指向的第一个元素的引用 (ref T)。
            //    这告诉编译器我们要处理 T 类型的数据。
            ref T startRef = ref Unsafe.AsRef<T>(ptr);

            // 2. 将该引用转换为原始内存地址 (void*)。
            fixed (void* voidPtr = &startRef)
            {

                // 3. 将 void* 转换为 byte*。
                // 这种通过 ref 间接转换的模式比简单的 (byte*)ptr 更具语义，
                // 能够解决 JIT 编译器对原始内存布局的错误假设。
                return (byte*)voidPtr;
            }
        }


        /// <summary>
        /// ASCII 大小写不敏感的 char 序列比对（char* 版本）。
        ///
        /// 思路与 byte* 版完全一致，差异仅在元素宽度：
        ///   char = ushort = 2 bytes，SIMD 寄存器容纳的字符数减半：
        ///     Vector256&lt;ushort&gt; → 16 chars/次（256 bits / 16 bits）
        ///     Vector128&lt;ushort&gt; → 8  chars/次（128 bits / 16 bits）
        ///
        /// 大小写处理逻辑与 byte* 版相同：
        ///   | 0x20   → 转小写（仅对 ASCII 字母有意义，非字母位无影响）
        ///   范围检测 → ('a'-1) &lt; (c | 0x20) &lt; ('z'+1) 判断是否为字母
        ///   非字母字符要求 direct_equal（原始值相等）；
        ///   字母字符允许 case_equal（| 0x20 后相等）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEqualIgnoreCase(char* b1, char* b2, int length)
        {
            if (b1 == b2) return true;

            int i = 0;

            // ── Vector256<ushort>：每次处理 16 个 char ────────────────────────────
            if (Vector256.IsHardwareAccelerated && length >= 16)
            {
                Vector256<ushort> v_a_minus_1 = Vector256.Create((ushort)('a' - 1));
                Vector256<ushort> v_z_plus_1 = Vector256.Create((ushort)('z' + 1));
                Vector256<ushort> v_or_20 = Vector256.Create((ushort)0x20);

                for (; i <= length - 16; i += 16)
                {
                    // 以 ushort* 加载，每次读 16 个 char（32 bytes）
                    Vector256<ushort> v1 = Vector256.Load((ushort*)(b1 + i));
                    Vector256<ushort> v2 = Vector256.Load((ushort*)(b2 + i));

                    // 直接相等（大小写相同或非字母字符）
                    Vector256<ushort> direct_equal = Vector256.Equals(v1, v2);

                    // 转小写后比较
                    Vector256<ushort> v1_lower = Vector256.BitwiseOr(v1, v_or_20);
                    Vector256<ushort> case_equal = Vector256.Equals(
                        v1_lower,
                        Vector256.BitwiseOr(v2, v_or_20));

                    // 判断 v1 是否为 ASCII 字母（a-z 范围）
                    Vector256<ushort> is_alpha = Vector256.BitwiseAnd(
                        Vector256.GreaterThan(v1_lower, v_a_minus_1),
                        Vector256.LessThan(v1_lower, v_z_plus_1));

                    // 字母位：允许大小写相等；非字母位：必须直接相等
                    Vector256<ushort> combined = Vector256.BitwiseOr(
                        direct_equal,
                        Vector256.BitwiseAnd(case_equal, is_alpha));

                    // 16 个 ushort 全部满足时 MSB 掩码 = 0xFFFF（16 位全 1）
                    if (combined.ExtractMostSignificantBits() != 0xFFFF) return false;
                }
            }

            // ── Vector128<ushort>：每次处理 8 个 char ─────────────────────────────
            if (Vector128.IsHardwareAccelerated && length - i >= 8)
            {
                Vector128<ushort> v_a_minus_1 = Vector128.Create((ushort)('a' - 1));
                Vector128<ushort> v_z_plus_1 = Vector128.Create((ushort)('z' + 1));
                Vector128<ushort> v_or_20 = Vector128.Create((ushort)0x20);

                for (; i <= length - 8; i += 8)
                {
                    Vector128<ushort> v1 = Vector128.Load((ushort*)(b1 + i));
                    Vector128<ushort> v2 = Vector128.Load((ushort*)(b2 + i));

                    Vector128<ushort> direct_equal = Vector128.Equals(v1, v2);

                    Vector128<ushort> v1_lower = Vector128.BitwiseOr(v1, v_or_20);
                    Vector128<ushort> case_equal = Vector128.Equals(
                        v1_lower,
                        Vector128.BitwiseOr(v2, v_or_20));

                    Vector128<ushort> is_alpha = Vector128.BitwiseAnd(
                        Vector128.GreaterThan(v1_lower, v_a_minus_1),
                        Vector128.LessThan(v1_lower, v_z_plus_1));

                    Vector128<ushort> combined = Vector128.BitwiseOr(
                        direct_equal,
                        Vector128.BitwiseAnd(case_equal, is_alpha));

                    // 8 个 ushort 全部满足时 MSB 掩码 = 0xFF（8 位全 1）
                    if (combined.ExtractMostSignificantBits() != 0xFF) return false;
                }
            }

            // ── 标量尾部：处理剩余不足 8 个的 char ───────────────────────────────
            for (; i < length; i++)
            {
                char x = b1[i];
                char y = b2[i];
                if (x == y) continue;

                // 检查是否互为大小写（仅限 ASCII 字母）
                int xOr = x | 0x20;
                if (xOr != (y | 0x20)) return false;
                if (!((xOr >= 'a') && (xOr <= 'z'))) return false;
            }

            return true;
        }



        /// <summary>
        /// ASCII 大小写不敏感的序列比对
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEqualIgnoreCase(byte* b1, byte* b2, int length)
        {
            if (b1 == b2) return true;
            int i = 0;

            if (Vector256.IsHardwareAccelerated && length >= 32)
            {
                Vector256<byte> v_a_minus_1 = Vector256.Create((byte)('a' - 1));
                Vector256<byte> v_z_plus_1 = Vector256.Create((byte)('z' + 1));
                Vector256<byte> v_or_20 = Vector256.Create((byte)0x20);

                for (; i <= length - 32; i += 32)
                {
                    Vector256<byte> v1 = Vector256.Load(b1 + i);
                    Vector256<byte> v2 = Vector256.Load(b2 + i);
                    Vector256<byte> direct_equal = Vector256.Equals(v1, v2);
                    Vector256<byte> v1_lower = Vector256.BitwiseOr(v1, v_or_20);
                    Vector256<byte> case_equal = Vector256.Equals(v1_lower, Vector256.BitwiseOr(v2, v_or_20));
                    Vector256<byte> is_alpha = Vector256.BitwiseAnd(
                        Vector256.GreaterThan(v1_lower, v_a_minus_1),
                        Vector256.LessThan(v1_lower, v_z_plus_1));

                    Vector256<byte> combined = Vector256.BitwiseOr(direct_equal, Vector256.BitwiseAnd(case_equal, is_alpha));
                    if (combined.ExtractMostSignificantBits() != 0xFFFFFFFF) return false;
                }
            }

            if (Vector128.IsHardwareAccelerated && length - i >= 16)
            {
                Vector128<byte> v_a_minus_1 = Vector128.Create((byte)('a' - 1));
                Vector128<byte> v_z_plus_1 = Vector128.Create((byte)('z' + 1));
                Vector128<byte> v_or_20 = Vector128.Create((byte)0x20);

                for (; i <= length - 16; i += 16)
                {
                    Vector128<byte> v1 = Vector128.Load(b1 + i);
                    Vector128<byte> v2 = Vector128.Load(b2 + i);
                    Vector128<byte> direct_equal = Vector128.Equals(v1, v2);
                    Vector128<byte> v1_lower = Vector128.BitwiseOr(v1, v_or_20);
                    Vector128<byte> case_equal = Vector128.Equals(v1_lower, Vector128.BitwiseOr(v2, v_or_20));
                    Vector128<byte> is_alpha = Vector128.BitwiseAnd(
                        Vector128.GreaterThan(v1_lower, v_a_minus_1),
                        Vector128.LessThan(v1_lower, v_z_plus_1));

                    Vector128<byte> combined = Vector128.BitwiseOr(direct_equal, Vector128.BitwiseAnd(case_equal, is_alpha));
                    if (combined.ExtractMostSignificantBits() != 0xFFFF) return false;
                }
            }

            for (; i < length; i++)
            {
                byte x = b1[i];
                byte y = b2[i];
                if (x == y) continue;

                // 检查是否互为大小写 (仅限 ASCII 字母)
                // 逻辑：两个字节 OR 0x20 后相等，且 OR 0x20 后在 'a'-'z' 范围内
                int xOr = x | 0x20;
                if (xOr != (y | 0x20)) return false;
                if (!((xOr >= 'a') && (xOr <= 'z'))) return false;
            }
            return true;
        }

    }
}
