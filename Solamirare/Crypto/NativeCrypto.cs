using System.Security.Cryptography;

namespace Solamirare;


public static unsafe partial class NativeCrypto
{
    private const uint StackBufferSize = 4096;

    private static bool s_initialized;

    private static delegate* unmanaged<byte*, int, void> s_random;

    private static delegate* unmanaged<byte*, nint*, void> s_ecdheP256Generate;

    private static delegate* unmanaged<nint, byte*, byte*, int> s_ecdheP256Derive;

    private static delegate* unmanaged<nint, void> s_ecdheP256Destroy;

    private static delegate* unmanaged<byte*, nint*, void>
        s_ecdsaP256Generate;

    private static delegate* unmanaged<
        nint,
        byte*,
        byte*,
        int>
        s_ecdsaP256Sign;

    private static delegate* unmanaged<
    byte*,
    byte*,
    byte*,
    int>
    s_ecdsaP256Verify;

    private static delegate* unmanaged<nint, void>
        s_ecdsaP256Destroy;

    // ============================================================
    // Initialize
    // ============================================================

    public static void Initialize()
    {
        if (s_initialized)
            return;

        if (OperatingSystem.IsWindows())
        {
            NativeCryptoWindows.Initialize();
        }
        else if (OperatingSystem.IsLinux())
        {
            NativeCryptoLinux.Initialize();
        }
        else if (OperatingSystem.IsMacOS())
        {
            NativeCryptoMacOS.Initialize();
        }
        else
        {
            throw new PlatformNotSupportedException(
                "NativeCrypto supports Windows, Linux and macOS.");
        }

        s_initialized = true;
    }


    private static void EnsureInitialized()
    {
        if (!s_initialized)
            Initialize();
    }


    // ============================================================
    // Native function registration
    // ============================================================

    internal static void SetRandom(
        delegate* unmanaged<byte*, int, void> function)
    {
        s_random = function;
    }


    internal static void SetEcdheP256Generate(
        delegate* unmanaged<byte*, nint*, void> function)
    {
        s_ecdheP256Generate = function;
    }


    internal static void SetEcdheP256Derive(
        delegate* unmanaged<nint, byte*, byte*, int> function)
    {
        s_ecdheP256Derive = function;
    }


    internal static void SetEcdheP256Destroy(
        delegate* unmanaged<nint, void> function)
    {
        s_ecdheP256Destroy = function;
    }



    internal static void SetEcdsaP256Generate(
        delegate* unmanaged<byte*, nint*, void> function)
    {
        s_ecdsaP256Generate = function;
    }


    internal static void SetEcdsaP256Sign(
        delegate* unmanaged<
            nint,
            byte*,
            byte*,
            int> function)
    {
        s_ecdsaP256Sign = function;
    }


    internal static void SetEcdsaP256Verify(
        delegate* unmanaged<
            byte*,
            byte*,
            byte*,
            int> function)
    {
        s_ecdsaP256Verify = function;
    }


    internal static void SetEcdsaP256Destroy(
        delegate* unmanaged<nint, void> function)
    {
        s_ecdsaP256Destroy = function;
    }



    // ============================================================
    // Random
    // ============================================================

    static void Random(
        byte* output,
        int length)
    {
        EnsureInitialized();

        if (output == null)
            throw new ArgumentNullException(nameof(output));

        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        if (length == 0)
            return;

        s_random(
            output,
            length);
    }


    // ============================================================
    // ECDHE-P256
    // ============================================================

    /// <summary>
    /// Generates an ephemeral P-256 ECDH key pair.
    ///
    /// publicKey receives the 65-byte uncompressed point:
    ///
    /// 04 || X(32) || Y(32)
    ///
    /// The returned native handle represents the private key and
    /// must be released with EcdheP256Destroy.
    /// </summary>
    public static nint EcdheP256Generate(
        byte* publicKey)
    {
        EnsureInitialized();

        if (publicKey == null)
            throw new ArgumentNullException(nameof(publicKey));

        nint privateKey = 0;

        s_ecdheP256Generate(
            publicKey,
            &privateKey);

        if (privateKey == 0)
        {
            throw new CryptographicException(
                "ECDHE-P256 key generation failed.");
        }

        return privateKey;
    }


    /// <summary>
    /// Derives the P-256 ECDH shared secret.
    ///
    /// peerPublicKey must contain:
    ///
    /// 04 || X(32) || Y(32)
    ///
    /// sharedSecret receives exactly 32 bytes.
    /// </summary>
    public static int EcdheP256Derive(
        nint privateKey,
        byte* peerPublicKey,
        byte* sharedSecret)
    {
        EnsureInitialized();

        if (privateKey == 0)
            throw new ArgumentNullException(nameof(privateKey));

        if (peerPublicKey == null)
            throw new ArgumentNullException(nameof(peerPublicKey));

        if (sharedSecret == null)
            throw new ArgumentNullException(nameof(sharedSecret));

        return s_ecdheP256Derive(
            privateKey,
            peerPublicKey,
            sharedSecret);
    }


