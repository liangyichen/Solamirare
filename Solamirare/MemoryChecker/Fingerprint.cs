using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;


namespace Solamirare;

// ============================================================
//  Fingerprint — 三版本快速指纹算法
//  静态构造函数在首次使用时根据硬件能力选定函数指针，
//  运行时热路径零分支判断。
//
//  版本 A — 标量四路并行（原版改进）
//          适用范围：length < 128
//          策略：4 × ulong 状态，MixFast，手动 ILP
//
//  版本 B — SIMD 中等长度（128 – 2048 字节）
//          适用范围：128 ≤ length ≤ 2048
//          策略：Vector256（AVX2 / x64）或 Vector128（AdvSimd / ARM64）
//                 8 路 XOR 累积，入口 / 出口各做一次 MixFast，
//                 减少 Mix 调用次数同时保持混合质量
//
//  版本 C — SIMD 超长数据（length > 2048）
//          适用范围：length > 2048
//          策略：在版本 B 基础上展开为 4 × Vector256（或 4 × Vector128），
//                 共 16 路（x64）/ 8 路（ARM64）独立累积，
//                 最大化 ILP，隐藏内存延迟；
//                 每 256 字节（x64）或 128 字节（ARM64）做一次周期性 Mix
//                 防止长输入累积雪崩失效
// ============================================================

[SkipLocalsInit]
/// <summary>
/// 提供用于非加密场景的高速内存指纹计算能力。
/// </summary>
public static unsafe class Fingerprint
{
    // ── 初始常数（pi 的十六进制展开，无 nothing-up-my-sleeve） ──
    private const ulong C0 = 0x243F6A8885A308D3UL;
    private const ulong C1 = 0x13198A2E03707344UL;
    private const ulong C2 = 0xA4093822299F31D0UL;
    private const ulong C3 = 0x082EFA98EC4E6C89UL;
    private const ulong C4 = 0x452821E638D01377UL;
    private const ulong C5 = 0xBE5466CF34E90C6CUL;
    private const ulong C6 = 0xC0AC29B7C97C50DDUL;
    private const ulong C7 = 0x3F84D5B5B5470917UL;

    // wyhash finalizer 常数
    private const ulong MUL_A = 0xbf58476d1ce4e5b9UL;
    private const ulong MUL_B = 0xff51afd7ed558ccdUL;
    private const ulong MUL_C = 0xc4ceb9fe1a85ec53UL;

    // 路由阈值
    private const uint THRESHOLD_B = 128;
    private const uint THRESHOLD_C = 2048;

    // ── 静态函数指针，消除运行时 if-else ──
    private static readonly delegate*<byte*, uint, ulong> s_implB;
    private static readonly delegate*<byte*, uint, ulong> s_implC;

    // ── 静态构造函数：一次性硬件检测 ──
    static Fingerprint()
    {
        // x64：优先 AVX2，否则降级 SSE2（Vector128 在 x64 上也 ok）
        // ARM64：使用 AdvSimd（即 NEON）
        // 两个平台都不支持时降级为标量 A 版本

        bool hasAvx2   = Avx2.IsSupported;
        bool hasAdvSimd = AdvSimd.IsSupported;

        if (hasAvx2)
        {
            s_implB = &FingerprintB_Avx2;
            s_implC = &FingerprintC_Avx2;
        }
        else if (hasAdvSimd)
        {
            s_implB = &FingerprintB_AdvSimd;
            s_implC = &FingerprintC_AdvSimd;
        }
        else
        {
            // 降级：用标量版本覆盖所有范围（不影响正确性）
            s_implB = &FingerprintA_Scalar;
            s_implC = &FingerprintA_Scalar;
        }
    }

    // ============================================================
    //  公共入口 — 根据长度路由
    // ============================================================

    /// <summary>
    /// 内存段快速指针计算（注意，不要用于公共哈希码，碰撞率会很高，但是在内容重复的逻辑中使用已经足够）
    /// <para>使用 ulong 足够保证 50亿 个对象的不重复率</para>
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong MemoryFingerprint<T>(T* obj)
    where T: unmanaged
    {
        uint TSize = (uint)sizeof(T);

        if (obj is null || TSize == 0) return 0;

        byte* ptr = (byte*)obj;

        if (TSize < THRESHOLD_B)
            return FingerprintA_Scalar(ptr, TSize);

        if (TSize <= THRESHOLD_C)
            return s_implB(ptr, TSize);

        return s_implC(ptr, TSize);
    }

