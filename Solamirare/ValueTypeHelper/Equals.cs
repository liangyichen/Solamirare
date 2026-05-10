using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Solamirare;

public unsafe static partial class ValueTypeHelper
{

    /// <summary>
    /// 与指定的内存段比较，值是否相等
    /// </summary>
    /// <param name="a"></param>
    /// <param name="sizeA"></param>
    /// <param name="b"></param>
    /// <param name="sizeB"></param>
    /// <returns></returns>
    public static bool Equals<T>(T* a, uint sizeA, T* b, uint sizeB)
    where T : unmanaged
    {

        if (a is null || b is null) return sizeA == 0 && sizeB == 0;
        if (sizeB == 0) return true;
        if (sizeA != sizeB) return false;



        byte* p1 = GetBytesPointer(a);
        byte* p2 = GetBytesPointer(b);

        int lengthBytes = sizeof(T) * (int)sizeB;

        // 1. 极短路径（L <= 32 字节）：最快出口，无冗余检查
        if (lengthBytes <= 32)
            return SequenceEqual_ShortPath(p1, p2, lengthBytes);


        // 2. 长路径预检 (仅 lengthBytes > 32 时执行)

        // 2a. 首尾检测 (单分支退出)
        if (p1[0] != p2[0] || p1[lengthBytes - 1] != p2[lengthBytes - 1])
            return false;

        // 2b. 针对大集合的单次随机取样检查
        if (lengthBytes > 256 && !SequenceEqual_MidpointPreCheck(p1, p2, lengthBytes))
        {
            return false;
        }


        return SequenceEqual_VectorT(p1, p2, lengthBytes);

    }

    /// <summary>
    /// 极短路径比较 (L &lt;= 32 字节)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static unsafe bool SequenceEqual_ShortPath(byte* p1, byte* p2, int lengthBytes)
    {
        // 0. 针对 0 长度的极速返回
        if (lengthBytes == 0) return true;

        // --- 极致路径 A：1-8 字节 (ulong 掩码单次比对) ---
        if (lengthBytes <= 8)
        {
            ulong mask = VectorSearchHelper.GetMask(lengthBytes);
            return (*(ulong*)p1 & mask) == (*(ulong*)p2 & mask);
        }

        // --- 极致路径 B：9-16 字节 (两次 ulong 重叠比对) ---
        // 即使 lengthBytes 是 11，比较 [0..7] 和 [length-8...length-1] 也能覆盖全貌
        if (lengthBytes <= 16)
        {
            if (*(ulong*)p1 != *(ulong*)p2) return false;
            return *(ulong*)(p1 + lengthBytes - 8) == *(ulong*)(p2 + lengthBytes - 8);
        }

        // --- 极致路径 C：17-32 字节 (两次 Vector128 重叠比对) ---
        // 使用 XMM 寄存器，1 条指令覆盖 16 字节。
        // 即使 lengthBytes 是 31，比较 [0..15] 和 [length-16...length-1] 也能覆盖全貌。
        if (Unsafe.ReadUnaligned<Vector128<byte>>(p1) != Unsafe.ReadUnaligned<Vector128<byte>>(p2))
            return false;

        return Unsafe.ReadUnaligned<Vector128<byte>>(p1 + lengthBytes - 16) == Unsafe.ReadUnaligned<Vector128<byte>>(p2 + lengthBytes - 16);
    }


    /// <summary>
    /// Vector&lt;T&gt; 通用加速比较
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static unsafe bool SequenceEqual_VectorT(byte* p1, byte* p2, int len)
    {
        

        // 1. 针对 32 字节以下直接走 ShortPath (已优化为无分支版本)
        if (len <= 32) return SequenceEqual_ShortPath(p1, p2, len);

        // x64 上运行，这个值会是 32
        int vCount = Vector<byte>.Count;

        // 如果是 ARM64 (vCount=16)，我们依然希望 17-31 字节走高效的 Overlapping
        if (len <= 32) return SequenceEqual_ShortPath(p1, p2, len);

        // 3. 处理 [33, vCount-1] 之间的尴尬区间 (使用 Vector128 Overlapping)
        // 相比原版的 long 比较，这里一次性覆盖 16 字节，效率翻倍
        if (len < vCount)
        {
            // 这里的逻辑类似 IndexOf 中的 Vector128 路径
            if (Unsafe.ReadUnaligned<Vector128<byte>>(p1) != Unsafe.ReadUnaligned<Vector128<byte>>(p2)) return false;
            return Unsafe.ReadUnaligned<Vector128<byte>>(p1 + len - 16) == Unsafe.ReadUnaligned<Vector128<byte>>(p2 + len - 16);
        }

        // 4. SIMD 主循环：双路展开 (Unrolled Loop)
        int i = 0;
        int dualVCount = vCount * 2;
        while (i <= len - dualVCount)
        {
            var va0 = Unsafe.ReadUnaligned<Vector<byte>>(p1 + i);
            var vb0 = Unsafe.ReadUnaligned<Vector<byte>>(p2 + i);
            var va1 = Unsafe.ReadUnaligned<Vector<byte>>(p1 + i + vCount);
            var vb1 = Unsafe.ReadUnaligned<Vector<byte>>(p2 + i + vCount);

            // 利用 bitwise OR 快速合并检查，减少分支跳转
            if (!Vector.EqualsAll(va0, vb0) || !Vector.EqualsAll(va1, vb1)) return false;

            i += dualVCount;
        }

        // 5. 尾部处理：Single Overlapping
        // 无论剩下多少 (即使只有 1 字节未读)，直接对准末尾补一发全量 SIMD
        if (i < len)
        {
            var vTail1 = Unsafe.ReadUnaligned<Vector<byte>>(p1 + len - vCount);
            var vTail2 = Unsafe.ReadUnaligned<Vector<byte>>(p2 + len - vCount);
            return Vector.EqualsAll(vTail1, vTail2);
        }

        return true;
    }



    /// <summary>
    /// 随机单点取样比较，如果失败，则整个比较一定失败，可以快速返回 false
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="lengthBytes"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool SequenceEqual_MidpointPreCheck(byte* p1, byte* p2, int lengthBytes)
    {
        const int CHUNK_SIZE = 8; // 8 字节，即 sizeof(long)

        // 确保取样位置是 8 字节对齐的（& ~7）
        int midpointOffset = (lengthBytes / 2) & ~7;

        // 如果中点位置太靠近末尾，则调整到末尾 8 字节之前
        if (midpointOffset > lengthBytes - CHUNK_SIZE)
        {
            midpointOffset = lengthBytes - CHUNK_SIZE;
        }

        // 执行单次 8 字节比较
        if (*(long*)(p1 + midpointOffset) != *(long*)(p2 + midpointOffset))
        {
            // [早期失败] 找到了不相等，则整个集合确定不相等
            return false;
        }

        return true;
    }
}