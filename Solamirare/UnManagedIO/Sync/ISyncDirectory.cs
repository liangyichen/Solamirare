namespace Solamirare;

/// <summary>
/// 跨平台文件夹操作
/// </summary>
internal unsafe interface ISyncDirectory
{
    /// <summary>
    /// 遍历文件夹下面的所有文件，不含子目录
    /// </summary>
    /// <param name="parentPath"></param>
    /// <param name="filenames">用于保存文件名的集合</param>
    /// <returns></returns>
    bool GetFilesNames(ReadOnlySpan<char> parentPath, UnManagedMemory<UnManagedString>* filenames);

    /// <summary>
    /// 遍历文件夹下面的所有文件，不含子目录
    /// </summary>
    /// <param name="parentPath"></param>
    /// <param name="action"></param>
    /// <param name="collection">指向集合形态的对象，用于保存文件内容</param>
    /// <returns></returns>
    bool GetFilesContents(ReadOnlySpan<char> parentPath, delegate*<ReadOnlySpan<char>, UnManagedString*, void*, void> action, void* collection);

    /// <summary>
    /// 检测文件夹是否存在，如果不存在则创建
    /// </summary>
    /// <param name="parentPath"></param>
    /// <returns></returns>
    bool EnsureDirectoryExists(ReadOnlySpan<char> parentPath);
}