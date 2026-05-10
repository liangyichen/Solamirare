namespace Solamirare;


/// <summary>
/// 跨平台IO操作
/// </summary>
public unsafe interface ISyncFilesIO
{

    /// <summary>
    /// 获取应用程序所在目录
    /// </summary>
    UnManagedString AppContextBaseDirectory();


    /// <summary>
    /// 判断文件是否存在
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    bool FileExists(ReadOnlySpan<char> filePath);

    /// <summary>
    /// 读取文件内容，自行确保外部的 UnManagedMemory&lt;char&gt; 具备安全容量
    /// <para>在不确定外部传入的 UnManagedMemory&lt;char&gt; 结果对象是否能够容纳文件容量时，因为可能会发生扩容操作（堆内存模式），它的 Pointer 地址有可能会改变</para>
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="externalMemory"></param>
    /// <param name="stackBufferSize">设置内部使用栈空间作为 Buffer 读取的容量</param>
    /// <returns></returns>
    bool ReadTextFile(ReadOnlySpan<char> filePath, UnManagedString* externalMemory, uint stackBufferSize = 1024);


    /// <summary>
    /// 读取文件大小，单位是 Bytes
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    int FileBytesSize(ReadOnlySpan<char> path);

    /// <summary>
    /// 把文本内容写入文件
    /// </summary>
    /// <param name="content"></param>
    /// <param name="filePath"></param>
    bool WriteTextToFile(ReadOnlySpan<char> content, ReadOnlySpan<char> filePath);

    /// <summary>
    /// 追加文件内容
    /// </summary>
    /// <param name="content"></param>
    /// <param name="filePath"></param>
    /// <returns></returns>
    bool AppendText(ReadOnlySpan<char> content, ReadOnlySpan<char> filePath);


    /// <summary>
    /// 把字节内容写入文件
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="bytesLength"></param>
    /// <param name="filePath"></param>
    bool WriteBytesToFile(byte* bytes, uint bytesLength, ReadOnlySpan<char> filePath);

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    bool DeleteFile(ReadOnlySpan<char> path);

}