using System;
using System.Collections.Generic;
using System.Text;

namespace Solamirare;

public static unsafe partial class UnManagedStringHelper
{

    /// <summary>
    /// 比较两个非托管字符串是否相等（忽略大小写）。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <param name="destination">要比较的目标字符串。</param>
    /// <returns>如果两个字符串在忽略大小写的情况下相等，则返回 true；否则返回 false。</returns>
    public static bool SequenceEqualIgnoreCase(this in UnManagedString source, in UnManagedString destination)
    {
        if (!source.Activated || !destination.Activated) return false;

        return source.Prototype.SequenceEqualIgnoreCase(destination);
    }



    /// <summary>
    /// 比较两个非托管字符串是否相等（忽略大小写）。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <param name="destination">要比较的目标字符串指针。</param>
    /// <returns>如果两个字符串在忽略大小写的情况下相等，则返回 true；否则返回 false。</returns>
    public static bool SequenceEqualIgnoreCase(this in UnManagedString source, UnManagedString* destination)
    {
        if (!source.Activated || destination is null || !destination->Activated) return false;

        return source.Prototype.SequenceEqualIgnoreCase(destination);
    }


    /// <summary>
    /// 比较两个非托管字符串是否相等（忽略大小写）。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <param name="destination">要比较的目标非托管集合。</param>
    /// <returns>如果两个字符串在忽略大小写的情况下相等，则返回 true；否则返回 false。</returns>
    public static bool SequenceEqualIgnoreCase(this in UnManagedString source, in UnManagedCollection<char> destination)
    {
        if (!source.Activated) return false;

        return source.Prototype.SequenceEqualIgnoreCase(destination);
    }

    /// <summary>
    /// 比较两个非托管字符串是否相等（忽略大小写）。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <param name="destination">要比较的目标非托管集合指针。</param>
    /// <returns>如果两个字符串在忽略大小写的情况下相等，则返回 true；否则返回 false。</returns>
    public static bool SequenceEqualIgnoreCase(this in UnManagedString source, UnManagedCollection<char>* destination)
    {
        if (!source.Activated || destination is null) return false;

        return source.Prototype.SequenceEqualIgnoreCase(destination);
    }


    /// <summary>
    /// 比较两个非托管字符串是否相等（忽略大小写）。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <param name="destination">要比较的目标只读 span。</param>
    /// <returns>如果两个字符串在忽略大小写的情况下相等，则返回 true；否则返回 false。</returns>
    public static bool SequenceEqualIgnoreCase(this in UnManagedString source, ReadOnlySpan<char> destination)
    {
        if (!source.Activated) return false;

        return source.Prototype.SequenceEqualIgnoreCase(destination);
    }

}
