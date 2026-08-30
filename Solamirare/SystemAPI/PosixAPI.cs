


/// <summary>
/// Posix 通用库
/// </summary>
internal unsafe partial class PosixAPI
{



    /// <summary>
    /// 向 POSIX 文件描述符写入指定数量的字节。
    /// </summary>
    /// <param name="fd">目标文件描述符。</param>
    /// <param name="buffer">待写入的非托管字节缓冲区。</param>
    /// <param name="count">要写入的字节数量。</param>
    /// <returns>实际写入的字节数量；失败时返回负值。</returns>
    [DllImport("libc", SetLastError = true, EntryPoint = "write")]
    public static extern nint Write(int fd, byte* buffer, nint count);




    /// <summary>
    /// 从 POSIX 文件描述符读取指定数量的字节。
    /// </summary>
    /// <param name="fd">源文件描述符。</param>
    /// <param name="buffer">接收数据的非托管字节缓冲区。</param>
    /// <param name="count">最多读取的字节数量。</param>
    /// <returns>实际读取的字节数量；文件结束时返回 0，失败时返回负值。</returns>
    [DllImport("libc", SetLastError = true, EntryPoint = "read")]
    public static extern nint Read(int fd, byte* buffer, nint count);


}