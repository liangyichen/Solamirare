namespace Solamirare;

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;


/// <summary>
/// 大小写不敏感的序列搜索扩展方法。
/// 本文件为 ValueTypeHelper 的 partial 扩展，提供以下方法：
///
///   char 版（UnManagedCollection&lt;char&gt;）：
///     StartIndexOf  — 检查 source 是否以 pattern 开头（bool）
///     EndsOf        — 检查 source 是否以 pattern 结尾（bool）
///     IndexOf       — 在 source 中正向查找 pattern，返回首次匹配的字符索引，未找到返回 -1
///     LastIndexOf   — 在 source 中反向查找 pattern，返回最后匹配的字符索引，未找到返回 -1
///
///   UTF-8 版（UnManagedCollection&lt;byte&gt; 存储 UTF-8 字节序列）：
///     StartIndexOf  — 同 char 版语义，比对 UTF-8 字节流
///     EndsOf        — 同 char 版语义，比对 UTF-8 字节流
///     IndexOf       — 正向查找，返回首次匹配位置的 Unicode code point 索引（以 code point 计数）
///     LastIndexOf   — 反向查找，返回最后匹配位置的 Unicode code point 索引
///
/// 大小写折叠规则（两个版本均相同）：
///   · 仅对 ASCII 字母（0x41–0x5A / 0x61–0x7A）做大小写折叠（| 0x20）。
///   · 非 ASCII 字符（UTF-8 多字节序列）要求字节流完全相同，不做额外折叠。
///   · 与 SequenceEqualIgnoreCase 的逻辑完全一致，可组合使用。
///
/// 核心内部原语：
///   MatchesAt_Char   — 在指定偏移处比较 charLen 个 char，大小写不敏感
///   MatchesAt_Utf8   — 在指定字节偏移处比较 patLen 字节，ASCII 折叠 + 多字节精确匹配
///   Utf8CharCount    — 计算字节范围 [0, byteOffset) 内的 Unicode code point 数量
///
/// <para>
/// Case-insensitive sequence search extension methods.
/// This file is a partial extension of ValueTypeHelper, providing:
///
///   char version (UnManagedCollection&lt;char&gt;):
///     StartIndexOf  — returns bool: does source start with pattern?
///     EndsOf        — returns bool: does source end with pattern?
///     IndexOf       — forward search; returns char index of first match, -1 if not found
///     LastIndexOf   — backward search; returns char index of last match, -1 if not found
///
///   UTF-8 version (UnManagedCollection&lt;byte&gt; holding a UTF-8 byte sequence):
///     StartIndexOf  — same semantics as char version, comparing raw UTF-8 bytes
///     EndsOf        — same semantics as char version, comparing raw UTF-8 bytes
///     IndexOf       — forward search; returns Unicode code point index of first match
///     LastIndexOf   — backward search; returns Unicode code point index of last match
///
/// Case-folding rules (same for both versions):
///   · Only ASCII letters (0x41–0x5A / 0x61–0x7A) are folded via | 0x20.
///   · Non-ASCII bytes (UTF-8 multi-byte sequences) require exact byte equality.
///   · Consistent with SequenceEqualIgnoreCase; designed to be composed with it.
/// </para>
/// </summary>
public unsafe static partial class ValueTypeHelper
{
    // ══════════════════════════════════════════════════════════════════════
    // 内部原语 — char 版
    // Internal primitives — char version
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 在 <paramref name="src"/> 的字符偏移 <paramref name="srcOffset"/> 处，
    /// 与 <paramref name="pat"/> 做大小写不敏感的 <paramref name="charLen"/> 字符比较。
    /// 直接复用 SequenceEqualIgnoreCase(char*, char*, int) 的完整 SIMD 路径。
    /// <para>
    /// Compares <paramref name="charLen"/> chars at <paramref name="srcOffset"/> in
    /// <paramref name="src"/> against <paramref name="pat"/>, case-insensitively.
    /// Delegates to the full SIMD path of SequenceEqualIgnoreCase(char*, char*, int).
    /// </para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MatchesAt_Char(char* src, int srcOffset, char* pat, int charLen)
        => SequenceEqualIgnoreCase(src + srcOffset, pat, charLen);