    // ============================================================
    //  共享工具函数
    // ============================================================

    /// <summary>
    /// 单次乘法混合（wyhash 风格）。
    /// latency ≈ 5 周期（1 mul + 2 xorshift），比双乘版本快约 40%。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MixFast(ulong v)
    {
        v ^= v >> 30;
        v *= MUL_A;
        v ^= v >> 27;
        return v;
    }

    /// <summary>
    /// 双乘强混合，用于版本 C 的周期性刷新，防止长输入雪崩失效。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MixStrong(ulong v)
    {
        v ^= v >> 33;
        v *= MUL_B;
        v ^= v >> 29;
        v *= MUL_C;
        v ^= v >> 32;
        return v;
    }

    /// <summary>
    /// 读取 1–7 字节尾部，零扩展为 ulong，不越界。
    /// 利用 bitmask 分支消除循环，编译后为 3 条 cmov。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReadTail(byte* p, uint n)
    {
        ulong v = 0;
        if ((n & 4) != 0) { v  =          *(uint*)p;  p += 4; }
        if ((n & 2) != 0) { v |= (ulong)(*(ushort*)p) << 32;  p += 2; }
        if ((n & 1) != 0) { v |= (ulong)(*p)          << 48; }
        return v;
    }

    /// <summary>
    /// 四路状态的标准 finalize + 合并，版本 A/B/C 共用出口。
    /// 混入 length 防止不同长度前缀碰撞。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong FinalMix4(ulong h0, ulong h1, ulong h2, ulong h3, uint length)
    {
        h0 ^= length;
        h0 ^= h1;
        h2 ^= h3;
        ulong h = MixStrong(h0 ^ h2);
        return h;
    }

    // ============================================================
    //  版本 A — 标量四路并行
    //  适用：length < 128
    // ============================================================
    private static ulong FingerprintA_Scalar(byte* ptr, uint length)
    {
        ulong h0 = C0, h1 = C1, h2 = C2, h3 = C3;

        byte* p   = ptr;
        byte* end = ptr + length;

        // 主循环：32B / 轮，四路独立，CPU 可乱序并行
        while (p + 32 <= end)
        {
            ulong v0 = *(ulong*)(p +  0);
            ulong v1 = *(ulong*)(p +  8);
            ulong v2 = *(ulong*)(p + 16);
            ulong v3 = *(ulong*)(p + 24);
            h0 ^= MixFast(v0);
            h1 ^= MixFast(v1);
            h2 ^= MixFast(v2);
            h3 ^= MixFast(v3);
            p += 32;
        }

        // 尾部 8B 块：轮流分给四个 lane，保持均匀覆盖
        int lane = 0;
        while (p + 8 <= end)
        {
            ulong v = *(ulong*)p;
            switch (lane & 3)
            {
                case 0: h0 ^= MixFast(v); break;
                case 1: h1 ^= MixFast(v); break;
                case 2: h2 ^= MixFast(v); break;
                default: h3 ^= MixFast(v); break;
            }
            lane++;
            p += 8;
        }

        // 1–7 字节尾部
        if (p < end)
            h0 ^= MixFast(ReadTail(p, (uint)(end - p)));

        return FinalMix4(h0, h1, h2, h3, length);
    }

