using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Solamirare;

public unsafe static class DebugHelper
{
    /// <summary>
    /// 把字节集合形式表示的字符序列打印出来
    /// </summary>
    /// <param name="chars"></param>
    public static unsafe void PrintUtf8Buffer(UnManagedCollection<byte> chars)
    {
        PrintUtf8Buffer(chars.InternalPointer, chars.Size);
    }
    
    /// <summary>
    /// 把字节集合形式表示的字符序列打印出来
    /// </summary>
    /// <param name="chars"></param>
    public static unsafe void PrintUtf8Buffer(UnManagedMemory<byte> chars)
    {
        PrintUtf8Buffer(chars.Pointer, chars.UsageSize);
    }
    
    /// <summary>
    /// 把字节集合形式表示的字符序列打印出来
    /// </summary>
    /// <param name="chars"></param>
    public static unsafe void PrintUtf8Buffer(UnManagedMemory<byte>* chars)
    {
        if (chars is null) return;

        PrintUtf8Buffer(chars->Pointer, chars->UsageSize);
    }

    /// <summary>
    /// 把字节集合形式表示的字符序列打印出来
    /// </summary>
    /// <param name="pSource"></param>
    /// <param name="bytesLen"></param>
    public static unsafe void PrintUtf8Buffer(byte* pSource, uint bytesLen)
    {
        var temp = new UnManagedMemory<byte>(pSource, bytesLen, bytesLen, MemoryTypeDefined.Stack);

        var chars = temp.CopyToChars();

        Console.WriteLine(chars.AsSpan());

        chars.Dispose();
    }



}