    // ══════════════════════════════════════════════════════════════════════
    // 内部原语 — UTF-8 版
    // Internal primitives — UTF-8 version
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 在 <paramref name="src"/> 的字节偏移 <paramref name="srcByteOffset"/> 处，
    /// 与 <paramref name="pat"/> 做大小写不敏感的 <paramref name="patLen"/> 字节比较。
    ///
    /// 折叠规则：
    ///   · ASCII 字节（&lt; 0x80）且为字母（a-z / A-Z）：| 0x20 折叠后比较。
    ///   · 其他所有字节（非 ASCII 多字节序列的组成字节）：要求精确相等。
    ///   · SIMD 路径直接复用 SequenceEqualIgnoreCase(byte*, byte*, int)。
    ///
    /// <para>
    /// Compares <paramref name="patLen"/> bytes at byte offset
    /// <paramref name="srcByteOffset"/> in <paramref name="src"/> against
    /// <paramref name="pat"/>, using ASCII-only case folding.
    /// Non-ASCII bytes require exact equality. Delegates to the SIMD path of
    /// SequenceEqualIgnoreCase(byte*, byte*, int).
    /// </para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MatchesAt_Utf8(byte* src, int srcByteOffset, byte* pat, int patLen)
        => SequenceEqualIgnoreCase(src + srcByteOffset, pat, patLen);

    /// <summary>
    /// 计算 UTF-8 字节流 <paramref name="p"/> 中前 <paramref name="byteCount"/>
    /// 字节所包含的 Unicode code point 数量。
    ///
    /// 计数规则（标准 UTF-8 前导字节判断）：
    ///   · 0xxxxxxx（&lt; 0x80）：单字节 code point，计 1。
    ///   · 11xxxxxx（≥ 0xC0）：多字节序列的起始字节，计 1。
    ///   · 10xxxxxx（0x80–0xBF）：延续字节，不计入 code point 数。
    ///
    /// 时间复杂度 O(byteCount)，在实际场景（pattern 较短）下开销可接受。
    ///
    /// <para>
    /// Counts the number of Unicode code points in the first
    /// <paramref name="byteCount"/> bytes of the UTF-8 stream <paramref name="p"/>.
    ///
    /// Counting rule:
    ///   · 0xxxxxxx (&lt; 0x80): single-byte code point, count 1.
    ///   · 11xxxxxx (≥ 0xC0): leading byte of multi-byte sequence, count 1.
    ///   · 10xxxxxx (0x80–0xBF): continuation byte, not counted.
    /// </para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Utf8CharCount(byte* p, int byteCount)
    {
        int count = 0;
        for (int i = 0; i < byteCount; i++)
        {
            // 延续字节（10xxxxxx）不是 code point 的起始位置，跳过不计。
            // Continuation bytes (10xxxxxx) do not start a code point; skip them.
            byte b = p[i];
            if ((b & 0xC0) != 0x80) count++;
        }
        return count;
    }

    /// <summary>
    /// 计算 UTF-8 字节流 <paramref name="p"/> 中第 <paramref name="byteEnd"/> 字节之前，
    /// 从字节偏移 <paramref name="byteStart"/> 开始的 code point 数量。
    /// 即：Utf8CharCount(p + byteStart, byteEnd - byteStart)。
    /// <para>
    /// Counts code points between byte offsets [byteStart, byteEnd) in the UTF-8
    /// stream <paramref name="p"/>; equivalent to
    /// Utf8CharCount(p + byteStart, byteEnd - byteStart).
    /// </para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Utf8CharCount(byte* p, int byteStart, int byteEnd)
        => Utf8CharCount(p + byteStart, byteEnd - byteStart);

    /// <summary>
    /// 将字节偏移 <paramref name="byteOffset"/> 转换为从流首到该位置的
    /// Unicode code point 索引（0-based）。
    /// <para>
    /// Converts a byte offset to a zero-based Unicode code point index
    /// measured from the start of the UTF-8 stream.
    /// </para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Utf8ByteOffsetToCharIndex(byte* p, int byteOffset)
        => Utf8CharCount(p, byteOffset);