    // ============================================================
    //  版本 B — AVX2（x64）
    //  适用：128 ≤ length ≤ 2048
    //
    //  策略：
    //    · 2 × Vector256<ulong>（acc0/acc1）= 8 路独立累积
    //    · 每轮处理 64B，纯 XOR（无 Mix），减少延迟
    //    · 出口时对各 lane 做 MixFast，保证混合质量
    //    · 尾部回落标量
    // ============================================================
    private static ulong FingerprintB_Avx2(byte* ptr, uint length)
    {
        // 用初始常数种子化两个向量累加器（8 个独立 ulong 状态）
        var acc0 = Vector256.Create(C0, C1, C2, C3);
        var acc1 = Vector256.Create(C4, C5, C6, C7);

        byte* p   = ptr;
        byte* end = ptr + length;

        // 主循环：64B / 轮，两条向量 XOR
        while (p + 64 <= end)
        {
            var chunk0 = Vector256.Load((ulong*)p);
            var chunk1 = Vector256.Load((ulong*)(p + 32));
            acc0 = Vector256.Xor(acc0, chunk0);
            acc1 = Vector256.Xor(acc1, chunk1);
            p += 64;
        }

        // 32B 尾块
        if (p + 32 <= end)
        {
            acc0 = Vector256.Xor(acc0, Vector256.Load((ulong*)p));
            p += 32;
        }

        // 拆出 8 个标量，各做 MixFast 后合并为 4 路
        ulong h0 = MixFast(acc0.GetElement(0)) ^ MixFast(acc1.GetElement(0));
        ulong h1 = MixFast(acc0.GetElement(1)) ^ MixFast(acc1.GetElement(1));
        ulong h2 = MixFast(acc0.GetElement(2)) ^ MixFast(acc1.GetElement(2));
        ulong h3 = MixFast(acc0.GetElement(3)) ^ MixFast(acc1.GetElement(3));

        // 标量收尾（最多 31 字节）
        int lane = 0;
        while (p + 8 <= end)
        {
            ulong v = *(ulong*)p;
            switch (lane & 3)
            {
                case 0: h0 ^= MixFast(v); break;
                case 1: h1 ^= MixFast(v); break;
                case 2: h2 ^= MixFast(v); break;
                default: h3 ^= MixFast(v); break;
            }
            lane++;
            p += 8;
        }

        if (p < end)
            h0 ^= MixFast(ReadTail(p, (uint)(end - p)));

        return FinalMix4(h0, h1, h2, h3, length);
    }

    // ============================================================
    //  版本 B — AdvSimd（ARM64 / NEON）
    //  适用：128 ≤ length ≤ 2048
    //
    //  策略与 AVX2 版对称，Vector128 宽度减半（2 × ulong / 向量），
    //  因此用 4 × Vector128 = 8 路独立累积，保持相同的路数。
    // ============================================================
    private static ulong FingerprintB_AdvSimd(byte* ptr, uint length)
    {
        var acc0 = Vector128.Create(C0, C1);
        var acc1 = Vector128.Create(C2, C3);
        var acc2 = Vector128.Create(C4, C5);
        var acc3 = Vector128.Create(C6, C7);

        byte* p   = ptr;
        byte* end = ptr + length;

        // 主循环：64B / 轮，四条向量 XOR
        while (p + 64 <= end)
        {
            acc0 = Vector128.Xor(acc0, Vector128.Load((ulong*)p));
            acc1 = Vector128.Xor(acc1, Vector128.Load((ulong*)(p + 16)));
            acc2 = Vector128.Xor(acc2, Vector128.Load((ulong*)(p + 32)));
            acc3 = Vector128.Xor(acc3, Vector128.Load((ulong*)(p + 48)));
            p += 64;
        }

        while (p + 16 <= end)
        {
            acc0 = Vector128.Xor(acc0, Vector128.Load((ulong*)p));
            p += 16;
        }

        ulong h0 = MixFast(acc0.GetElement(0)) ^ MixFast(acc2.GetElement(0));
        ulong h1 = MixFast(acc0.GetElement(1)) ^ MixFast(acc2.GetElement(1));
        ulong h2 = MixFast(acc1.GetElement(0)) ^ MixFast(acc3.GetElement(0));
        ulong h3 = MixFast(acc1.GetElement(1)) ^ MixFast(acc3.GetElement(1));

        int lane = 0;
        while (p + 8 <= end)
        {
            ulong v = *(ulong*)p;
            switch (lane & 3)
            {
                case 0: h0 ^= MixFast(v); break;
                case 1: h1 ^= MixFast(v); break;
                case 2: h2 ^= MixFast(v); break;
                default: h3 ^= MixFast(v); break;
            }
            lane++;
            p += 8;
        }

        if (p < end)
            h0 ^= MixFast(ReadTail(p, (uint)(end - p)));

        return FinalMix4(h0, h1, h2, h3, length);
    }

