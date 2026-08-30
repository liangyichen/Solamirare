using System.Runtime.InteropServices;
using System.Text;

namespace Solamirare;




/// <summary>
/// 原生控制台
/// </summary>
public unsafe static partial class NativeConsole
{
    private const int StdInputHandle = -10;
    private const int StdOutputHandle = -11;



    /// <summary>
    /// 将字符串写入到控制台
    /// <para>如果是在 windows 环境的 visual studio code 或者 vscode 的调试环境中，内部会自动调用 .Net 标准 Console.Out.Write 方法，会产生GC，但是在标准控制台中会调用原生无 GC 打印</para>
    /// </summary>
    public static void Write(ReadOnlySpan<char> chars)
    {
        if (chars.IsEmpty)
        {
        
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            void* handle = WindowsAPI.GetStdHandle(StdOutputHandle);
            if (handle != null)
            {
                fixed (char* p = chars)
                {
                    uint written;
                    if (WindowsAPI.WriteConsoleW(handle, p, (uint)chars.Length, out written, null))
                    {
                        return;
                    }
                }
            }

            Console.Out.Write(chars);
        }
        else if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
        {
            fixed (char* pChars = chars)
            {
                int byteCount = Encoding.UTF8.GetByteCount(pChars, chars.Length);

                if (byteCount > 8192)
                {

                    byte* buffer = (byte*)NativeMemory.AllocZeroed((nuint)byteCount);

                    Encoding.UTF8.GetBytes(pChars, chars.Length, buffer, byteCount);

                    PosixAPI.Write(1, buffer, byteCount);

                    NativeMemory.Free(buffer);
                }
                else
                {
                    byte* buffer = stackalloc byte[byteCount];

                    Encoding.UTF8.GetBytes(pChars, chars.Length, buffer, byteCount);

                    PosixAPI.Write(1, buffer, byteCount);

                }
            }
        }

    }

    /// <summary>
    /// 将字符串写入到控制台，并追加换行
    /// <para>如果是在 windows 环境的 visual studio code 或者 vscode 的调试环境中，内部会自动调用 .Net 标准 Console.Out.Write 方法，会产生GC，但是在标准控制台中会调用原生无 GC 打印</para>
    /// </summary>
    public static void WriteLine()
    {
        Write(Environment.NewLine);
    }

    /// <summary>
    /// 将字符串写入到控制台，并追加换行
    /// <para>如果是在 windows 环境的 visual studio code 或者 vscode 的调试环境中，内部会自动调用 .Net 标准 Console.Out.Write 方法，会产生GC，但是在标准控制台中会调用原生无 GC 打印</para>
    /// </summary>
    public static void WriteLine(ReadOnlySpan<char> chars)
    {
        Write(chars);
        Write(Environment.NewLine);
    }


    /// <summary>
    /// 从标准输入读取一行文本，并写入调用方预先分配的非托管字符缓冲区。
    /// </summary>
    /// <param name="buffer">目标非托管字符缓冲区。</param>
    /// <param name="bufferLength">目标缓冲区可写入的字符数量。</param>
    /// <returns>实际写入的字符数量，不包含换行符；失败或输入结束时返回 -1。</returns>
    public static int ReadLine(char* buffer, uint bufferLength)
    {
        if (buffer is null || bufferLength == 0 || bufferLength > int.MaxValue)
            return -1;

        if (OperatingSystem.IsWindows())
        {
            void* handle = WindowsAPI.GetStdHandle(StdInputHandle);
            if (handle == null)
                return -1;

            uint read;
            if (WindowsAPI.ReadConsoleW(handle, buffer, bufferLength, out read, null))
                return TrimLineEnding(buffer, (int)read);

            byte* bytes = stackalloc byte[4096];
            if (!WindowsAPI.ReadFile(handle, bytes, 4096, &read, null) || read == 0)
                return -1;

            int charsRead = Encoding.UTF8.GetChars(bytes, (int)read, buffer, (int)bufferLength);
            return TrimLineEnding(buffer, charsRead);
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            byte* bytes = stackalloc byte[4096];
            nint read;
            int total = 0;

            do
            {
                read = PosixAPI.Read(0, bytes + total, 4096 - total);
                if (read <= 0)
                    return total == 0 ? -1 : TrimLineEnding(buffer, Encoding.UTF8.GetChars(bytes, total, buffer, (int)bufferLength));

                total += (int)read;
            }
            while (total < 4096 && bytes[total - 1] != (byte)'\n');

            int charsRead = Encoding.UTF8.GetChars(bytes, total, buffer, (int)bufferLength);
            return TrimLineEnding(buffer, charsRead);
        }

        return -1;
    }

    /// <summary>
    /// 从标准输入读取一行文本，并写入预先分配的非托管字符串。
    /// </summary>
    /// <param name="destination">预先分配容量的非托管字符串。</param>
    /// <returns>实际写入的字符数量，不包含换行符；失败或输入结束时返回 -1。</returns>
    public static int ReadLine(UnManagedString* destination)
    {
        if (destination is null || !destination->Allocated || destination->Pointer is null)
            return -1;

        int length = ReadLine(destination->Pointer, destination->Capacity);
        if (length >= 0)
            destination->ReLength((uint)length);

        return length;
    }

    private static int TrimLineEnding(char* buffer, int length)
    {
        while (length > 0 && (buffer[length - 1] == '\r' || buffer[length - 1] == '\n'))
            length--;

        return length;
    }


}