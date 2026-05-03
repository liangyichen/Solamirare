using Solamirare;


/// <summary>
/// Solamirare环境变量
/// </summary>
public static class SolamirareEnvironment
{


    internal const string UnManagedMemoryGuid = "9ED54F84-A89D-0000-0000-000000000000";

    internal const string ValueDictionaryGuid = "9ED54F84-A89D-0000-0003-000000000000";

    internal const string UnManagedCollectionGuid = "9ED54F84-A89D-0000-0005-000000000000";

    internal const string ValueLinkedListGuid = "9ED54F84-A89D-0000-0002-000000000000";

    internal const string ValueFrozenStackGuid = "9ED54F84-A89D-0000-0006-000000000000";

    internal const string ValueStackGuid = "9ED54F84-A89D-0000-0007-000000000000";

    internal const string CircularDequeGuid = "9ED54F84-A89D-0000-0008-000000000000";



    /// <summary>
    /// 非托管字符串类型
    /// </summary>
    public static readonly Type Type_UnManagedMemory_Char;


    /// <summary>
    /// 非托管字符串类型
    /// </summary>
    public static readonly Type Type_UnManagedCollection_Char;


    readonly static ulong memoryPoolSize;


    /// <summary>内存对齐字节数，所有分配都按此对齐。</summary>
    public const nuint ALIGNMENT = 64;



    /// <summary>
    /// 内存池初始化容量
    /// </summary>
    public static ulong MemoryPoolSize
    {
        get
        {
            return memoryPoolSize;
        }
    }



    static SolamirareEnvironment()
    {

        memoryPoolSize = 10 * 1024 * 1024;

        //---------

        Type_UnManagedMemory_Char = typeof(UnManagedString);

        Type_UnManagedCollection_Char = typeof(UnManagedCollection<char>);

    }



}