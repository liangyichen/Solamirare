using System.Security.Cryptography;

namespace Solamirare;

public static unsafe partial class NativeCrypto
{
    
    private static delegate* unmanaged<byte*, int, byte*, int, byte*, void> s_hmacSha256;

    private static delegate* unmanaged<byte*, int, byte*, int, byte*, void> s_hmacSha384;


    internal static void SetHmacSha256(delegate* unmanaged<byte*, int, byte*, int, byte*, void> function)
    {
        s_hmacSha256 = function;
    }


    internal static void SetHmacSha384(delegate* unmanaged<byte*, int, byte*, int, byte*, void> function)
    {
        s_hmacSha384 = function;
    }


    public static UnManagedString HmacSha256(ReadOnlySpan<char> key, ReadOnlySpan<byte> input)
    {
        EnsureInitialized();

        if (key.IsEmpty || input.IsEmpty)
            return UnManagedString.Empty;

        byte* output = stackalloc byte[32];

        int keyBufferCount = key.Length * 4;

        UnManagedMemory<byte> keyBytes;

        if (keyBufferCount < 4096)
        {
            byte* keyBuffer = stackalloc byte[keyBufferCount];

            keyBytes = new UnManagedMemory<byte>(keyBuffer, (uint) keyBufferCount, 0, MemoryTypeDefined.Stack);

            key.CopyToBytes(&keyBytes);
        }
        else
        {
            keyBytes = key.CopyToBytes();
        }

        fixed (byte* pInput = input)
        {
            s_hmacSha256(keyBytes.Pointer, key.Length, pInput, input.Length, output);
        }

        keyBytes.Dispose();

        UnManagedMemory<byte> hashMemory = new UnManagedMemory<byte>(output, 32, 32, MemoryTypeDefined.Stack);

        UnManagedString result = EncodeHex(hashMemory);

        return result;
    }


    public static UnManagedString HmacSha256(ReadOnlySpan<char> key, ReadOnlySpan<char> input)
    {
        if (key.IsEmpty || input.IsEmpty)
            return UnManagedString.Empty;

        UnManagedMemory<byte> inputBytes;

        int inputBufferCount = input.Length * 4;

        UnManagedString result;

        if (inputBufferCount < 4096)
        {
            byte* inputBuffer = stackalloc byte[inputBufferCount];

            inputBytes = new UnManagedMemory<byte>(inputBuffer, (uint) inputBufferCount, 0, MemoryTypeDefined.Stack);

            input.CopyToBytes(&inputBytes);
        }
        else
        {
            inputBytes = input.CopyToBytes();
        }

        result = HmacSha256(key, inputBytes);

        inputBytes.Dispose();

        return result;
    }


    public static UnManagedString HmacSha384(byte* key, int keyLength, byte* input, int inputLength)
    {
        if (key == null || keyLength < 1) return UnManagedString.Empty;
        if (input == null || inputLength < 1) return UnManagedString.Empty;

        byte* output = stackalloc byte[48];
        HmacSha384(key, keyLength, input, inputLength, output);
        UnManagedMemory<byte> hashMemory = new UnManagedMemory<byte>(output, 48, 48, MemoryTypeDefined.Stack);
        return EncodeHex(hashMemory);
    }


    static void HmacSha384(byte* key, int keyLength, byte* input, int inputLength, byte* output)
    {
        EnsureInitialized();

        if (key == null && keyLength != 0) throw new ArgumentNullException(nameof(key));
        if (input == null && inputLength != 0) throw new ArgumentNullException(nameof(input));
        if (keyLength < 0) throw new ArgumentOutOfRangeException(nameof(keyLength));
        if (inputLength < 0) throw new ArgumentOutOfRangeException(nameof(inputLength));
        if (output == null) throw new ArgumentNullException(nameof(output));

        s_hmacSha384(key, keyLength, input, inputLength, output);
    }

}