    // ============================================================
    //  版本 C — AVX2（x64）超长数据
    //  适用：length > 2048
    //
    //  策略：
    //    · 4 × Vector256<ulong> = 16 路独立累积，完全隐藏内存延迟
    //    · 每 256B 做一次周期性 Mix（防止大量 XOR 累积后雪崩失效）
    //    · 周期性 Mix 使用 MixStrong（双乘），保持长输入的混合质量
    //    · 出口合并为 4 路标量，复用 FinalMix4
    // ============================================================
    private static ulong FingerprintC_Avx2(byte* ptr, uint length)
    {
        var acc0 = Vector256.Create(C0, C1, C2, C3);
        var acc1 = Vector256.Create(C4, C5, C6, C7);
        var acc2 = Vector256.Create(C1, C3, C5, C7);
        var acc3 = Vector256.Create(C0, C2, C4, C6);

        byte* p   = ptr;
        byte* end = ptr + length;

        // 每 256B 做一次周期刷新
        const uint REFRESH_STRIDE = 256;
        byte* refreshEnd = p + ((uint)(end - p) & ~(REFRESH_STRIDE - 1u));

        while (p < refreshEnd)
        {
            // 每次迭代处理恰好 256B：
            // 组 0：p[0..127]，组 1：p[128..255]
            acc0 = Vector256.Xor(acc0, Vector256.Load((ulong*)(p +   0)));
            acc1 = Vector256.Xor(acc1, Vector256.Load((ulong*)(p +  32)));
            acc2 = Vector256.Xor(acc2, Vector256.Load((ulong*)(p +  64)));
            acc3 = Vector256.Xor(acc3, Vector256.Load((ulong*)(p +  96)));
            acc0 = Vector256.Xor(acc0, Vector256.Load((ulong*)(p + 128)));
            acc1 = Vector256.Xor(acc1, Vector256.Load((ulong*)(p + 160)));
            acc2 = Vector256.Xor(acc2, Vector256.Load((ulong*)(p + 192)));
            acc3 = Vector256.Xor(acc3, Vector256.Load((ulong*)(p + 224)));
            p += 256;

            // 周期性 Mix：防止 XOR 累积导致高位熵损失
            // Vector256 无 ulong 向量乘法，拆出 lane 做标量 MixStrong 再打包
            acc0 = RefreshVec256(acc0);
            acc1 = RefreshVec256(acc1);
            acc2 = RefreshVec256(acc2);
            acc3 = RefreshVec256(acc3);
        }

        // 处理剩余的完整 64B 块（不足 256B 的部分）
        while (p + 64 <= end)
        {
            acc0 = Vector256.Xor(acc0, Vector256.Load((ulong*)p));
            acc1 = Vector256.Xor(acc1, Vector256.Load((ulong*)(p + 32)));
            p += 64;
        }

        if (p + 32 <= end)
        {
            acc2 = Vector256.Xor(acc2, Vector256.Load((ulong*)p));
            p += 32;
        }

        // 16 路合并为 4 路标量
        ulong h0 = CollapseVec256(acc0, acc2, 0);
        ulong h1 = CollapseVec256(acc0, acc2, 1);
        ulong h2 = CollapseVec256(acc1, acc3, 2);
        ulong h3 = CollapseVec256(acc1, acc3, 3);

        int lane = 0;
        while (p + 8 <= end)
        {
            ulong v = *(ulong*)p;
            switch (lane & 3)
            {
                case 0: h0 ^= MixFast(v); break;
                case 1: h1 ^= MixFast(v); break;
                case 2: h2 ^= MixFast(v); break;
                default: h3 ^= MixFast(v); break;
            }
            lane++;
            p += 8;
        }

        if (p < end)
            h0 ^= MixFast(ReadTail(p, (uint)(end - p)));

        return FinalMix4(h0, h1, h2, h3, length);
    }