    /// <summary>
    /// Releases an ECDHE-P256 private-key handle.
    /// </summary>
    public static void EcdheP256Destroy(
        nint privateKey)
    {
        if (privateKey == 0)
            return;

        EnsureInitialized();

        s_ecdheP256Destroy(
            privateKey);
    }




    // ============================================================
    // ECDSA-P256
    // ============================================================

    /// <summary>
    /// Generates a P-256 ECDSA key pair.
    ///
    /// publicKey receives the 65-byte uncompressed point:
    ///
    /// 04 || X(32) || Y(32)
    ///
    /// The returned native handle represents the private key and
    /// must be released with EcdsaP256Destroy.
    /// </summary>
    public static nint EcdsaP256Generate(
        byte* publicKey)
    {
        EnsureInitialized();

        if (publicKey == null)
            throw new ArgumentNullException(nameof(publicKey));

        nint privateKey = 0;

        s_ecdsaP256Generate(
            publicKey,
            &privateKey);

        if (privateKey == 0)
        {
            throw new CryptographicException(
                "ECDSA-P256 key generation failed.");
        }

        return privateKey;
    }


    /// <summary>
    /// Signs a 32-byte SHA-256 digest using ECDSA-P256.
    ///
    /// signature receives a 64-byte IEEE P1363 signature:
    ///
    /// R(32) || S(32)
    ///
    /// Returns the signature length, which is always 64.
    /// </summary>
    public static int EcdsaP256Sign(
        nint privateKey,
        byte* hash,
        byte* signature)
    {
        EnsureInitialized();

        if (privateKey == 0)
            throw new ArgumentNullException(nameof(privateKey));

        if (hash == null)
            throw new ArgumentNullException(nameof(hash));

        if (signature == null)
            throw new ArgumentNullException(nameof(signature));

        return s_ecdsaP256Sign(
            privateKey,
            hash,
            signature);
    }


    /// <summary>
    /// Verifies a 64-byte IEEE P1363 ECDSA-P256 signature.
    ///
    /// hash must contain exactly 32 bytes.
    /// signature must contain:
    ///
    /// R(32) || S(32)
    /// </summary>
    public static bool EcdsaP256Verify(
        byte* publicKey,
        byte* hash,
        byte* signature)
    {
        EnsureInitialized();

        if (publicKey == null)
            throw new ArgumentNullException(nameof(publicKey));

        if (hash == null)
            throw new ArgumentNullException(nameof(hash));

        if (signature == null)
            throw new ArgumentNullException(nameof(signature));

        return s_ecdsaP256Verify(
            publicKey,
            hash,
            signature) != 0;
    }


    /// <summary>
    /// Releases an ECDSA-P256 private-key handle.
    /// </summary>
    public static void EcdsaP256Destroy(
        nint privateKey)
    {
        if (privateKey == 0)
            return;

        EnsureInitialized();

        s_ecdsaP256Destroy(
            privateKey);
    }



    // ============================================================
    // HKDF
    // ============================================================

    public static void HkdfSha256(
        byte* ikm,
        int ikmLength,
        byte* salt,
        int saltLength,
        byte* info,
        int infoLength,
        byte* output,
        int outputLength)
    {
        EnsureInitialized();

        Hkdf(
            ikm,
            ikmLength,
            salt,
            saltLength,
            info,
            infoLength,
            output,
            outputLength,
            s_hmacSha256,
            32);
    }


    public static void HkdfSha384(
        byte* ikm,
        int ikmLength,
        byte* salt,
        int saltLength,
        byte* info,
        int infoLength,
        byte* output,
        int outputLength)
    {
        EnsureInitialized();

        Hkdf(
            ikm,
            ikmLength,
            salt,
            saltLength,
            info,
            infoLength,
            output,
            outputLength,
            s_hmacSha384,
            48);
    }


