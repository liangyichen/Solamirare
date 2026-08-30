using System.Security.Cryptography;

namespace Solamirare;

public static unsafe partial class NativeCrypto
{
    
    private static delegate* unmanaged<
        byte*,
        byte*,
        byte*,
        int,
        byte*,
        int,
        byte*,
        byte*,
        int>
        s_aes256GcmEncrypt;

    private static delegate* unmanaged<byte*, byte*, byte*, int, byte*, int, byte*, byte*, int> s_aes256GcmDecrypt;

    private static delegate* unmanaged<byte*, byte*, byte*, int, byte*, int, byte*, byte*, int> s_aes128GcmEncrypt;

    private static delegate* unmanaged<byte*, byte*, byte*, int, byte*, int, byte*, byte*, int> s_aes128GcmDecrypt;

    internal static void SetAes256GcmEncrypt(delegate* unmanaged<byte*, byte*, byte*, int, byte*, int, byte*, byte*, int> function)
    {
        s_aes256GcmEncrypt = function;
    }


    internal static void SetAes256GcmDecrypt(delegate* unmanaged<byte*, byte*, byte*, int, byte*, int, byte*, byte*, int> function)
    {
        s_aes256GcmDecrypt = function;
    }


    internal static void SetAes128GcmEncrypt(delegate* unmanaged<byte*, byte*, byte*, int, byte*, int, byte*, byte*, int> function)
    {
        s_aes128GcmEncrypt = function;
    }


    internal static void SetAes128GcmDecrypt(delegate* unmanaged<byte*, byte*, byte*, int, byte*, int, byte*, byte*, int> function)
    {
        s_aes128GcmDecrypt = function;
    }


    public static void Aes256GcmEncrypt(byte* key, byte* nonce, byte* aad, int aadLength, byte* plaintext, int plaintextLength, byte* ciphertext, byte* tag)
    {
        ValidateAesParameters(key, nonce, aad, aadLength, plaintext, plaintextLength, ciphertext);
        if (tag == null) throw new ArgumentNullException();
        s_aes256GcmEncrypt(key, nonce, aad, aadLength, plaintext, plaintextLength, ciphertext, tag);
    }

    public static void Aes256GcmDecrypt(byte* key, byte* nonce, byte* aad, int aadLength, byte* ciphertext, int ciphertextLength, byte* tag, byte* plaintext)
    {
        ValidateAesParameters(key, nonce, aad, aadLength, ciphertext, ciphertextLength, plaintext);
        int result = s_aes256GcmDecrypt(key, nonce, aad, aadLength, ciphertext, ciphertextLength, tag, plaintext);
        if (result < 0) throw new CryptographicException("AES-256-GCM authentication failed.");
    }


    public static void Aes128GcmEncrypt(byte* key, byte* nonce, byte* aad, int aadLength, byte* plaintext, int plaintextLength, byte* ciphertext, byte* tag)
    {
        ValidateAesParameters(key, nonce, aad, aadLength, plaintext, plaintextLength, ciphertext);
        if (tag == null) throw new ArgumentNullException();
        s_aes128GcmEncrypt(key, nonce, aad, aadLength, plaintext, plaintextLength, ciphertext, tag);
    }


    public static void Aes128GcmDecrypt(byte* key, byte* nonce, byte* aad, int aadLength, byte* ciphertext, int ciphertextLength, byte* tag, byte* plaintext)
    {
        ValidateAesParameters(key, nonce, aad, aadLength, ciphertext, ciphertextLength, plaintext);
        int result = s_aes128GcmDecrypt(key, nonce, aad, aadLength, ciphertext, ciphertextLength, tag, plaintext);
        if (result < 0) throw new CryptographicException("AES-128-GCM authentication failed.");
    }

    private static void ValidateAesParameters(byte* key, byte* nonce, byte* aad, int aadLength, byte* input, int inputLength, byte* output)
    {
        EnsureInitialized();
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (nonce == null) throw new ArgumentNullException(nameof(nonce));
        if (aad == null && aadLength != 0) throw new ArgumentNullException(nameof(aad));
        if (aadLength < 0) throw new ArgumentOutOfRangeException(nameof(aadLength));
        if (inputLength < 0) throw new ArgumentOutOfRangeException(nameof(inputLength));
    }