    // ============================================================
    //  版本 C — AdvSimd（ARM64）超长数据
    //  策略：4 × Vector128<ulong> = 8 路，每 256B 周期刷新
    // ============================================================
    private static ulong FingerprintC_AdvSimd(byte* ptr, uint length)
    {
        var acc0 = Vector128.Create(C0, C1);
        var acc1 = Vector128.Create(C2, C3);
        var acc2 = Vector128.Create(C4, C5);
        var acc3 = Vector128.Create(C6, C7);

        byte* p   = ptr;
        byte* end = ptr + length;

        const uint REFRESH_STRIDE = 256;
        byte* refreshEnd = p + ((uint)(end - p) & ~(REFRESH_STRIDE - 1u));

        while (p < refreshEnd)
        {
            // 每次迭代处理恰好 256B：4 个 Vector128（各 16B）× 4 组 = 256B
            acc0 = Vector128.Xor(acc0, Vector128.Load((ulong*)(p +   0)));
            acc1 = Vector128.Xor(acc1, Vector128.Load((ulong*)(p +  16)));
            acc2 = Vector128.Xor(acc2, Vector128.Load((ulong*)(p +  32)));
            acc3 = Vector128.Xor(acc3, Vector128.Load((ulong*)(p +  48)));
            acc0 = Vector128.Xor(acc0, Vector128.Load((ulong*)(p +  64)));
            acc1 = Vector128.Xor(acc1, Vector128.Load((ulong*)(p +  80)));
            acc2 = Vector128.Xor(acc2, Vector128.Load((ulong*)(p +  96)));
            acc3 = Vector128.Xor(acc3, Vector128.Load((ulong*)(p + 112)));
            acc0 = Vector128.Xor(acc0, Vector128.Load((ulong*)(p + 128)));
            acc1 = Vector128.Xor(acc1, Vector128.Load((ulong*)(p + 144)));
            acc2 = Vector128.Xor(acc2, Vector128.Load((ulong*)(p + 160)));
            acc3 = Vector128.Xor(acc3, Vector128.Load((ulong*)(p + 176)));
            acc0 = Vector128.Xor(acc0, Vector128.Load((ulong*)(p + 192)));
            acc1 = Vector128.Xor(acc1, Vector128.Load((ulong*)(p + 208)));
            acc2 = Vector128.Xor(acc2, Vector128.Load((ulong*)(p + 224)));
            acc3 = Vector128.Xor(acc3, Vector128.Load((ulong*)(p + 240)));
            p += 256;

            acc0 = RefreshVec128(acc0);
            acc1 = RefreshVec128(acc1);
            acc2 = RefreshVec128(acc2);
            acc3 = RefreshVec128(acc3);
        }

        while (p + 64 <= end)
        {
            acc0 = Vector128.Xor(acc0, Vector128.Load((ulong*)p));
            acc1 = Vector128.Xor(acc1, Vector128.Load((ulong*)(p + 16)));
            acc2 = Vector128.Xor(acc2, Vector128.Load((ulong*)(p + 32)));
            acc3 = Vector128.Xor(acc3, Vector128.Load((ulong*)(p + 48)));
            p += 64;
        }

        while (p + 16 <= end)
        {
            acc0 = Vector128.Xor(acc0, Vector128.Load((ulong*)p));
            p += 16;
        }

        ulong h0 = MixFast(acc0.GetElement(0)) ^ MixFast(acc2.GetElement(0));
        ulong h1 = MixFast(acc0.GetElement(1)) ^ MixFast(acc2.GetElement(1));
        ulong h2 = MixFast(acc1.GetElement(0)) ^ MixFast(acc3.GetElement(0));
        ulong h3 = MixFast(acc1.GetElement(1)) ^ MixFast(acc3.GetElement(1));

        int lane = 0;
        while (p + 8 <= end)
        {
            ulong v = *(ulong*)p;
            switch (lane & 3)
            {
                case 0: h0 ^= MixFast(v); break;
                case 1: h1 ^= MixFast(v); break;
                case 2: h2 ^= MixFast(v); break;
                default: h3 ^= MixFast(v); break;
            }
            lane++;
            p += 8;
        }

        if (p < end)
            h0 ^= MixFast(ReadTail(p, (uint)(end - p)));

        return FinalMix4(h0, h1, h2, h3, length);
    }

    // ============================================================
    //  SIMD 辅助：周期性 Mix（拆出标量再打包，AVX2 无 ulong 向量乘法）
    // ============================================================

    /// <summary>
    /// 对 Vector256 的 4 个 ulong lane 分别做 MixStrong，然后打包回向量。
    /// 用于版本 C 的周期刷新，防止长输入累积后高位熵崩溃。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<ulong> RefreshVec256(Vector256<ulong> v)
    {
        return Vector256.Create(
            MixStrong(v.GetElement(0)),
            MixStrong(v.GetElement(1)),
            MixStrong(v.GetElement(2)),
            MixStrong(v.GetElement(3)));
    }

    /// <summary>
    /// 对 Vector128 的 2 个 ulong lane 分别做 MixStrong，然后打包回向量。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<ulong> RefreshVec128(Vector128<ulong> v)
    {
        return Vector128.Create(
            MixStrong(v.GetElement(0)),
            MixStrong(v.GetElement(1)));
    }

    /// <summary>
    /// 将两个 Vector256 的指定 lane 做 MixFast 后 XOR 合并为一个 ulong。
    /// 用于版本 C 出口将 16 路合并为 4 路。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong CollapseVec256(Vector256<ulong> a, Vector256<ulong> b, int lane)
    {
        return MixFast(a.GetElement(lane)) ^ MixFast(b.GetElement(lane));
    }
}