    private static void Hkdf(
        byte* ikm,
        int ikmLength,
        byte* salt,
        int saltLength,
        byte* info,
        int infoLength,
        byte* output,
        int outputLength,
        delegate* unmanaged<byte*, int, byte*, int, byte*, void> hmac,
        int hashLength)
    {
        EnsureInitialized();

        if (ikm == null && ikmLength != 0)
            throw new ArgumentNullException(nameof(ikm));

        if (salt == null && saltLength != 0)
            throw new ArgumentNullException(nameof(salt));

        if (info == null && infoLength != 0)
            throw new ArgumentNullException(nameof(info));

        if (output == null && outputLength != 0)
            throw new ArgumentNullException(nameof(output));

        if (ikmLength < 0)
            throw new ArgumentOutOfRangeException(nameof(ikmLength));

        if (saltLength < 0)
            throw new ArgumentOutOfRangeException(nameof(saltLength));

        if (infoLength < 0)
            throw new ArgumentOutOfRangeException(nameof(infoLength));

        if (outputLength < 0 ||
            outputLength > 255 * hashLength)
            throw new ArgumentOutOfRangeException(nameof(outputLength));

        if (outputLength == 0)
            return;

        byte* zeroSalt =
            stackalloc byte[48];

        for (int i = 0; i < hashLength; i++)
            zeroSalt[i] = 0;

        byte* prk =
            stackalloc byte[48];

        hmac(
            saltLength == 0
                ? zeroSalt
                : salt,
            saltLength == 0
                ? hashLength
                : saltLength,
            ikm,
            ikmLength,
            prk);

        int messageLength =
            checked(hashLength + infoLength + 1);

        UnManagedMemory<byte> heapMessage =
            default;

        byte* message;

        if (messageLength <= (int) StackBufferSize)
        {
            byte* stackMessage =
                stackalloc byte[messageLength];

            message = stackMessage;
        }
        else
        {
            heapMessage =
                new UnManagedMemory<byte>(
                    (uint) messageLength,
                    (uint) messageLength);

            message =
                heapMessage.Pointer;
        }

        try
        {
            byte* previous =
                stackalloc byte[48];

            int previousLength = 0;

            int written = 0;

            byte counter = 1;

            while (written < outputLength)
            {
                if (previousLength > 0)
                {
                    for (int i = 0;
                         i < previousLength;
                         i++)
                    {
                        message[i] =
                            previous[i];
                    }
                }

                if (infoLength > 0)
                {
                    for (int i = 0;
                         i < infoLength;
                         i++)
                    {
                        message[
                            previousLength + i] =
                            info[i];
                    }
                }

                message[
                    previousLength +
                    infoLength] =
                    counter;

                hmac(
                    prk,
                    hashLength,
                    message,
                    previousLength +
                    infoLength +
                    1,
                    previous);

                int copyLength =
                    Math.Min(
                        hashLength,
                        outputLength - written);

                for (int i = 0;
                     i < copyLength;
                     i++)
                {
                    output[
                        written + i] =
                        previous[i];
                }

                written += copyLength;

                previousLength =
                    hashLength;

                counter++;
            }
        }
        finally
        {
            heapMessage.Dispose();
        }
    }


    // ============================================================
    // Hex Encode
    // ============================================================

    private static UnManagedString EncodeHex(
        UnManagedMemory<byte> input)
    {
        int inputLength =
            checked((int) input.UsageSize);

        int outputLength =
            checked(inputLength * 2);

        UnManagedString output =
            new UnManagedString(
                (uint) outputLength,
                (uint) outputLength);

        byte* source =
            input.Pointer;

        char* destination =
            output.Pointer;

        for (int i = 0;
             i < inputLength;
             i++)
        {
            byte value =
                source[i];

            int high =
                value >> 4;

            int low =
                value & 0x0F;

            destination[i * 2] =
                ToHexChar(high);

            destination[i * 2 + 1] =
                ToHexChar(low);
        }

        return output;
    }


    // ============================================================
    // Hex Decode
    // ============================================================

    private static UnManagedMemory<byte> DecodeHex(
        UnManagedMemory<char> input)
    {
        int inputLength =
            checked((int) input.UsageSize);

        if ((inputLength & 1) != 0)
        {
            throw new ArgumentException(
                "Invalid hexadecimal data.",
                nameof(input));
        }

        int outputLength =
            inputLength / 2;

        UnManagedMemory<byte> output =
            new UnManagedMemory<byte>(
                (uint) outputLength,
                0);

        try
        {
            DecodeHex(
                input,
                &output);

            return output;
        }
        catch
        {
            output.Dispose();

            throw;
        }
    }


    private static void DecodeHex(
        UnManagedMemory<char> input,
        UnManagedMemory<byte>* output)
    {
        int inputLength =
            checked((int) input.UsageSize);

        if ((inputLength & 1) != 0)
        {
            throw new ArgumentException(
                "Invalid hexadecimal data.",
                nameof(input));
        }

        int outputLength =
            inputLength / 2;

        if (output == null ||
            !output->Activated ||
            output->Capacity <
            (uint) outputLength)
        {
            throw new ArgumentException(
                "Output buffer is too small.",
                nameof(output));
        }

        char* source =
            input.Pointer;

        byte* destination =
            output->Pointer;

        for (int i = 0;
             i < outputLength;
             i++)
        {
            int high =
                HexValue(
                    source[i * 2]);

            int low =
                HexValue(
                    source[i * 2 + 1]);

            if (high < 0 ||
                low < 0)
            {
                throw new ArgumentException(
                    "Invalid hexadecimal data.",
                    nameof(input));
            }

            destination[i] =
                (byte) ((high << 4) | low);
        }

        output->ReLength(
            (uint) outputLength);
    }


    // ============================================================
    // Hex helpers
    // ============================================================

    private static char ToHexChar(
        int value)
    {
        if (value < 10)
            return (char) ('0' + value);

        return (char) ('A' + value - 10);
    }


    private static int HexValue(
        char value)
    {
        if (value >= '0' &&
            value <= '9')
            return value - '0';

        if (value >= 'A' &&
            value <= 'F')
            return value - 'A' + 10;

        if (value >= 'a' &&
            value <= 'f')
            return value - 'a' + 10;

        return -1;
    }
}