    public static UnManagedString Aes256Encrypt(UnManagedString value, UnManagedString key)
    {
        uint maxBytes = checked(value.UsageSize * 4);

        if (maxBytes < StackBufferSize)
        {
            byte* valueBuffer = stackalloc byte[(int) StackBufferSize];

            UnManagedMemory<byte> valueBytes = new UnManagedMemory<byte>(valueBuffer, StackBufferSize, 0, MemoryTypeDefined.Stack);

            bool copied = value.CopyToBytes(&valueBytes);

            if (!copied)
            {
                throw new ArgumentException(
                    "Failed to convert value to UTF-8 bytes.",
                    nameof(value));
            }

            uint encryptedCapacity = checked(valueBytes.UsageSize + 28);

            byte* encryptedBuffer = stackalloc byte[(int) StackBufferSize];

            UnManagedMemory<byte> encryptedBytes = new UnManagedMemory<byte>(encryptedBuffer, StackBufferSize, encryptedCapacity, MemoryTypeDefined.Stack);

            Aes256GcmEncryptInto(valueBytes, key, &encryptedBytes);

            return EncodeHex(encryptedBytes);
        }

        {
            UnManagedMemory<byte> valueBytes = value.CopyToBytes();

            UnManagedMemory<byte> encryptedBytes = default;

            try
            {
                encryptedBytes =
                    Aes256Encrypt(valueBytes, key);

                return EncodeHex(encryptedBytes);
            }
            finally
            {
                encryptedBytes.Dispose();
                valueBytes.Dispose();
            }
        }
    }

    public static UnManagedString Aes128Encrypt(UnManagedString value, UnManagedString key)
    {
        uint maxBytes = checked(value.UsageSize * 4);

        if (maxBytes < StackBufferSize)
        {
            byte* valueBuffer = stackalloc byte[(int) StackBufferSize];

            UnManagedMemory<byte> valueBytes = new UnManagedMemory<byte>(valueBuffer, StackBufferSize, 0, MemoryTypeDefined.Stack);

            bool copied = value.CopyToBytes(&valueBytes);

            if (!copied)
            {
                throw new ArgumentException(
                    "Failed to convert value to UTF-8 bytes.",
                    nameof(value));
            }

            uint encryptedCapacity = checked(valueBytes.UsageSize + 28);

            byte* encryptedBuffer = stackalloc byte[(int) StackBufferSize];

            UnManagedMemory<byte> encryptedBytes = new UnManagedMemory<byte>(encryptedBuffer, StackBufferSize, encryptedCapacity, MemoryTypeDefined.Stack);

            Aes128GcmEncryptInto(valueBytes, key, &encryptedBytes);

            return EncodeHex(encryptedBytes);
        }

        {
            UnManagedMemory<byte> valueBytes = value.CopyToBytes();

            UnManagedMemory<byte> encryptedBytes = default;

            try
            {
                encryptedBytes =
                    Aes128Encrypt(valueBytes, key);

                return EncodeHex(encryptedBytes);
            }
            finally
            {
                encryptedBytes.Dispose();
                valueBytes.Dispose();
            }
        }
    }

    public static UnManagedString Aes256Decrypt(UnManagedString value, UnManagedString key)
    {
        uint hexLength = value.UsageSize;

        if ((hexLength & 1) != 0)
        {
            throw new ArgumentException(
                "Invalid hexadecimal encrypted data.",
                nameof(value));
        }

        uint encryptedSize = hexLength / 2;

        if (encryptedSize < 28)
        {
            throw new ArgumentException(
                "Invalid AES-256-GCM encrypted data.",
                nameof(value));
        }

        uint plaintextSize = encryptedSize - 28;

        if (encryptedSize <= StackBufferSize &&
            plaintextSize <= StackBufferSize)
        {
            byte* encryptedBuffer = stackalloc byte[(int) StackBufferSize];

            UnManagedMemory<byte> encryptedBytes = new UnManagedMemory<byte>(encryptedBuffer, StackBufferSize, 0, MemoryTypeDefined.Stack);

            DecodeHex(value, &encryptedBytes);

            byte* plaintextBuffer = stackalloc byte[(int) StackBufferSize];

            UnManagedMemory<byte> plaintextBytes = new UnManagedMemory<byte>(plaintextBuffer, StackBufferSize, 0, MemoryTypeDefined.Stack);

            plaintextBytes.ReLength(plaintextSize);

            Aes256GcmDecryptInto(encryptedBytes, key, &plaintextBytes);

            return plaintextBytes.CopyToChars();
        }

        {
            UnManagedMemory<byte> encryptedBytes = DecodeHex(value);

            UnManagedMemory<byte> plaintextBytes = default;

            try
            {
                plaintextBytes =
                    Aes256Decrypt(encryptedBytes, key);

                return plaintextBytes.CopyToChars();
            }
            finally
            {
                plaintextBytes.Dispose();
                encryptedBytes.Dispose();
            }
        }
    }

