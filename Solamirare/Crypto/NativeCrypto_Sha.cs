using System.Security.Cryptography;

namespace Solamirare;

public static unsafe partial class NativeCrypto
{

    private static delegate* unmanaged<byte*, int, byte*, void> s_sha256;


    private static delegate* unmanaged<byte*, int, byte*, void> s_sha384;


    internal static void SetSha256(delegate* unmanaged<byte*, int, byte*, void> function)
    {
        s_sha256 = function;
    }


    internal static void SetSha384(delegate* unmanaged<byte*, int, byte*, void> function)
    {
        s_sha384 = function;
    }



    static void Sha256(byte* input, int inputLength, byte* output)
    {
        EnsureInitialized();

        if (input == null && inputLength != 0)
            throw new ArgumentNullException(nameof(input));

        if (inputLength < 0)
            throw new ArgumentOutOfRangeException(nameof(inputLength));

        if (output == null)
            throw new ArgumentNullException(nameof(output));

        s_sha256(input, inputLength, output);
    }


    static void Sha384(byte* input, int inputLength, byte* output)
    {
        EnsureInitialized();

        if (input == null && inputLength != 0)
            throw new ArgumentNullException(nameof(input));

        if (inputLength < 0)
            throw new ArgumentOutOfRangeException(nameof(inputLength));

        if (output == null)
            throw new ArgumentNullException(nameof(output));

        s_sha384(input, inputLength, output);
    }


    public static UnManagedString Sha384(byte* input, int inputLength)
    {
        if (input is null || inputLength < 1) return UnManagedString.Empty;

        byte* hash = stackalloc byte[48];

        UnManagedMemory<byte> hashMemory = new UnManagedMemory<byte>(hash, 48, 48, MemoryTypeDefined.Stack);

        Sha384(input, inputLength, hash);

        return EncodeHex(hashMemory);
    }


    public static UnManagedString Sha256(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty) return UnManagedString.Empty;

        UnManagedMemory<byte> input;

        int bufferSize = text.Length * 4;

        UnManagedString result;

        if (bufferSize < 1024)
        {
            byte* stackBuffer = stackalloc byte[bufferSize];

            input = new UnManagedMemory<byte>(stackBuffer, 4096, 0, MemoryTypeDefined.Stack);

            text.CopyToBytes(&input);

            result = Sha256(input.Pointer, (int) input.UsageSize);
        }
        else
        {
            input = text.CopyToBytes();

            result = Sha256(input.Pointer, (int) input.UsageSize);

            input.Dispose();
        }

        return result;
    }


    public static UnManagedString Sha256(byte* input, int inputLength)
    {
        if (input is null || inputLength < 1) return UnManagedString.Empty;

        byte* hash = stackalloc byte[32];

        UnManagedMemory<byte> hashMemory = new UnManagedMemory<byte>(hash, 32, 32, MemoryTypeDefined.Stack);

        Sha256(input, inputLength, hash);

        UnManagedString result = EncodeHex(hashMemory);

        return result;
    }


}