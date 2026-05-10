using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Solamirare;

public static unsafe partial class UnManagedStringHelper
{



    /// <summary>
    /// 将托管字符串复制到非托管内存中。
    /// </summary>
    /// <param name="source">源字符串。</param>
    /// <returns>非托管字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnManagedString CopyToChars(this string source)
    {
        return new UnManagedString(source.AsSpan());
    }







    static void CopyToChars(in UnManagedCollection<byte> source, UnManagedString* result, uint decodedCharCount)
    {
        if (result is not null && result->Activated && result->Capacity > 0)
        {
            byte* p_source = source.InternalPointer;
            char* p_result = result->Pointer;

            if (result->Capacity >= decodedCharCount)
                Encoding.UTF8.GetChars(p_source, (int)source.Size, p_result, (int)decodedCharCount);
            else
            {
                //如果 result 扩容失败，真实长度不足以容纳整个 char序列，则取能够容纳的最大容量（当前 result真实容量）
                //因为 source.Length 是不能计算出截取长度的， 只能让它计算出完整内容
                //必须创建一个临时容器来存储完整的解码内容


                if (decodedCharCount < 1024)
                {
                    char* stack_temp = stackalloc char[(int)decodedCharCount];

                    Encoding.UTF8.GetChars(p_source, (int)source.Size, stack_temp, (int)decodedCharCount);

                    Unsafe.CopyBlock(p_result, stack_temp, result->Capacity * sizeof(char));
                }
                else
                {
                    UnManagedString temp = new UnManagedString(decodedCharCount, decodedCharCount);

                    Encoding.UTF8.GetChars(p_source, (int)source.Size, temp.Pointer, (int)decodedCharCount);

                    Unsafe.CopyBlock(p_result, temp.Pointer, result->Capacity * sizeof(char));

                    temp.Dispose();
                }


            }
        }
    }


    /// <summary>
    /// 将 UTF-8 字节序列转换为非托管字符集合（创建新内存）。
    /// <para>外部必须确保字节集合包含有效的 UTF-8 字符数据。</para>
    /// </summary>
    /// <param name="source">源字节序列。</param>
    /// <returns>转换后的字符集合。</returns>
    public static UnManagedString CopyToChars(this in UnManagedMemory<byte> source)
    {
        if (!source.Activated || source.IsEmpty) return UnManagedString.Empty;

        UnManagedCollection<byte> collection = source;

        return CopyToChars(collection);
    }


    /// <summary>
    /// 将 UTF-8 字节序列转换为非托管字符集合（创建新内存）。
    /// <para>外部必须确保字节集合包含有效的 UTF-8 字符数据。</para>
    /// </summary>
    /// <param name="source">源字节序列。</param>
    /// <returns>转换后的字符集合。</returns>
    public static UnManagedString CopyToChars(this in UnManagedCollection<byte> source)
    {
        if (source.IsEmpty) return UnManagedString.Empty;

        uint decodedCharCount = (uint)Encoding.UTF8.GetCharCount(source.InternalPointer, (int)source.Size);

        UnManagedString memory = new UnManagedString(decodedCharCount, decodedCharCount);

        CopyToChars(source, &memory, decodedCharCount);

        return memory;
    }


    /// <summary>
    /// 将 UTF-8 字节序列转换为非托管字符集合（值复制）。
    /// <para>外部必须确保字节集合包含有效的 UTF-8 字符数据。</para>
    /// </summary>
    /// <param name="source">源字节序列。</param>
    /// <param name="result">存储结果的字符集合。</param>
    public static void CopyToChars(this in UnManagedMemory<byte> source, UnManagedString* result)
    {
        if (result is null || !result->Activated || source.Pointer is null || !source.Activated)
            return;

        uint decodedCharCount = (uint)Encoding.UTF8.GetCharCount(source.Pointer, (int)source.UsageSize);

        result->EnsureCapacity(decodedCharCount, MemoryScaleMode.AppendEquals);

        result->ReLength(decodedCharCount);

        CopyToChars(source, result, decodedCharCount);
    }




}