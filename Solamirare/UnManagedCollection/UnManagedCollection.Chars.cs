using System.Runtime.CompilerServices;

namespace Solamirare;

// UnManagedCollection 的字符序列扩展处理

/// <summary>
/// 提供针对字符型 <see cref="UnManagedCollection{T}"/> 的扩展方法。
/// </summary>
public static unsafe class UnManagedCollectionExtension
{

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
    public static int IndexOfIgnoreCase(this in UnManagedCollection<char> source, in UnManagedCollection<char> pattern)
    {
        return ValueTypeHelper.IndexOfIgnoreCase(source, pattern);
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
    public static int LastIndexOfIgnoreCase(this in UnManagedCollection<char> source, in UnManagedCollection<char> pattern)
    {
        return ValueTypeHelper.LastIndexOfIgnoreCase(source, pattern);
    }


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
    public static bool StartsWithIgnoreCase(this in UnManagedCollection<char> source, in UnManagedCollection<char> pattern)
    {
        return ValueTypeHelper.StartWithIgnoreCase(source, pattern);
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
    public static bool EndsWithIgnoreCase(this in UnManagedCollection<char> source, in UnManagedCollection<char> pattern)
    {
        return ValueTypeHelper.EndsWithIgnoreCase(source, pattern);
    }


    /// <summary>
    /// 忽略大小写的字符序列比较
    /// </summary>
    /// <param name="source"></param>
    /// <param name="distination"></param>
    /// <returns></returns>
    public static bool SequenceEqualIgnoreCase(this in UnManagedCollection<char> source, in UnManagedString distination)
    {
        return SequenceEqualIgnoreCase(source, distination.Prototype);
    }

    /// <summary>
    /// 忽略大小写的字符序列比较
    /// </summary>
    /// <param name="source"></param>
    /// <param name="distination"></param>
    /// <returns></returns>
    public static bool SequenceEqualIgnoreCase(this in UnManagedCollection<char> source, UnManagedString* distination)
    {
        if (distination is null) return false;

        return SequenceEqualIgnoreCase(source, distination->Prototype);
    }


    /// <summary>
    /// 忽略大小写的字符序列比较
    /// </summary>
    /// <param name="source"></param>
    /// <param name="distination"></param>
    /// <returns></returns>
    public static bool SequenceEqualIgnoreCase(this in UnManagedCollection<char> source, in UnManagedCollection<char> distination)
    {
        if (source.IsEmpty || distination.IsEmpty) return false;

        bool result = ValueTypeHelper.SequenceEqualIgnoreCase(source.InternalPointer, distination.InternalPointer, (int)distination.Size);

        return result;
    }


    /// <summary>
    /// 忽略大小写的字符序列比较
    /// </summary>
    /// <param name="source"></param>
    /// <param name="distination"></param>
    /// <returns></returns>
    public static bool SequenceEqualIgnoreCase(this in UnManagedCollection<char> source, UnManagedCollection<char>* distination)
    {
        if (source.IsEmpty || distination is null || distination->IsEmpty) return false;

        bool result = ValueTypeHelper.SequenceEqualIgnoreCase(source.InternalPointer, distination->InternalPointer, (int)distination->Size);

        return result;
    }

    /// <summary>
    /// 忽略大小写的字符序列比较
    /// </summary>
    /// <param name="source"></param>
    /// <param name="distination"></param>
    /// <returns></returns>
    public static bool SequenceEqualIgnoreCase(this in UnManagedCollection<char> source, ReadOnlySpan<char> distination)
    {
        if (source.IsEmpty || distination.IsEmpty) return false;

        bool result;

        fixed (char* p_distination = distination)
        {
            result = ValueTypeHelper.SequenceEqualIgnoreCase(source.InternalPointer, p_distination, distination.Length);
        }

        return result;
    }





    /// <summary>
    /// 过滤首尾空白字符。
    /// <para>Filters leading and trailing whitespace characters.</para>
    /// </summary>
    /// <param name="source">原始的字符片段。<para>The original character segment.</para></param>
    /// <returns>过滤掉首尾空白后的新字符片段（Span 的切片）。<para>A new character segment with leading and trailing whitespace removed (a slice of Span).</para></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnManagedCollection<char> Trim(this in UnManagedCollection<char> source)
    {
        // 如果为空，直接返回。
        if (source.IsEmpty)
        {
            return source;
        }

        char first = *source[0];

        char last = *source[(int)source.Size - 1];

        if (!char.IsWhiteSpace(first) && !char.IsWhiteSpace(last))
            return source;

        
        char* startPtr = source.InternalPointer;

        char* currentPtr = startPtr;
        char* endPtr = startPtr + source.Size; // 结束指针指向最后一个元素之后

        uint newStartIndex = 0;

        // --- 3. 过滤头部空白 (Trim Start) ---
        // 从头开始向前移动指针，直到遇到第一个非空白字符
        while (currentPtr < endPtr && char.IsWhiteSpace(*currentPtr))
        {
            currentPtr++;
            newStartIndex++; // 记录新的起始索引
        }

        // 检查：如果都是空白，则返回一个空 Span
        if (newStartIndex == source.Size)
        {
            return UnManagedCollection<char>.Empty;
        }

        // --- 4. 过滤尾部空白 (Trim End) ---
        char* backPtr = endPtr - 1; // 从最后一个字符开始向后移动
        uint newEndIndex = source.Size;

        // 向后迭代，直到遇到第一个非空白字符，或者和头部指针相遇
        while (backPtr >= currentPtr && char.IsWhiteSpace(*backPtr))
        {
            backPtr--;
            newEndIndex--; // 记录新的结束索引
        }

        // 5. 计算结果并返回新的切片
        uint newLength = newEndIndex - newStartIndex;

        fixed (UnManagedCollection<char>* p_source = &source)

            return p_source->Slice(newStartIndex, newLength);
    }


    /// <summary>
    /// 分割为序列
    /// <para>Splits into a sequence.</para>
    /// <para>内部的 UnManagedCollection 映射到 source 的局部片段，不需要释放，但是内部 UnManagedCollection 本身会占用内存，所以外部的 UnManagedMemory 需要释放</para>
    /// <para>The internal UnManagedCollection maps to a local segment of source and does not need to be released, but the internal UnManagedCollection itself occupies memory, so the external UnManagedMemory needs to be released.</para>
    /// </summary>
    /// <param name="source"><para>The source collection.</para></param>
    /// <param name="separator"><para>The separator character.</para></param>
    /// <returns><para>The split result.</para></returns>
    public static UnManagedMemory<UnManagedCollection<char>> SplitMapToCollection(this UnManagedCollection<char> source, char separator)
    {
        UnManagedMemory<UnManagedCollection<char>> result = UnManagedStringHelper.SplitMapToCollection(&source, separator);

        return result;
    }

    /// <summary>
    /// 把字符串分割为 ValueFrozenDictionary（片段映射，不会创建片段的克隆段），使用两个符号做二维分割
    /// <para>Splits the string into a ValueFrozenDictionary (segment mapping, does not create cloned segments), using two symbols for two-dimensional splitting.</para>
    /// <para>key 和 value 映射到 source 的片段，不需要释放。但是 key 和 value 本身依然是作为 ValueFrozenDictionary 的内部内存，需要外部 ValueFrozenDictionary 执行 Dispose 进行释放</para>
    /// <para>Key and value map to segments of source and do not need to be released. However, the key and value themselves are still internal memory of ValueFrozenDictionary, and the external ValueFrozenDictionary needs to execute Dispose to release them.</para>
    /// </summary>
    /// <param name="source"><para>The source collection.</para></param>
    /// <param name="outerSymbol"><para>The outer separator symbol.</para></param>
    /// <param name="innerSymbol"><para>The inner separator symbol.</para></param>
    /// <returns><para>The dictionary result.</para></returns>
    public static ValueDictionary<UnManagedString, UnManagedString> SplitMapToValueDictionary(this in UnManagedCollection<char> source, char outerSymbol, char innerSymbol)
    {
        ValueDictionary<UnManagedString, UnManagedString> result;

        fixed (UnManagedCollection<char>* p_source = &source)
            result = UnManagedStringHelper.SplitMapToValueDictionary(p_source, outerSymbol, innerSymbol);

        return result;
    }


    /// <summary>
    /// 把字符串分割为 ValueFrozenDictionary（复制到新的内存），使用两个符号做二维分割
    /// <para>Splits the string into a ValueFrozenDictionary (copied to new memory), using two symbols for two-dimensional splitting.</para>
    /// </summary>
    /// <param name="source">原始字符串<para>The original string.</para></param>
    /// <param name="outerSymbol">外部分割符号，例如 Cookie 中的分号<para>The outer separator symbol, such as the semicolon in a Cookie.</para></param>
    /// <param name="innerSymbol">内部分割符号，例如 Cookie 中的等于号<para>The inner separator symbol, such as the equals sign in a Cookie.</para></param>
    /// <returns>包含 Key-Value 字符串对的字典<para>A dictionary containing Key-Value string pairs.</para></returns>
    public static ValueDictionary<UnManagedString, UnManagedString> SplitCopyToValueDictionary(this in UnManagedCollection<char> source, char outerSymbol, char innerSymbol)
    {
        ValueDictionary<UnManagedString, UnManagedString> result;

        fixed (UnManagedCollection<char>* p_source = &source)
            result = UnManagedStringHelper.SplitCopyToValueDictionary(p_source, outerSymbol, innerSymbol);

        return result;
    }

}
