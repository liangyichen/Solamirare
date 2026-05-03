using Solamirare; // 需要 MemoryMarshal
using System.Runtime.CompilerServices;

/// <summary>
/// 随机字符串生成器
/// </summary>
public static unsafe class RandomStringGenerator
{

    static RandomStringGenerator()
    {
        charsetLength = 87;

        char* alloc = stackalloc char[] {
            // 数字
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            // 大写字母
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
            // 小写字母
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            // 特殊符号
            '~', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '_', '+', ';', ':', '"', '\'', '\\', '|', '<', ',', '>', '.', '?', '/'
        };

        uint byteCount = sizeof(char) * charsetLength;

        CharacterPool = (char*)NativeMemory.AllocZeroed(byteCount);

        Unsafe.CopyBlock(CharacterPool, alloc, byteCount);
    }

    /// <summary>
    /// 定义包含所有可能字符的字符集，现在包括了指定的特殊符号
    /// </summary>
    private static char* CharacterPool;

    private static uint charsetLength;

    /// <summary>
    /// <para>生成一个包含字母、数字和特殊符号的随机字符串，并将结果写入到指定的内存地址。</para>
    /// <para>调用者必须保证指针和长度的有效性。</para>
    /// </summary>
    /// <param name="destination">指向目标内存块起始位置的指针。</param>
    /// <param name="destinationLength">目标内存块的长度（以字符为单位）。</param>
    public static unsafe bool Generate(char* destination, uint destinationLength)
    {

        if (destination == null || destinationLength <= 0)
        {
            return false;
        }

        RandomNumberGenerator rng = new RandomNumberGenerator();

        for (nint i = 0; i < destinationLength; i++)
        {
            int randomIndex = rng.Next(0, (int)charsetLength);

            // 直接通过指针将选中的字符写入目标内存
            *(destination + i) = CharacterPool[randomIndex];
        }

        return true;
    }

    /// <summary>
    /// <para>生成一个包含字母、数字和特殊符号的随机字符串</para>
    /// <para>外部调用者注意释放 UnManagedMemory</para>
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    public static UnManagedString Generate(uint length)
    {
        if (length <= 0) goto FAILURE;

        UnManagedString memory = new UnManagedString(length, length);

        bool success = Generate(memory.Pointer, memory.UsageSize);

        if (!success)
        {
            goto FAILURE;
        }

        return memory;

    FAILURE:

        return new UnManagedString();
    }
}