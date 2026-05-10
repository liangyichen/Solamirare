namespace Solamirare;


/// <summary>
/// 文件操作上下文
/// </summary>
public unsafe struct IO_URingContext
{
    internal int fd;

    internal int result;

    internal bool isDone;

    internal UnManagedMemory<byte>* readBuffer;
}
