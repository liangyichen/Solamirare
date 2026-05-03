using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Solamirare;

//非托管字符串的功能集合



/// <summary>
/// </summary>
public static unsafe partial class UnManagedStringHelper
{




    /// <summary>
    /// 计算使用指定分隔符分割字符串后产生的子字符串数量。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <param name="separator">分隔符。</param>
    /// <returns>分割后的子字符串数量。</returns>
    public static int Count(ReadOnlySpan<char> source, char separator)
    {
        if (source.IsEmpty) return 0;

        MemoryExtensions.SpanSplitEnumerator<char> enums = source.Split(separator);

        int count = 0;

        foreach (Range i in enums)
        {
            count += 1;
        }

        return count;
    }


    /// <summary>
    /// 将字符串分割为非托管集合序列（映射模式）。
    /// <para>内部的 <see cref="UnManagedCollection{T}"/> 映射到 <paramref name="source"/> 的局部片段，不需要释放。</para>
    /// <para>但是返回的集合本身占用的内存需要释放。</para>
    /// </summary>
    /// <param name="source">源字符串集合。</param>
    /// <param name="separator">分隔符。</param>
    /// <returns>分割后的字符串集合。</returns>
    public static UnManagedMemory<UnManagedCollection<char>> SplitMapToCollection(UnManagedCollection<char>* source, char separator)
    {
        if (source is null || source->IsEmpty)
            return UnManagedMemory<UnManagedCollection<char>>.Empty;

        Span<char> spanSource = source->AsSpan();

        int count = Count(spanSource, separator);

        if (count == 0 || separator == '\0')
        {
            return UnManagedMemory<UnManagedCollection<char>>.Empty;
        }

        UnManagedMemory<UnManagedCollection<char>> result = new((uint)count, 0);

        MemoryExtensions.SpanSplitEnumerator<char> enums = spanSource.Split(separator);

        foreach (Range i in enums)
        {
            Span<char> slice = spanSource[i];

            result.Add(slice.MapToUnManagedCollection());
        }


        return result;
    }

    /// <summary>
    /// 将字符串分割为键值对字典（复制模式）。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <param name="entrySeparator">条目之间的分隔符（例如 Cookie 中的分号）。</param>
    /// <param name="keyValueSeparator">键和值之间的分隔符（例如 Cookie 中的等号）。</param>
    /// <returns>包含键值对的字典。</returns>
    public static ValueDictionary<UnManagedString, UnManagedString> SplitCopyToValueDictionary(UnManagedCollection<char>* source, char entrySeparator, char keyValueSeparator)
    {

        if (source is null || source->IsEmpty)
            return ValueDictionary<UnManagedString, UnManagedString>.Empty;


        UnManagedMemory<UnManagedCollection<char>> nodes = SplitMapToCollection(source, entrySeparator);

        ValueDictionary<UnManagedString, UnManagedString> result = new(nodes.UsageSize);

        for (int i = 0; i < nodes.UsageSize; i++)
        {
            UnManagedCollection<char>* segment = nodes[i];


            int equalIndex = segment->IndexOf(keyValueSeparator);

            Span<char> keySpan;
            Span<char> valueSpan;

            if (equalIndex >= 0)
            {
                // 找到了内部分割符，分割 Key 和 Value
                keySpan = segment->Slice(0, (uint)equalIndex).Trim();
                valueSpan = segment->Slice((uint)equalIndex + 1).Trim();
            }
            else
            {
                // 没有内部分割符，整个片段是 Key，Value 为空
                keySpan = segment->Trim();
                valueSpan = Span<char>.Empty;
            }


            if (!keySpan.IsEmpty)
            {
                UnManagedString keyMem = new UnManagedString(keySpan);

                UnManagedString valueMem;

                if (valueSpan.IsEmpty)

                    valueMem = UnManagedString.Empty;
                else

                    valueMem = new UnManagedString(valueSpan);

                result.AddOrUpdate(keyMem, valueMem);
            }

        }

        nodes.Dispose();

        return result;
    }