    // ══════════════════════════════════════════════════════════════════════
    // char 版 — StartIndexOf / EndsOf / IndexOf / LastIndexOf
    // char version — StartIndexOf / EndsOf / IndexOf / LastIndexOf
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 检查 <paramref name="source"/> 是否以 <paramref name="pattern"/> 开头（大小写不敏感）。
    ///
    /// 实现：长度检查 + 对前 patternLen 个字符调用 SequenceEqualIgnoreCase SIMD 路径。
    /// 时间复杂度 O(patternLen)，空间复杂度 O(1)。
    ///
    /// <para>
    /// Returns true if <paramref name="source"/> starts with <paramref name="pattern"/>,
    /// case-insensitively.
    /// Implementation: length check + SIMD SequenceEqualIgnoreCase on the first
    /// patternLen chars. O(patternLen) time, O(1) space.
    /// </para>
    /// </summary>
    /// <param name="source">被搜索的字符集合。/ The collection being searched.</param>
    /// <param name="pattern">要匹配的前缀序列。/ The prefix sequence to match.</param>
    /// <returns>source 以 pattern 开头时返回 true。/ true if source starts with pattern.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartWithIgnoreCase(
        in UnManagedCollection<char> source,
        in UnManagedCollection<char> pattern)
    {
        // pattern 为空：按惯例任何字符串都以空串开头。
        // An empty pattern matches the start of any source by convention.
        if (pattern.IsEmpty) return true;

        int srcLen = (int)source.Size;
        int patLen = (int)pattern.Size;

        // source 比 pattern 短，不可能匹配。
        // Source shorter than pattern: cannot match.
        if (srcLen < patLen) return false;

        return MatchesAt_Char(source.InternalPointer, 0, pattern.InternalPointer, patLen);
    }

    /// <summary>
    /// 检查 <paramref name="source"/> 是否以 <paramref name="pattern"/> 结尾（大小写不敏感）。
    ///
    /// 实现：长度检查 + 对末尾 patternLen 个字符调用 SequenceEqualIgnoreCase SIMD 路径。
    /// 时间复杂度 O(patternLen)，空间复杂度 O(1)。
    ///
    /// <para>
    /// Returns true if <paramref name="source"/> ends with <paramref name="pattern"/>,
    /// case-insensitively.
    /// Implementation: length check + SIMD SequenceEqualIgnoreCase on the last
    /// patternLen chars. O(patternLen) time, O(1) space.
    /// </para>
    /// </summary>
    /// <param name="source">被搜索的字符集合。/ The collection being searched.</param>
    /// <param name="pattern">要匹配的后缀序列。/ The suffix sequence to match.</param>
    /// <returns>source 以 pattern 结尾时返回 true。/ true if source ends with pattern.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsWithIgnoreCase(
        in UnManagedCollection<char> source,
        in UnManagedCollection<char> pattern)
    {
        if (pattern.IsEmpty) return true;

        int srcLen = (int)source.Size;
        int patLen = (int)pattern.Size;

        if (srcLen < patLen) return false;

        // 从 source 末尾向前 patLen 个字符处开始比较。
        // Compare starting at (srcLen - patLen) from the end of source.
        int startOffset = srcLen - patLen;
        return MatchesAt_Char(source.InternalPointer, startOffset, pattern.InternalPointer, patLen);
    }

    /// <summary>
    /// 在 <paramref name="source"/> 中正向查找 <paramref name="pattern"/> 首次出现的位置
    /// （大小写不敏感）。
    ///
    /// 算法：朴素滑动窗口，窗口宽度为 patLen，每个起始位置调用 SIMD SequenceEqualIgnoreCase。
    /// 最坏时间复杂度 O(srcLen × patLen)，对短 pattern（HTTP Header 场景）实际接近 O(srcLen)。
    /// 如需处理极长重复模式，可改用 BM/KMP，但对本项目场景不必要。
    ///
    /// <para>
    /// Forward search for the first occurrence of <paramref name="pattern"/> in
    /// <paramref name="source"/>, case-insensitively.
    /// Algorithm: naive sliding window of width patLen; each position calls the SIMD
    /// SequenceEqualIgnoreCase. Worst case O(srcLen × patLen); in practice O(srcLen)
    /// for short patterns typical in HTTP header parsing.
    /// </para>
    /// </summary>
    /// <param name="source">被搜索的字符集合。/ The collection being searched.</param>
    /// <param name="pattern">要查找的子序列。/ The subsequence to find.</param>
    /// <returns>
    /// 首次匹配的字符索引（0-based），未找到返回 -1。
    /// / Zero-based char index of the first match, or -1 if not found.
    /// </returns>
    public static int IndexOfIgnoreCase(
        in UnManagedCollection<char> source,
        in UnManagedCollection<char> pattern)
    {
        if (pattern.IsEmpty) return 0;

        int srcLen = (int)source.Size;
        int patLen = (int)pattern.Size;

        if (srcLen < patLen) return -1;

        char* src = source.InternalPointer;
        char* pat = pattern.InternalPointer;

        // 快速路径：pattern 长度为 1，只需逐字符比较，避免函数调用开销。
        // Fast path: single-char pattern — compare char-by-char without call overhead.
        if (patLen == 1)
        {
            char p0 = pat[0];
            // p0_lower：pattern 首字符的小写形式（仅对 ASCII 字母有意义）。
            // p0_lower: lowercase form of the first pattern char (meaningful for ASCII letters only).
            char p0_lower = (char)(p0 | 0x20);
            bool p0_is_alpha = p0_lower >= 'a' && p0_lower <= 'z';

            for (int i = 0; i < srcLen; i++)
            {
                char s = src[i];
                if (s == p0) return i;
                if (p0_is_alpha && (char)(s | 0x20) == p0_lower) return i;
            }
            return -1;
        }

        // 一般路径：滑动窗口 + SIMD 比较。
        // General path: sliding window + SIMD compare.
        // 先用首字符过滤，避免对每个位置都调用完整 SIMD 比较。
        // Pre-filter by first char to avoid a full SIMD call at every position.
        char first = pat[0];
        char first_low = (char)(first | 0x20);
        bool first_alpha = first_low >= 'a' && first_low <= 'z';
        int limit = srcLen - patLen; // 最后一个合法起始位置 / last valid start index

        for (int i = 0; i <= limit; i++)
        {
            char s = src[i];
            // 首字符不匹配：快速跳过，不进入 SIMD 比较。
            // First char mismatch: skip immediately without entering SIMD.
            bool firstMatch = (s == first) || (first_alpha && (char)(s | 0x20) == first_low);
            if (!firstMatch) continue;

            // 首字符匹配：用 SIMD 比较剩余 patLen-1 个字符。
            // First char matched: use SIMD to compare the remaining patLen-1 chars.
            if (MatchesAt_Char(src, i, pat, patLen)) return i;
        }
        return -1;
    }

    /// <summary>
    /// 在 <paramref name="source"/> 中反向查找 <paramref name="pattern"/> 最后一次出现的位置
    /// （大小写不敏感）。
    ///
    /// 算法：从末尾向前滑动，与 IndexOf 对称，同样使用首字符过滤 + SIMD 比较。
    ///
    /// <para>
    /// Backward search for the last occurrence of <paramref name="pattern"/> in
    /// <paramref name="source"/>, case-insensitively.
    /// Algorithm: sliding window from the end toward the start; mirrors IndexOf with
    /// the same first-char filter + SIMD compare.
    /// </para>
    /// </summary>
    /// <param name="source">被搜索的字符集合。/ The collection being searched.</param>
    /// <param name="pattern">要查找的子序列。/ The subsequence to find.</param>
    /// <returns>
    /// 最后匹配的字符索引（0-based），未找到返回 -1。
    /// / Zero-based char index of the last match, or -1 if not found.
    /// </returns>
    public static int LastIndexOfIgnoreCase(
        in UnManagedCollection<char> source,
        in UnManagedCollection<char> pattern)
    {
        if (pattern.IsEmpty) return (int)source.Size;

        int srcLen = (int)source.Size;
        int patLen = (int)pattern.Size;

        if (srcLen < patLen) return -1;

        char* src = source.InternalPointer;
        char* pat = pattern.InternalPointer;

        // 快速路径：pattern 长度为 1。
        // Fast path: single-char pattern.
        if (patLen == 1)
        {
            char p0 = pat[0];
            char p0_lower = (char)(p0 | 0x20);
            bool p0_alpha = p0_lower >= 'a' && p0_lower <= 'z';

            for (int i = srcLen - 1; i >= 0; i--)
            {
                char s = src[i];
                if (s == p0) return i;
                if (p0_alpha && (char)(s | 0x20) == p0_lower) return i;
            }
            return -1;
        }

        // 一般路径：从末尾向前滑动。
        // General path: slide from end toward start.
        char first = pat[0];
        char first_low = (char)(first | 0x20);
        bool first_alpha = first_low >= 'a' && first_low <= 'z';

        for (int i = srcLen - patLen; i >= 0; i--)
        {
            char s = src[i];
            bool firstMatch = (s == first) || (first_alpha && (char)(s | 0x20) == first_low);
            if (!firstMatch) continue;

            if (MatchesAt_Char(src, i, pat, patLen)) return i;
        }
        return -1;
    }
    

    /// <summary>
    /// 检查 UTF-8 字节序列 <paramref name="source"/> 是否以
    /// UTF-8 字节序列 <paramref name="pattern"/> 开头（ASCII 大小写不敏感）。
    ///
    /// 实现：字节长度检查 + 对 source 前 patLen 字节调用 SequenceEqualIgnoreCase(byte*) SIMD 路径。
    /// 时间复杂度 O(patLen)。
    ///
    /// <para>
    /// Returns true if the UTF-8 byte sequence <paramref name="source"/> starts with
    /// the UTF-8 byte sequence <paramref name="pattern"/>, using ASCII-only case folding.
    /// Implementation: byte-length check + SIMD SequenceEqualIgnoreCase(byte*) on the
    /// first patLen bytes. O(patLen) time.
    /// </para>
    /// </summary>
    /// <param name="source">被搜索的 UTF-8 字节集合。/ The UTF-8 byte collection being searched.</param>
    /// <param name="pattern">要匹配的 UTF-8 前缀。/ The UTF-8 prefix to match.</param>
    /// <returns>source 以 pattern 开头时返回 true。/ true if source starts with pattern.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartWithIgnoreCase(
        in UnManagedCollection<byte> source,
        in UnManagedCollection<byte> pattern)
    {
        fixed(UnManagedCollection<byte>* p_source = &source)
        fixed(UnManagedCollection<byte>* p_pattern = &pattern)

        return StartWithIgnoreCase(p_source, p_pattern);
    }




    /// <summary>
    /// 检查 UTF-8 字节序列 <paramref name="source"/> 是否以
    /// UTF-8 字节序列 <paramref name="pattern"/> 开头（ASCII 大小写不敏感）。
    ///
    /// 实现：字节长度检查 + 对 source 前 patLen 字节调用 SequenceEqualIgnoreCase(byte*) SIMD 路径。
    /// 时间复杂度 O(patLen)。
    ///
    /// <para>
    /// Returns true if the UTF-8 byte sequence <paramref name="source"/> starts with
    /// the UTF-8 byte sequence <paramref name="pattern"/>, using ASCII-only case folding.
    /// Implementation: byte-length check + SIMD SequenceEqualIgnoreCase(byte*) on the
    /// first patLen bytes. O(patLen) time.
    /// </para>
    /// </summary>
    /// <param name="source">被搜索的 UTF-8 字节集合。/ The UTF-8 byte collection being searched.</param>
    /// <param name="pattern">要匹配的 UTF-8 前缀。/ The UTF-8 prefix to match.</param>
    /// <returns>source 以 pattern 开头时返回 true。/ true if source starts with pattern.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartWithIgnoreCase(
        UnManagedCollection<byte>* source,
        UnManagedCollection<byte>* pattern)
    {

        if (source is null || source->IsEmpty) return false;

        if (pattern is null || pattern->IsEmpty) return true;

        int srcLen = (int)source->Size;

        int patLen = (int)pattern->Size;

        if (srcLen < patLen) return false;

        return MatchesAt_Utf8(source->InternalPointer, 0, pattern->InternalPointer, patLen);
    }

    /// <summary>
    /// 检查 UTF-8 字节序列 <paramref name="source"/> 是否以
    /// UTF-8 字节序列 <paramref name="pattern"/> 结尾（ASCII 大小写不敏感）。
    ///
    /// 实现：字节长度检查 + 对 source 末尾 patLen 字节调用 SequenceEqualIgnoreCase(byte*) SIMD 路径。
    /// 时间复杂度 O(patLen)。
    ///
    /// <para>
    /// Returns true if the UTF-8 byte sequence <paramref name="source"/> ends with
    /// the UTF-8 byte sequence <paramref name="pattern"/>, using ASCII-only case folding.
    /// Implementation: byte-length check + SIMD SequenceEqualIgnoreCase(byte*) on the
    /// last patLen bytes. O(patLen) time.
    /// </para>
    /// </summary>
    /// <param name="source">被搜索的 UTF-8 字节集合。/ The UTF-8 byte collection being searched.</param>
    /// <param name="pattern">要匹配的 UTF-8 后缀。/ The UTF-8 suffix to match.</param>
    /// <returns>source 以 pattern 结尾时返回 true。/ true if source ends with pattern.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsWithIgnoreCase(
        in UnManagedCollection<byte> source,
        in UnManagedCollection<byte> pattern)
    {
        if (pattern.IsEmpty) return true;

        int srcLen = (int)source.Size;
        int patLen = (int)pattern.Size;

        if (srcLen < patLen) return false;

        // 从 source 字节流末尾向前 patLen 字节处开始比较。
        // Compare starting at byteOffset = (srcLen - patLen) from the end.
        int startByteOffset = srcLen - patLen;

        // 边界安全：必须确认 startByteOffset 落在 UTF-8 code point 的起始字节上，
        // 否则 pattern 与 source 的多字节字符边界不对齐，比较结果无意义。
        // 检查方式：startByteOffset 处的字节不得为延续字节（10xxxxxx）。
        // Boundary safety: startByteOffset must coincide with the start of a UTF-8 code
        // point, otherwise multi-byte boundaries are misaligned and the comparison is
        // meaningless. Check: the byte at startByteOffset must not be a continuation byte.
        byte boundary = source.InternalPointer[startByteOffset];
        if ((boundary & 0xC0) == 0x80)
        {
            // 起始点落在延续字节内：字节流边界不对齐，无法匹配。
            // Start falls on a continuation byte: boundary misaligned, cannot match.
            return false;
        }

        return MatchesAt_Utf8(source.InternalPointer, startByteOffset, pattern.InternalPointer, patLen);
    }


    /// <summary>
    /// 检查 UTF-8 字节序列 <paramref name="source"/> 是否以
    /// UTF-8 字节序列 <paramref name="pattern"/> 结尾（ASCII 大小写不敏感）。
    ///
    /// 实现：字节长度检查 + 对 source 末尾 patLen 字节调用 SequenceEqualIgnoreCase(byte*) SIMD 路径。
    /// 时间复杂度 O(patLen)。
    ///
    /// <para>
    /// Returns true if the UTF-8 byte sequence <paramref name="source"/> ends with
    /// the UTF-8 byte sequence <paramref name="pattern"/>, using ASCII-only case folding.
    /// Implementation: byte-length check + SIMD SequenceEqualIgnoreCase(byte*) on the
    /// last patLen bytes. O(patLen) time.
    /// </para>
    /// </summary>
    /// <param name="source">被搜索的 UTF-8 字节集合。/ The UTF-8 byte collection being searched.</param>
    /// <param name="pattern">要匹配的 UTF-8 后缀。/ The UTF-8 suffix to match.</param>
    /// <returns>source 以 pattern 结尾时返回 true。/ true if source ends with pattern.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsWithIgnoreCase(
        UnManagedCollection<byte>* source,
        UnManagedCollection<byte>* pattern)
    {
        if (source is null || pattern is null) return false;
        if (source->IsEmpty || pattern->IsEmpty) return true;

        int srcLen = (int)source->Size;
        int patLen = (int)pattern->Size;

        if (srcLen < patLen) return false;

        // 从 source 字节流末尾向前 patLen 字节处开始比较。
        // Compare starting at byteOffset = (srcLen - patLen) from the end.
        int startByteOffset = srcLen - patLen;

        // 边界安全：必须确认 startByteOffset 落在 UTF-8 code point 的起始字节上，
        // 否则 pattern 与 source 的多字节字符边界不对齐，比较结果无意义。
        // 检查方式：startByteOffset 处的字节不得为延续字节（10xxxxxx）。
        // Boundary safety: startByteOffset must coincide with the start of a UTF-8 code
        // point, otherwise multi-byte boundaries are misaligned and the comparison is
        // meaningless. Check: the byte at startByteOffset must not be a continuation byte.
        byte boundary = source->InternalPointer[startByteOffset];
        if ((boundary & 0xC0) == 0x80)
        {
            // 起始点落在延续字节内：字节流边界不对齐，无法匹配。
            // Start falls on a continuation byte: boundary misaligned, cannot match.
            return false;
        }

        return MatchesAt_Utf8(source->InternalPointer, startByteOffset, pattern->InternalPointer, patLen);
    }




    /// <summary>
    /// 在 UTF-8 字节序列 <paramref name="source"/> 中正向查找
    /// UTF-8 字节序列 <paramref name="pattern"/> 首次出现的位置（ASCII 大小写不敏感）。
    ///
    /// 算法：
    ///   1. 以首字节过滤定位候选起始位置（跳过延续字节，确保对齐）。
    ///   2. 对每个候选位置调用 MatchesAt_Utf8 进行完整字节比较。
    ///   3. 匹配成功后调用 Utf8ByteOffsetToCharIndex 将字节偏移转换为
    ///      Unicode code point 索引。
    ///
    /// 时间复杂度：O(srcLen × patLen)（最坏），O(srcLen) 的首字节过滤在实践中显著降低开销。
    ///
    /// <para>
    /// Forward search for the first occurrence of the UTF-8 pattern in source,
    /// using ASCII-only case folding.
    ///
    /// Algorithm:
    ///   1. Filter candidate positions by first byte (skip continuation bytes for alignment).
    ///   2. Call MatchesAt_Utf8 at each candidate for a full byte comparison.
    ///   3. On match, call Utf8ByteOffsetToCharIndex to convert the byte offset to a
    ///      Unicode code point index.
    ///
    /// Time: O(srcLen × patLen) worst case; first-byte filtering makes it O(srcLen)
    /// in practice.
    /// </para>
    /// </summary>
    /// <param name="source">被搜索的 UTF-8 字节集合。/ The UTF-8 byte collection being searched.</param>
    /// <param name="pattern">要查找的 UTF-8 子序列。/ The UTF-8 subsequence to find.</param>
    /// <returns>
    /// 首次匹配位置的 Unicode code point 索引（0-based），未找到返回 -1。
    /// / Zero-based Unicode code point index of the first match, or -1 if not found.
    /// </returns>
    public static int IndexOfIgnoreCase(
        in UnManagedCollection<byte> source,
        in UnManagedCollection<byte> pattern)
    {
        if (pattern.IsEmpty) return 0;

        int srcLen = (int)source.Size;
        int patLen = (int)pattern.Size;

        if (srcLen < patLen) return -1;

        byte* src = source.InternalPointer;
        byte* pat = pattern.InternalPointer;

        // 首字节过滤：预先计算 pattern[0] 的折叠值，用于跳过不可能的起始位置。
        // First-byte filter: pre-compute folded form of pat[0] to skip impossible starts.
        byte p0 = pat[0];
        byte p0_low = (byte)(p0 | 0x20);
        bool p0_alpha = p0_low >= (byte)'a' && p0_low <= (byte)'z' && p0 < 0x80;
        // pattern 首字节不得是延续字节（UTF-8 合法 pattern 不允许从延续字节开始）。
        // A valid UTF-8 pattern must not start with a continuation byte.
        // (Caller responsibility; we do not validate pattern encoding here.)

        int limit = srcLen - patLen; // 最后一个合法字节起始偏移 / last valid byte start offset

        for (int i = 0; i <= limit; i++)
        {
            byte s = src[i];

            // 跳过延续字节（10xxxxxx）：延续字节不是任何 code point 的起始位置，
            // 永远不可能与 pattern 的起始字节对齐匹配。
            // Skip continuation bytes (10xxxxxx): they cannot be the start of any code point
            // and can never align with the start of a valid UTF-8 pattern.
            if ((s & 0xC0) == 0x80) continue;

            // 首字节不匹配：快速跳过。
            // First byte mismatch: skip immediately.
            bool firstMatch = (s == p0) || (p0_alpha && (byte)(s | 0x20) == p0_low && s < 0x80);
            if (!firstMatch) continue;

            if (MatchesAt_Utf8(src, i, pat, patLen))
            {
                // 将字节偏移 i 转换为 Unicode code point 索引。
                // Convert byte offset i to Unicode code point index.
                return Utf8ByteOffsetToCharIndex(src, i);
            }
        }
        return -1;
    }


    /// <summary>
    /// 在 UTF-8 字节序列 <paramref name="source"/> 中反向查找
    /// UTF-8 字节序列 <paramref name="pattern"/> 最后一次出现的位置（ASCII 大小写不敏感）。
    ///
    /// 算法：
    ///   1. 从末尾向前滑动，跳过延续字节以保证 code point 对齐。
    ///   2. 对每个候选位置调用 MatchesAt_Utf8 进行完整字节比较。
    ///   3. 匹配成功后调用 Utf8ByteOffsetToCharIndex 将字节偏移转换为
    ///      Unicode code point 索引。
    ///
    /// <para>
    /// Backward search for the last occurrence of the UTF-8 pattern in source,
    /// using ASCII-only case folding.
    ///
    /// Algorithm:
    ///   1. Slide from end toward start; skip continuation bytes for code-point alignment.
    ///   2. Call MatchesAt_Utf8 at each candidate.
    ///   3. On match, call Utf8ByteOffsetToCharIndex to get the code point index.
    /// </para>
    /// </summary>
    /// <param name="source">被搜索的 UTF-8 字节集合。/ The UTF-8 byte collection being searched.</param>
    /// <param name="pattern">要查找的 UTF-8 子序列。/ The UTF-8 subsequence to find.</param>
    /// <returns>
    /// 最后匹配位置的 Unicode code point 索引（0-based），未找到返回 -1。
    /// / Zero-based Unicode code point index of the last match, or -1 if not found.
    /// </returns>
    public static int LastIndexOfIgnoreCase(
        in UnManagedCollection<byte> source,
        in UnManagedCollection<byte> pattern)
    {
        if (pattern.IsEmpty) return Utf8CharCount(source.InternalPointer, (int)source.Size);

        int srcLen = (int)source.Size;
        int patLen = (int)pattern.Size;

        if (srcLen < patLen) return -1;

        byte* src = source.InternalPointer;
        byte* pat = pattern.InternalPointer;

        byte p0 = pat[0];
        byte p0_low = (byte)(p0 | 0x20);
        bool p0_alpha = p0_low >= (byte)'a' && p0_low <= (byte)'z' && p0 < 0x80;

        // 从最后一个可能的起始字节偏移向前扫描。
        // Scan backward from the last possible start byte offset.
        for (int i = srcLen - patLen; i >= 0; i--)
        {
            byte s = src[i];

            // 跳过延续字节（理由同 IndexOf）。
            // Skip continuation bytes (same reason as IndexOf).
            if ((s & 0xC0) == 0x80) continue;

            bool firstMatch = (s == p0) || (p0_alpha && (byte)(s | 0x20) == p0_low && s < 0x80);
            if (!firstMatch) continue;

            if (MatchesAt_Utf8(src, i, pat, patLen))
            {
                return Utf8ByteOffsetToCharIndex(src, i);
            }
        }
        return -1;
    }
}