    public static UnManagedString Aes128Decrypt(UnManagedString value, UnManagedString key)
    {
        uint hexLength = value.UsageSize;

        if ((hexLength & 1) != 0)
        {
            throw new ArgumentException(
                "Invalid hexadecimal encrypted data.",
                nameof(value));
        }

        uint encryptedSize = hexLength / 2;

        if (encryptedSize < 28)
        {
            throw new ArgumentException(
                "Invalid AES-128-GCM encrypted data.",
                nameof(value));
        }

        uint plaintextSize = encryptedSize - 28;

        if (encryptedSize <= StackBufferSize &&
            plaintextSize <= StackBufferSize)
        {
            byte* encryptedBuffer = stackalloc byte[(int) StackBufferSize];

            UnManagedMemory<byte> encryptedBytes = new UnManagedMemory<byte>(encryptedBuffer, StackBufferSize, 0, MemoryTypeDefined.Stack);

            DecodeHex(value, &encryptedBytes);

            byte* plaintextBuffer = stackalloc byte[(int) StackBufferSize];

            UnManagedMemory<byte> plaintextBytes = new UnManagedMemory<byte>(plaintextBuffer, StackBufferSize, 0, MemoryTypeDefined.Stack);

            plaintextBytes.ReLength(plaintextSize);

            Aes128GcmDecryptInto(encryptedBytes, key, &plaintextBytes);

            return plaintextBytes.CopyToChars();
        }

        {
            UnManagedMemory<byte> encryptedBytes = DecodeHex(value);

            UnManagedMemory<byte> plaintextBytes = default;

            try
            {
                plaintextBytes =
                    Aes128Decrypt(encryptedBytes, key);

                return plaintextBytes.CopyToChars();
            }
            finally
            {
                plaintextBytes.Dispose();
                encryptedBytes.Dispose();
            }
        }
    }

    public static UnManagedMemory<byte> Aes256Encrypt(UnManagedMemory<byte> value, UnManagedString key)
    {
        byte* keyHash = stackalloc byte[32];
        HashKey(key, keyHash);
        UnManagedMemory<byte> derivedKey = new UnManagedMemory<byte>(keyHash, 32, 32, MemoryTypeDefined.Stack);
        return Aes256Encrypt(value, derivedKey);
    }

    public static UnManagedMemory<byte> Aes128Encrypt(UnManagedMemory<byte> value, UnManagedString key)
    {
        byte* keyHash = stackalloc byte[16];
        HashKey(key, keyHash);
        UnManagedMemory<byte> derivedKey = new UnManagedMemory<byte>(keyHash, 16, 16, MemoryTypeDefined.Stack);
        return Aes128Encrypt(value, derivedKey);
    }

    public static UnManagedMemory<byte> Aes256Decrypt(UnManagedMemory<byte> value, UnManagedString key)
    {
        byte* keyHash = stackalloc byte[32];
        HashKey(key, keyHash);
        UnManagedMemory<byte> derivedKey = new UnManagedMemory<byte>(keyHash, 32, 32, MemoryTypeDefined.Stack);
        return Aes256Decrypt(value, derivedKey);
    }

    public static UnManagedMemory<byte> Aes128Decrypt(UnManagedMemory<byte> value, UnManagedString key)
    {
        byte* keyHash = stackalloc byte[16];
        HashKey(key, keyHash);
        UnManagedMemory<byte> derivedKey = new UnManagedMemory<byte>(keyHash, 16, 16, MemoryTypeDefined.Stack);
        return Aes128Decrypt(value, derivedKey);
    }

    public static UnManagedMemory<byte> Aes256Encrypt(UnManagedMemory<byte> value, UnManagedMemory<byte> key)
    {
        if (key.UsageSize != 32) throw new ArgumentException("AES-256-GCM requires a 32-byte key.", nameof(key));
        int plaintextLength = checked((int) value.UsageSize);
        int encryptedLength = checked(plaintextLength + 28);
        return AllocateAndExecute(value, key, encryptedLength, &Aes256GcmEncryptInto);
    }