    /// <summary>
    /// 将字符串分割为键值对字典（映射模式）。
    /// <para>键和值映射到 <paramref name="source"/> 的片段，不需要单独释放。</para>
    /// <para>但是字典本身需要释放。</para>
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <param name="entrySeparator">条目之间的分隔符。</param>
    /// <param name="keyValueSeparator">键和值之间的分隔符。</param>
    /// <returns>包含键值对的字典。</returns>
    public static ValueDictionary<UnManagedString, UnManagedString> SplitMapToValueDictionary(UnManagedCollection<char>* source, char entrySeparator, char keyValueSeparator)
    {
        if (source is null || source->IsEmpty)
            return ValueDictionary<UnManagedString, UnManagedString>.Empty;

        UnManagedMemory<UnManagedCollection<char>> nodes = SplitMapToCollection(source, entrySeparator);

        if (nodes.IsEmpty)
            return ValueDictionary<UnManagedString, UnManagedString>.Empty;

        ValueDictionary<UnManagedString, UnManagedString> result = new(nodes.UsageSize);

        for (int i = 0; i < nodes.UsageSize; i++)
        {
            UnManagedCollection<char>* segment = nodes[i];


            int equalIndex = segment->IndexOf(keyValueSeparator);

            UnManagedString keySpan;
            UnManagedString valueSpan;

            if (equalIndex >= 0)
            {
                // 找到了内部分割符，分割 Key 和 Value
                keySpan = segment->Slice(0, (uint)equalIndex).Trim().AsUnManagedMemory();
                valueSpan = segment->Slice((uint)equalIndex + 1).Trim().AsUnManagedMemory();
            }
            else
            {
                // 没有内部分割符，整个片段是 Key，Value 为空
                keySpan = segment->Trim().AsUnManagedMemory();
                valueSpan = UnManagedString.Empty;
            }


            if (!keySpan.IsEmpty)
            {
                result.AddOrUpdate(keySpan, valueSpan);
            }

        }

        nodes.Dispose();

        return result;
    }


    /// <summary>
    /// 将托管字符串数组复制到非托管内存中。
    /// </summary>
    /// <param name="source">源字符串数组。</param>
    /// <returns>非托管字符串集合。</returns>
    public static UnManagedMemory<UnManagedString> CopyToUnManagedMemory(this string[] source)
    {
        if (source is not null && source.Length > 0)
        {
            UnManagedMemory<UnManagedString> contents = new UnManagedMemory<UnManagedString>((uint)source.Length, 0);

            for (int i = 0; i < source.Length; i++)
            {
                string item = source[i];
                UnManagedString node = new UnManagedString(item);
                contents.Add(node);
            }

            return contents;
        }


        return new UnManagedMemory<UnManagedString>(0);
    }




    /// <summary>
    /// 使用指定的分隔符连接非托管字符串集合的成员。
    /// </summary>
    /// <param name="source">要连接的字符串集合。</param>
    /// <param name="separator">分隔符。</param>
    /// <param name="buffer">用于存储结果的外部内存缓冲区，外部调用者必须保证长度足够。</param>
    /// <param name="bufferLength">外部缓冲区的长度。</param>
    /// <returns>连接后的非托管字符串。</returns>
    public static UnManagedString Join(this in UnManagedMemory<UnManagedString> source, char separator, char* buffer, uint bufferLength)
    {
        UnManagedString result =
            buffer is not null && bufferLength > 0 ?
            new UnManagedString(buffer, bufferLength, 0) :
            UnManagedString.Empty;


        if (separator != '\0' && !source.IsEmpty)
        {
            for (int i = 0; i < source.UsageSize; i++)
            {
                UnManagedString* source_item = source[i];
                result.AddRange((UnManagedCollection<char>*)source_item);
                result.Add(separator);
            }
        }

        if (!result.IsEmpty)
        {
            result.ReLength(result.UsageSize - 1);//减去最后一个符号
        }

        return result;
    }


    /// <summary>
    /// 计算字符串集合中所有字符串的总字符数。
    /// </summary>
    /// <param name="source">字符串集合。</param>
    /// <returns>总字符数。</returns>
    public static uint CharsCount(this in UnManagedMemory<UnManagedString> source)
    {
        if (!source.Activated) return 0;

        uint count = 0;

        for (int i = 0; i < source.UsageSize; i++)
        {
            UnManagedString* item = source[i];
            if (item is not null)
            {
                count += (uint)item->UsageSize;
            }
        }

        return count;
    }



    /// <summary>
    /// 将字符串转换为大写（原地修改）。
    /// </summary>
    /// <param name="source">要修改的字符串。</param>
    public unsafe static void SetAsUpper(this in UnManagedString source)
    {
        if (source.IsEmpty || !source.Activated)
            return;

        for (int i = 0; i < source.UsageSize; i++)
        {
            if (source.Pointer[i] >= 'a' && source.Pointer[i] <= 'z')
            {
                *(source.Pointer + i) = (char)(source.Pointer[i] - ('a' - 'A'));
            }
        }
    }


    /// <summary>
    /// 将字符串转换为小写（原地修改）。
    /// </summary>
    /// <param name="source">要修改的字符串。</param>
    public unsafe static void SetAsLower(this in UnManagedString source)
    {
        if (source.IsEmpty || !source.Activated)
            return;

        for (int i = 0; i < source.UsageSize; i++)
        {
            if (source.Pointer[i] >= 'A' && source.Pointer[i] <= 'Z')
            {
                *(source.Pointer + i) = (char)(source.Pointer[i] + ('a' - 'A'));
            }
        }
    }


