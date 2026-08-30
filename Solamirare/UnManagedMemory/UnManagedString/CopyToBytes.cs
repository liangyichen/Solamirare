using System;
using System.Collections.Generic;
using System.Text;

namespace Solamirare;


//与字节序列的转换


public static unsafe partial class UnManagedStringHelper
{

    /// <summary>
    /// 将字符串转换为 UTF-8 字节序列（值复制）。
    /// </summary>
    /// <param name="source"></param>
    /// <param name="reaultAddress"></param>
    /// <param name="resultLength"></param>
    /// <returns></returns>
    public static bool CopyToBytes(this in UnManagedString source, byte* reaultAddress, uint resultLength)
    {

        if (source.IsEmpty)
            return false;

        Span<char> span_chars = source.AsSpan();

        return CopyToBytes(span_chars, reaultAddress, resultLength);

    }


    /// <summary>
    /// 将字符串转换为 UTF-8 字节序列（值复制）。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <param name="result">存储结果的字节集合。</param>
    public static bool CopyToBytes(this in UnManagedString source, UnManagedMemory<byte>* result)
    {

        if (!source.Activated || source.IsEmpty || result is null || !result->Activated)
            return false;

        Span<char> span_chars = source.AsSpan();

        return CopyToBytes(span_chars, result);

    }


    /// <summary>
    /// 将字符串转换为 UTF-8 字节序列（值复制）。
    /// </summary>
    /// <param name="source"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool CopyToBytes(this in UnManagedCollection<char> source, UnManagedMemory<byte>* result)
    {

        if (source.IsEmpty || result is null || !result->Activated)
            return false;

        Span<char> span_chars = source.AsSpan();

        return CopyToBytes(span_chars, result);

    }


    /// <summary>
    /// 将字符串转换为 UTF-8 字节序列（值复制）。
    /// </summary>
    /// <param name="source"></param>
    /// <param name="reaultAddress"></param>
    /// <param name="resultLength"></param>
    /// <returns></returns>
    public static bool CopyToBytes(this in UnManagedCollection<char> source, byte* reaultAddress, uint resultLength)
    {

        if (source.IsEmpty)
            return false;

        Span<char> span_chars = source.AsSpan();

        return CopyToBytes(span_chars, reaultAddress, resultLength);

    }


    /// <summary>
    /// 将字符串转换为 UTF-8 字节序列（值复制）。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <param name="result">存储结果的字节集合。</param>
    public static bool CopyToBytes(this ReadOnlySpan<char> source, UnManagedMemory<byte>* result)
    {

        if (result is null || !result->Activated) return false;

        if (!source.IsEmpty)
        {

            int bytesCount = Encoding.UTF8.GetByteCount(source);

            bool ensure = result->EnsureCapacity((uint)bytesCount);

            if (!ensure) return false;


            result->ReLength((uint)bytesCount);

            Encoding.UTF8.GetBytes(source, result->AsSpan());

            return true;
        }

        return false;
    }


    /// <summary>
    /// 将字符串转换为 UTF-8 字节序列（值复制）。
    /// </summary>
    /// <param name="source"></param>
    /// <param name="reaultAddress"></param>
    /// <param name="resultLength"></param>
    /// <returns></returns>
    public static bool CopyToBytes(this ReadOnlySpan<char> source, byte* reaultAddress, uint resultLength)
    {

        if (reaultAddress is null || resultLength == 0) return false;

        if (!source.IsEmpty)
        {

            int bytesCount = Encoding.UTF8.GetByteCount(source);

            bool ensure = resultLength >= bytesCount;

            if (!ensure) return false;


            fixed (char* pSource = source)
            {
                Encoding.UTF8.GetBytes(pSource, source.Length, reaultAddress, (int)resultLength);
            }

            return true;
        }

        return false;
    }


    /// <summary>
    /// 将字符串转换为 UTF-8 字节序列（创建新内存）。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <returns>包含 UTF-8 字节的新非托管内存。</returns>
    public static UnManagedMemory<byte> CopyToBytes(this ReadOnlySpan<char> source)
    {
        UnManagedMemory<byte> mem = new UnManagedMemory<byte>();

        CopyToBytes(source, &mem);

        return mem;
    }

    /// <summary>
    /// 将字符串转换为 UTF-8 字节序列（创建新内存）。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <returns>包含 UTF-8 字节的新非托管内存。</returns>
    public static UnManagedMemory<byte> CopyToBytes(this in UnManagedString source)
    {
        if (!source.IsEmpty && source.Activated)
        {

            Span<char> span = source.AsSpan();

            UnManagedMemory<byte> result = new UnManagedMemory<byte>();

            CopyToBytes(span, &result);

            return result;
        }

        return UnManagedMemory<byte>.Empty;
    }


    /// <summary>
    /// 将字符串转换为 UTF-8 字节序列（创建新内存）。
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static UnManagedMemory<byte> CopyToBytes(this in UnManagedCollection<char> source)
    {
        if (!source.IsEmpty)
        {

            Span<char> span = source.AsSpan();

            UnManagedMemory<byte> result = new UnManagedMemory<byte>();

            CopyToBytes(span, &result);

            return result;
        }

        return UnManagedMemory<byte>.Empty;
    }
}
