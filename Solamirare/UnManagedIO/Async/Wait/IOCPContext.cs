using System.Runtime.CompilerServices;

namespace Solamirare;


/// <summary>
/// 文件操作上下文
/// </summary>
public unsafe struct IOCPContext
{
    internal void* hFile;
    internal void* hEvent;
    internal OVERLAPPED Overlapped;
    internal uint ErrorCode;

    internal bool isDone;
}