    /// <summary>
    /// 移除字符串两端的空白字符（返回新视图，不复制内存）。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <returns>移除空白后的字符串视图。</returns>
    public static UnManagedCollection<char> Trim(this in UnManagedString source)
    {
        if (!source.Activated || source.IsEmpty) return UnManagedString.Empty;


        UnManagedCollection<char> inner = source.Prototype.Trim();

        return inner;
    }


    /// <summary>
    /// 移除字符串两端的空白字符（复制到新内存）。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <returns>移除空白后的新字符串。</returns>
    public static UnManagedString TrimClone(this in UnManagedString source)
    {
        if (!source.Activated || source.IsEmpty) return UnManagedString.Empty;

        UnManagedCollection<char> inner = source.Prototype.Trim();

        var mem = new UnManagedString(inner);

        return mem;
    }


    /// <summary>
    /// 使用指定分隔符分割字符串（复制模式）。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <param name="separator">分隔符。</param>
    /// <returns>分割后的字符串集合。</returns>
    public static UnManagedMemory<UnManagedString> SplitCopyToCollection(this ReadOnlySpan<char> source, char separator)
    {
        int count = Count(source, separator);

        if (count == 0 || separator == '\0')
        {
            return UnManagedMemory<UnManagedString>.Empty;
        }

        UnManagedMemory<UnManagedString> result = new((uint)count, 0);

        MemoryExtensions.SpanSplitEnumerator<char> enums = source.Split(separator);

        foreach (Range i in enums)
        {
            ReadOnlySpan<char> slice = source[i];

            result.Add(slice);
        }

        return result;
    }



    /// <summary>
    /// 使用指定分隔符分割字符串，并将结果存入提供的集合中。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <param name="separator">分隔符。</param>
    /// <param name="result">存储结果的目标集合。</param>
    /// <param name="clone">是否复制每个片段的内容。如果为 false，则结果引用源字符串的内存。</param>
    /// <returns>操作是否成功。</returns>
    public static bool Split(this in UnManagedString source, char separator, UnManagedMemory<UnManagedString>* result, bool clone)
    {

        if (!source.Activated || result is null || !result->Activated) return false;


        int count;

        if (separator == '\0' || result is null)
        {
            return false;
        }
        else
        {
            count = source.Count(separator);
        }

        if (count == -1) return false;

        bool resize_result = result->EnsureCapacity((uint)count);

        int start = 0, index = 0;

        if (resize_result)
        {
            while (true)
            {
                int separatorIndex = source.Slice((uint)start).IndexOf(separator);

                if (separatorIndex == -1)
                {
                    index++;

                    UnManagedString endof = source.Slice((uint)start);

                    result->Add(clone ? endof.Clone() : endof);

                    break;
                }
                else
                {
                    index++;
                    UnManagedString node = source.Slice((uint)start, (uint)separatorIndex);
                    result->Add(clone ? node.Clone() : node);

                    start += separatorIndex + 1;
                }
            }

            return true;
        }
        else
        {
            return false;
        }
    }



    /// <summary>
    /// 将非托管字符串转换为 <see cref="int"/>。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <returns>转换后的整数。如果转换失败，返回默认值。</returns>
    public static int ToInt32(this in UnManagedString source)
    {
        if (!source.Activated) return -1;

        int.TryParse(source.AsSpan(), out int result);

        return result;
    }

    /// <summary>
    /// 将非托管字符串转换为 <see cref="bool"/>。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <returns>转换后的布尔值。支持 "1"/"0" 或 "true"/"false"。</returns>
    public static bool ToBoolean(this in UnManagedString source)
    {
        if (!source.Activated) return false;

        if (source == "1" || source == "0") return source != "0" ? true : false;

        bool.TryParse(source.AsSpan(), out bool result);

        return result;
    }


    /// <summary>
    /// 将非托管字符串转换为枚举值。
    /// </summary>
    /// <typeparam name="T">枚举类型。</typeparam>
    /// <param name="source">源字符串。</param>
    /// <returns>转换后的枚举值。</returns>
    public static T ToEnum<T>(this in UnManagedString source) where T : Enum
    {
        if (!source.IsEmpty && source.Activated)
        {
            Enum.TryParse(typeof(T), source.AsSpan(), out var result);

            return (T)result!;
        }
        else
        {
            return default!;
        }
    }