    public static UnManagedMemory<byte> Aes128Encrypt(UnManagedMemory<byte> value, UnManagedMemory<byte> key)
    {
        if (key.UsageSize != 16) throw new ArgumentException("AES-128-GCM requires a 16-byte key.", nameof(key));
        int plaintextLength = checked((int) value.UsageSize);
        int encryptedLength = checked(plaintextLength + 28);
        return AllocateAndExecute(value, key, encryptedLength, &Aes128GcmEncryptInto);
    }

    public static UnManagedMemory<byte> Aes256Decrypt(UnManagedMemory<byte> value, UnManagedMemory<byte> key)
    {
        if (key.UsageSize != 32) throw new ArgumentException("AES-256-GCM requires a 32-byte key.", nameof(key));
        int encryptedLength = checked((int) value.UsageSize);
        if (encryptedLength < 28) throw new ArgumentException("Invalid AES-256-GCM encrypted data.", nameof(value));
        int plaintextLength = encryptedLength - 28;
        return AllocateAndExecute(value, key, plaintextLength, &Aes256GcmDecryptInto);
    }

    public static UnManagedMemory<byte> Aes128Decrypt(UnManagedMemory<byte> value, UnManagedMemory<byte> key)
    {
        if (key.UsageSize != 16) throw new ArgumentException("AES-128-GCM requires a 16-byte key.", nameof(key));
        int encryptedLength = checked((int) value.UsageSize);
        if (encryptedLength < 28) throw new ArgumentException("Invalid AES-128-GCM encrypted data.", nameof(value));
        int plaintextLength = encryptedLength - 28;
        return AllocateAndExecute(value, key, plaintextLength, &Aes128GcmDecryptInto);
    }

    private static void HashKey(UnManagedString key, byte* output)
    {
        UnManagedMemory<byte> keyBytes = key.CopyToBytes();
        try { Sha256(keyBytes.Pointer, checked((int) keyBytes.UsageSize), output); }
        finally { keyBytes.Dispose(); }
    }