    /// <summary>
    /// 将非托管字符串转换为 <see cref="DateTime"/>。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <returns>转换后的时间。如果失败，返回逻辑空时间。</returns>
    public static DateTime ToDateTime(this in UnManagedString source)
    {
        if (source.IsEmpty || !source.Activated) return UnManagedMemory_Extension.EmptyDatetimeValue;

        if (DateTime.TryParse(source.AsSpan(), out var result))
        {
            return result;
        }
        else
        {
            return UnManagedMemory_Extension.EmptyDatetimeValue;
        }
    }


    /// <summary>
    /// 对字符集合进行排序。
    /// </summary>
    /// <param name="source">要排序的集合。</param>
    public static void Sort(this in UnManagedString source)
    {
        if (source.Pointer is not null && source.Activated)
            UnmanagedMemorySorter.Sort(source.Pointer, source.UsageSize, &CharComparer);
    }

    //char 本身的实际值就是 int ，所以使用 int 的逻辑来比较即可
    private static int CharComparer(nint ptrA, nint ptrB)
    {
        // 将 void* 转换为 char*，然后解引用以获取 char 值
        char a = *(char*)ptrA;
        char b = *(char*)ptrB;

        // 直接比较 char 的数值大小
        if (a < b) return -1;
        if (a > b) return 1;
        return 0;
    }

    /// <summary>
    /// 将字符串转换为大写（复制到新副本）。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <returns>转换为大写的新字符串。</returns>
    public static UnManagedString ToUpper(this in UnManagedString source)
    {

        if (source.IsEmpty || !source.Activated) return UnManagedString.Empty;

        UnManagedString result = new UnManagedString(source.UsageSize, source.UsageSize);

        char* pSource = source.Pointer;
        char* pResult = result.Pointer;
        uint len = source.UsageSize;

        for (uint i = 0; i < len; i++)
        {
            char c = pSource[i];
            if (c >= 'a' && c <= 'z')
            {
                c = (char)(c - 32);
            }
            pResult[i] = c;
        }

        return result;
    }

    /// <summary>
    /// 将字符串转换为小写（复制到新副本）。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <returns>转换为小写的新字符串。</returns>
    public static UnManagedString ToLower(this in UnManagedString source)
    {
        if (source.IsEmpty || !source.Activated) return UnManagedString.Empty;

        UnManagedString result = new UnManagedString(source.UsageSize, source.UsageSize);

        char* pSource = source.Pointer;
        char* pResult = result.Pointer;
        uint len = source.UsageSize;

        for (uint i = 0; i < len; i++)
        {
            char c = pSource[i];
            if (c >= 'A' && c <= 'Z')
            {
                c = (char)(c + 32);
            }
            pResult[i] = c;
        }

        return result;
    }






    /// <summary>
    /// 指示指定的字符串是 null、空还是仅由空白字符组成。
    /// </summary>
    /// <param name="source">要测试的字符串。</param>
    /// <returns>如果字符串为 null、空或仅包含空白字符，则为 true；否则为 false。</returns>
    public static bool IsNullOrWhiteSpace(this in UnManagedString source)
    {
        if (source.IsEmpty || !source.Activated) return true;

        char* p = source.Pointer;
        for (uint i = 0; i < source.UsageSize; i++)
        {
            if (!char.IsWhiteSpace(p[i])) return false;
        }
        return true;
    }

    /// <summary>
    /// 移除字符串开头的空白字符（返回视图）。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <returns>移除开头空白后的字符串视图。</returns>
    public static UnManagedCollection<char> TrimStart(this in UnManagedString source)
    {
        if (source.IsEmpty || !source.Activated) return UnManagedCollection<char>.Empty;

        uint start = 0;

        char* p = source.Pointer;

        while (start < source.UsageSize && char.IsWhiteSpace(p[start]))
        {
            start++;
        }

        if (start == source.UsageSize) return UnManagedCollection<char>.Empty;

        if (start == 0) return source;

        ReadOnlySpan<char> span = new ReadOnlySpan<char>(p + start, (int)(source.UsageSize - start));

        return span.MapToUnManagedCollection();
    }

    /// <summary>
    /// 移除字符串结尾的空白字符（返回视图）。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <returns>移除结尾空白后的字符串视图。</returns>
    public static UnManagedCollection<char> TrimEnd(this in UnManagedString source)
    {
        if (source.IsEmpty || !source.Activated) return UnManagedString.Empty;

        uint end = source.UsageSize;

        char* p = source.Pointer;

        while (end > 0 && char.IsWhiteSpace(p[end - 1]))
        {
            end--;
        }

        if (end == 0) return UnManagedCollection<char>.Empty;

        if (end == source.UsageSize) return source;

        ReadOnlySpan<char> span = new ReadOnlySpan<char>(p, (int)end);

        return span.MapToUnManagedCollection();
    }


}