    private static UnManagedMemory<byte> AllocateAndExecute(UnManagedMemory<byte> value, UnManagedMemory<byte> key, int outputLength, delegate*<UnManagedMemory<byte>, UnManagedMemory<byte>, UnManagedMemory<byte>*, void> operation)
    {
        UnManagedMemory<byte> result = new UnManagedMemory<byte>((uint) outputLength, (uint) outputLength);
        try
        {
            operation(value, key, &result);
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    private static void Aes256GcmEncryptInto(UnManagedMemory<byte> value, UnManagedString key, UnManagedMemory<byte>* output)
    {
        byte* keyHash = stackalloc byte[32];

        HashKey(key, keyHash);

        UnManagedMemory<byte> derivedKey = new UnManagedMemory<byte>(keyHash, 32, 32, MemoryTypeDefined.Stack);

        Aes256GcmEncryptInto(value, derivedKey, output);
    }

    private static void Aes128GcmEncryptInto(UnManagedMemory<byte> value, UnManagedString key, UnManagedMemory<byte>* output)
    {
        byte* keyHash = stackalloc byte[32];

        HashKey(key, keyHash);

        UnManagedMemory<byte> derivedKey = new UnManagedMemory<byte>(keyHash, 16, 16, MemoryTypeDefined.Stack);

        Aes128GcmEncryptInto(value, derivedKey, output);
    }

    private static void Aes128GcmEncryptInto(UnManagedMemory<byte> value, UnManagedMemory<byte> key, UnManagedMemory<byte>* output)
    {
        if (key.UsageSize != 16)
            throw new ArgumentException("AES-128-GCM requires a 16-byte key.", nameof(key));

        int plaintextLength = checked((int) value.UsageSize);
        int requiredLength = checked(plaintextLength + 28);

        if (output == null || !output->Activated || output->Capacity < (uint) requiredLength)
            throw new ArgumentException("Output buffer is too small.", nameof(output));

        byte* outputPointer = output->Pointer;
        byte* nonce = outputPointer;
        byte* ciphertext = outputPointer + 12;
        byte* tag = ciphertext + plaintextLength;

        Random(nonce, 12);

        Aes128GcmEncrypt(
            key.Pointer,
            nonce,
            null,
            0,
            value.Pointer,
            plaintextLength,
            ciphertext,
            tag);

        output->ReLength((uint) requiredLength);
    }

    private static void Aes256GcmEncryptInto(UnManagedMemory<byte> value, UnManagedMemory<byte> key, UnManagedMemory<byte>* output)
    {
        if (key.UsageSize != 32)
        {
            throw new ArgumentException("AES-256-GCM requires a 32-byte key.", nameof(key));
        }

        int plaintextLength = checked((int) value.UsageSize);

        int requiredLength = checked(plaintextLength + 28);

        if (output == null || !output->Activated || output->Capacity < (uint) requiredLength)
        {
            throw new ArgumentException("Output buffer is too small.", nameof(output));
        }

        byte* outputPointer = output->Pointer;

        byte* nonce = outputPointer;

        byte* ciphertext = outputPointer + 12;

        byte* tag = ciphertext + plaintextLength;

        Random(nonce, 12);

        Aes256GcmEncrypt(key.Pointer, nonce, null, 0, value.Pointer, plaintextLength, ciphertext, tag);

        output->ReLength((uint) requiredLength);
    }


    // ============================================================
    // DecryptInto
    // ============================================================

    private static void Aes256GcmDecryptInto(UnManagedMemory<byte> value, UnManagedString key, UnManagedMemory<byte>* output)
    {
        byte* keyHash = stackalloc byte[32];
        HashKey(key, keyHash);
        UnManagedMemory<byte> derivedKey = new UnManagedMemory<byte>(keyHash, 32, 32, MemoryTypeDefined.Stack);
        Aes256GcmDecryptInto(value, derivedKey, output);
    }

    private static void Aes128GcmDecryptInto(UnManagedMemory<byte> value, UnManagedString key, UnManagedMemory<byte>* output)
    {
        byte* keyHash = stackalloc byte[32];
        HashKey(key, keyHash);

        UnManagedMemory<byte> derivedKey = new UnManagedMemory<byte>(keyHash, 16, 16, MemoryTypeDefined.Stack);

        Aes128GcmDecryptInto(value, derivedKey, output);
    }

    static void Aes256GcmDecryptInto(UnManagedMemory<byte> value, UnManagedMemory<byte> key, UnManagedMemory<byte>* output)
    {
        if (key.UsageSize != 32)
        {
            throw new ArgumentException(
                "AES-256-GCM requires a 32-byte key.",
                nameof(key));
        }

        int encryptedLength = checked((int) value.UsageSize);

        if (encryptedLength < 28)
        {
            throw new ArgumentException(
                "Invalid AES-256-GCM encrypted data.",
                nameof(value));
        }

        int plaintextLength = encryptedLength - 28;

        if (output == null ||
            !output->Activated ||
            output->Capacity < (uint) plaintextLength)
        {
            throw new ArgumentException(
                "Output buffer is too small.",
                nameof(output));
        }

        byte* valuePointer = value.Pointer;

        byte* nonce = valuePointer;

        byte* ciphertext = valuePointer + 12;

        byte* tag = ciphertext + plaintextLength;

        Aes256GcmDecrypt(key.Pointer, nonce, null, 0, ciphertext, plaintextLength, tag, output->Pointer);

        output->ReLength((uint) plaintextLength);
    }

    private static void Aes128GcmDecryptInto(UnManagedMemory<byte> value, UnManagedMemory<byte> key, UnManagedMemory<byte>* output)
    {
        if (key.UsageSize != 16)
            throw new ArgumentException("AES-128-GCM requires a 16-byte key.", nameof(key));

        int encryptedLength = checked((int) value.UsageSize);

        if (encryptedLength < 28)
            throw new ArgumentException("Invalid AES-128-GCM encrypted data.", nameof(value));

        int plaintextLength = encryptedLength - 28;

        if (output == null || !output->Activated || output->Capacity < (uint) plaintextLength)
            throw new ArgumentException("Output buffer is too small.", nameof(output));

        byte* valuePointer = value.Pointer;

        byte* nonce = valuePointer;
        byte* ciphertext = valuePointer + 12;
        byte* tag = ciphertext + plaintextLength;

        Aes128GcmDecrypt(
            key.Pointer,
            nonce,
            null,
            0,
            ciphertext,
            plaintextLength,
            tag,
            output->Pointer);

        output->ReLength((uint) plaintextLength);
    }

}