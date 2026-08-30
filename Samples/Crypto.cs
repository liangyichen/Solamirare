using System;

namespace Solamirare;

public static unsafe class EcdsaP256Test
{
    private const int PublicKeyLength = 65;
    private const int HashLength = 32;
    private const int SignatureLength = 64;

    // ============================================================
    // RFC 6979 Appendix A.2.5
    //
    // ECDSA P-256 / SHA-256
    //
    // Message:
    //     "sample"
    //
    // Public key:
    //     04 || X || Y
    //
    // Signature:
    //     r || s
    //
    // The KAT is used only for verification.
    // ============================================================

    private static readonly byte[] KatPublicKey =
    {
        0x04,

        // X
        0x60, 0xFE, 0xD4, 0xBA,
        0x25, 0x5A, 0x9D, 0x31,
        0xC9, 0x61, 0xEB, 0x74,
        0xC6, 0x35, 0x6D, 0x68,
        0xC0, 0x49, 0xB8, 0x92,
        0x3B, 0x61, 0xFA, 0x6C,
        0xE6, 0x69, 0x62, 0x2E,
        0x60, 0xF2, 0x9F, 0xB6,

        // Y
        0x79, 0x03, 0xFE, 0x10,
        0x08, 0xB8, 0xBC, 0x99,
        0xA4, 0x1A, 0xE9, 0xE9,
        0x56, 0x28, 0xBC, 0x64,
        0xF2, 0xF1, 0xB2, 0x0C,
        0x2D, 0x7E, 0x9F, 0x51,
        0x77, 0xA3, 0xC2, 0x94,
        0xD4, 0x46, 0x22, 0x99
    };


    private static readonly byte[] KatHash =
    {
        0xAF, 0x2B, 0xDB, 0xE1,
        0xAA, 0x9B, 0x6E, 0xC1,
        0xE2, 0xAD, 0xE1, 0xD6,
        0x94, 0xF4, 0x1F, 0xC7,
        0x1A, 0x83, 0x1D, 0x02,
        0x68, 0xE9, 0x89, 0x15,
        0x62, 0x11, 0x3D, 0x8A,
        0x62, 0xAD, 0xD1, 0xBF
    };


    private static readonly byte[] KatSignature =
    {
// r
0xEF, 0xD4, 0x8B, 0x2A,
0xAC, 0xB6, 0xA8, 0xFD,
0x11, 0x40, 0xDD, 0x9C,
0xD4, 0x5E, 0x81, 0xD6,
0x9D, 0x2C, 0x87, 0x7B,
0x56, 0xAA, 0xF9, 0x91,
0xC3, 0x4D, 0x0E, 0xA8,
0x4E, 0xAF, 0x37, 0x16,

        // s
        0xF7, 0xCB, 0x1C, 0x94,
        0x2D, 0x65, 0x7C, 0x41,
        0xD4, 0x36, 0xC7, 0xA1,
        0xB6, 0xE2, 0x9F, 0x65,
        0xF3, 0xE9, 0x00, 0xDB,
        0xB9, 0xAF, 0xF4, 0x06,
        0x4D, 0xC4, 0xAB, 0x2F,
        0x84, 0x3A, 0xCD, 0xA8
    };


    // ============================================================
    // Main test
    // ============================================================

    public static bool Run()
    {
        Console.WriteLine(
            "========================================");

        Console.WriteLine(
            "ECDSA-P256 Acceptance Test");

        Console.WriteLine(
            "========================================");

        Console.WriteLine();

        try
        {
            if (!TestGenerate())
                return Fail("Generate");

            if (!TestSignAndVerify())
                return Fail("Sign / Verify");

            if (!TestWrongPublicKey())
                return Fail("Wrong Public Key");

            if (!TestModifiedHash())
                return Fail("Modified Hash");

            if (!TestModifiedR())
                return Fail("Modified R");

            if (!TestModifiedS())
                return Fail("Modified S");

            if (!TestMultipleMessages())
                return Fail("Multiple Messages");

            if (!TestMultipleKeyPairs())
                return Fail("Multiple Key Pairs");

            if (!TestRfc6979Kat())
                return Fail("RFC 6979 KAT");

            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "ECDSA-P256 ACCEPTANCE TEST PASSED");

            Console.WriteLine(
                "========================================");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine();

            Console.WriteLine(
                $"ECDSA-P256 ACCEPTANCE TEST FAILED: {ex.Message}");

            return false;
        }
    }


    // ============================================================
    // 1. Key generation
    // ============================================================

    private static bool TestGenerate()
    {
        Console.WriteLine(
            "[1] Testing key generation...");

        byte* publicKey =
            stackalloc byte[PublicKeyLength];

        nint privateKey = 0;

        try
        {
            privateKey =
                NativeCrypto.EcdsaP256Generate(
                    publicKey);

            if (privateKey == 0)
                return false;

            if (publicKey[0] != 0x04)
                return false;

            Console.WriteLine(
                "    Private Handle : OK");

            Console.WriteLine(
                "    Public Length  : 65");

            Console.WriteLine(
                $"    Public Key     : {ToHex(publicKey, PublicKeyLength)}");

            Console.WriteLine();

            return true;
        }
        finally
        {
            if (privateKey != 0)
            {
                NativeCrypto.EcdsaP256Destroy(
                    privateKey);
            }
        }
    }


    // ============================================================
    // 2. Sign -> Verify
    // ============================================================

    private static bool TestSignAndVerify()
    {
        Console.WriteLine(
            "[2] Testing Sign -> Verify...");

        byte* publicKey =
            stackalloc byte[PublicKeyLength];

        byte* hash =
            stackalloc byte[HashLength];

        byte* signature =
            stackalloc byte[SignatureLength];

        nint privateKey = 0;

        try
        {
            privateKey =
                NativeCrypto.EcdsaP256Generate(
                    publicKey);

            FillTestHash(
                hash);

            int length =
                NativeCrypto.EcdsaP256Sign(
                    privateKey,
                    hash,
                    signature);

            if (length != SignatureLength)
                return false;

            bool valid =
                NativeCrypto.EcdsaP256Verify(
                    publicKey,
                    hash,
                    signature);

            Console.WriteLine(
                $"    Signature Length : {length}");

            Console.WriteLine(
                $"    Signature        : {ToHex(signature, SignatureLength)}");

            Console.WriteLine(
                $"    Verify           : {valid}");

            Console.WriteLine();

            return valid;
        }
        finally
        {
            if (privateKey != 0)
            {
                NativeCrypto.EcdsaP256Destroy(
                    privateKey);
            }
        }
    }


    // ============================================================
    // 3. Wrong public key must fail
    // ============================================================

    private static bool TestWrongPublicKey()
    {
        Console.WriteLine(
            "[3] Testing wrong public key rejection...");

        byte* alicePublicKey =
            stackalloc byte[PublicKeyLength];

        byte* bobPublicKey =
            stackalloc byte[PublicKeyLength];

        byte* hash =
            stackalloc byte[HashLength];

        byte* signature =
            stackalloc byte[SignatureLength];

        nint alicePrivateKey = 0;
        nint bobPrivateKey = 0;

        try
        {
            alicePrivateKey =
                NativeCrypto.EcdsaP256Generate(
                    alicePublicKey);

            bobPrivateKey =
                NativeCrypto.EcdsaP256Generate(
                    bobPublicKey);

            FillTestHash(
                hash);

            int length =
                NativeCrypto.EcdsaP256Sign(
                    alicePrivateKey,
                    hash,
                    signature);

            if (length != SignatureLength)
                return false;

            bool valid =
                NativeCrypto.EcdsaP256Verify(
                    bobPublicKey,
                    hash,
                    signature);

            Console.WriteLine(
                $"    Verify with Bob key : {valid}");

            Console.WriteLine();

            return !valid;
        }
        finally
        {
            if (alicePrivateKey != 0)
            {
                NativeCrypto.EcdsaP256Destroy(
                    alicePrivateKey);
            }

            if (bobPrivateKey != 0)
            {
                NativeCrypto.EcdsaP256Destroy(
                    bobPrivateKey);
            }
        }
    }


    // ============================================================
    // 4. Modified hash must fail
    // ============================================================

    private static bool TestModifiedHash()
    {
        Console.WriteLine(
            "[4] Testing modified hash rejection...");

        byte* publicKey =
            stackalloc byte[PublicKeyLength];

        byte* hash =
            stackalloc byte[HashLength];

        byte* modifiedHash =
            stackalloc byte[HashLength];

        byte* signature =
            stackalloc byte[SignatureLength];

        nint privateKey = 0;

        try
        {
            privateKey =
                NativeCrypto.EcdsaP256Generate(
                    publicKey);

            FillTestHash(
                hash);

            for (int i = 0; i < HashLength; i++)
            {
                modifiedHash[i] =
                    hash[i];
            }

            int length =
                NativeCrypto.EcdsaP256Sign(
                    privateKey,
                    hash,
                    signature);

            if (length != SignatureLength)
                return false;

            modifiedHash[0] ^= 0x01;

            bool valid =
                NativeCrypto.EcdsaP256Verify(
                    publicKey,
                    modifiedHash,
                    signature);

            Console.WriteLine(
                $"    Modified hash valid : {valid}");

            Console.WriteLine();

            return !valid;
        }
        finally
        {
            if (privateKey != 0)
            {
                NativeCrypto.EcdsaP256Destroy(
                    privateKey);
            }
        }
    }


    // ============================================================
    // 5. Modified R must fail
    // ============================================================

    private static bool TestModifiedR()
    {
        Console.WriteLine(
            "[5] Testing modified R rejection...");

        byte* publicKey =
            stackalloc byte[PublicKeyLength];

        byte* hash =
            stackalloc byte[HashLength];

        byte* signature =
            stackalloc byte[SignatureLength];

        nint privateKey = 0;

        try
        {
            privateKey =
                NativeCrypto.EcdsaP256Generate(
                    publicKey);

            FillTestHash(
                hash);

            int length =
                NativeCrypto.EcdsaP256Sign(
                    privateKey,
                    hash,
                    signature);

            if (length != SignatureLength)
                return false;

            signature[0] ^= 0x01;

            bool valid =
                NativeCrypto.EcdsaP256Verify(
                    publicKey,
                    hash,
                    signature);

            Console.WriteLine(
                $"    Modified R valid : {valid}");

            Console.WriteLine();

            return !valid;
        }
        finally
        {
            if (privateKey != 0)
            {
                NativeCrypto.EcdsaP256Destroy(
                    privateKey);
            }
        }
    }


    // ============================================================
    // 6. Modified S must fail
    // ============================================================

    private static bool TestModifiedS()
    {
        Console.WriteLine(
            "[6] Testing modified S rejection...");

        byte* publicKey =
            stackalloc byte[PublicKeyLength];

        byte* hash =
            stackalloc byte[HashLength];

        byte* signature =
            stackalloc byte[SignatureLength];

        nint privateKey = 0;

        try
        {
            privateKey =
                NativeCrypto.EcdsaP256Generate(
                    publicKey);

            FillTestHash(
                hash);

            int length =
                NativeCrypto.EcdsaP256Sign(
                    privateKey,
                    hash,
                    signature);

            if (length != SignatureLength)
                return false;

            signature[SignatureLength - 1] ^= 0x01;

            bool valid =
                NativeCrypto.EcdsaP256Verify(
                    publicKey,
                    hash,
                    signature);

            Console.WriteLine(
                $"    Modified S valid : {valid}");

            Console.WriteLine();

            return !valid;
        }
        finally
        {
            if (privateKey != 0)
            {
                NativeCrypto.EcdsaP256Destroy(
                    privateKey);
            }
        }
    }


    // ============================================================
    // 7. Multiple messages
    // ============================================================

    private static bool TestMultipleMessages()
    {
        Console.WriteLine(
            "[7] Testing multiple Sign / Verify operations...");

        byte* publicKey =
            stackalloc byte[PublicKeyLength];

        byte* hash =
            stackalloc byte[HashLength];

        byte* signature =
            stackalloc byte[SignatureLength];

        nint privateKey = 0;

        try
        {
            privateKey =
                NativeCrypto.EcdsaP256Generate(
                    publicKey);

            for (int round = 0; round < 100; round++)
            {
                for (int i = 0; i < HashLength; i++)
                {
                    hash[i] =
                        (byte)(round + i);
                }

                int length =
                    NativeCrypto.EcdsaP256Sign(
                        privateKey,
                        hash,
                        signature);

                if (length != SignatureLength)
                    return false;

                bool valid =
                    NativeCrypto.EcdsaP256Verify(
                        publicKey,
                        hash,
                        signature);

                if (!valid)
                    return false;
            }

            Console.WriteLine(
                "    100 Sign / Verify operations : PASS");

            Console.WriteLine();

            return true;
        }
        finally
        {
            if (privateKey != 0)
            {
                NativeCrypto.EcdsaP256Destroy(
                    privateKey);
            }
        }
    }


    // ============================================================
    // 8. Multiple independent key pairs
    // ============================================================

    private static bool TestMultipleKeyPairs()
    {
        Console.WriteLine(
            "[8] Testing multiple independent key pairs...");

        byte* publicKey =
            stackalloc byte[PublicKeyLength];

        byte* hash =
            stackalloc byte[HashLength];

        byte* signature =
            stackalloc byte[SignatureLength];

        FillTestHash(
            hash);

        for (int round = 0; round < 100; round++)
        {
            nint privateKey =
                NativeCrypto.EcdsaP256Generate(
                    publicKey);

            try
            {
                int length =
                    NativeCrypto.EcdsaP256Sign(
                        privateKey,
                        hash,
                        signature);

                if (length != SignatureLength)
                    return false;

                bool valid =
                    NativeCrypto.EcdsaP256Verify(
                        publicKey,
                        hash,
                        signature);

                if (!valid)
                    return false;
            }
            finally
            {
                NativeCrypto.EcdsaP256Destroy(
                    privateKey);
            }
        }

        Console.WriteLine(
            "    100 independent key lifecycles : PASS");

        Console.WriteLine();

        return true;
    }


    // ============================================================
    // 9. RFC 6979 P-256 KAT
    // ============================================================

    private static bool TestRfc6979Kat()
    {
        Console.WriteLine(
            "[9] Testing RFC 6979 P-256 KAT...");

        byte* publicKey =
            stackalloc byte[PublicKeyLength];

        byte* hash =
            stackalloc byte[HashLength];

        byte* signature =
            stackalloc byte[SignatureLength];

        for (int i = 0; i < PublicKeyLength; i++)
        {
            publicKey[i] =
                KatPublicKey[i];
        }

        for (int i = 0; i < HashLength; i++)
        {
            hash[i] =
                KatHash[i];
        }

        for (int i = 0; i < SignatureLength; i++)
        {
            signature[i] =
                KatSignature[i];
        }

        // --------------------------------------------------------
        // Original RFC 6979 signature must verify.
        // --------------------------------------------------------

        bool valid =
            NativeCrypto.EcdsaP256Verify(
                publicKey,
                hash,
                signature);

        Console.WriteLine(
            $"    RFC 6979 valid signature : {valid}");

        if (!valid)
            return false;

        // --------------------------------------------------------
        // Modified R must fail.
        // --------------------------------------------------------

        signature[0] ^= 0x01;

        bool modifiedR =
            NativeCrypto.EcdsaP256Verify(
                publicKey,
                hash,
                signature);

        Console.WriteLine(
            $"    Modified R rejection      : {!modifiedR}");

        if (modifiedR)
            return false;

        signature[0] ^= 0x01;

        // --------------------------------------------------------
        // Modified S must fail.
        // --------------------------------------------------------

        signature[SignatureLength - 1] ^= 0x01;

        bool modifiedS =
            NativeCrypto.EcdsaP256Verify(
                publicKey,
                hash,
                signature);

        Console.WriteLine(
            $"    Modified S rejection      : {!modifiedS}");

        if (modifiedS)
            return false;

        signature[SignatureLength - 1] ^= 0x01;

        // --------------------------------------------------------
        // Modified hash must fail.
        // --------------------------------------------------------

        hash[0] ^= 0x01;

        bool modifiedHash =
            NativeCrypto.EcdsaP256Verify(
                publicKey,
                hash,
                signature);

        Console.WriteLine(
            $"    Modified hash rejection   : {!modifiedHash}");

        if (modifiedHash)
            return false;

        Console.WriteLine();

        return true;
    }


    // ============================================================
    // Fixed test hash for generated-key tests
    // ============================================================

    private static void FillTestHash(
        byte* hash)
    {
        for (int i = 0; i < HashLength; i++)
        {
            hash[i] =
                (byte)(i + 1);
        }
    }


    // ============================================================
    // Failure
    // ============================================================

    private static bool Fail(
        string test)
    {
        Console.WriteLine();

        Console.WriteLine(
            $"ECDSA-P256 TEST FAILED: {test}");

        return false;
    }


    // ============================================================
    // Hex output
    // ============================================================

    private static string ToHex(
        byte* data,
        int length)
    {
        const string hex =
            "0123456789ABCDEF";

        char[] result =
            new char[length * 2];

        for (int i = 0; i < length; i++)
        {
            byte value =
                data[i];

            result[i * 2] =
                hex[value >> 4];

            result[i * 2 + 1] =
                hex[value & 0x0F];
        }

        return new string(result);
    }
}

public static unsafe class CryptoSample
{





    public static void HkdfSha256Example()
    {
        const string ikmText = "input-key-material";
        const string saltText = "my-salt";
        const string infoText = "my-info";

        byte* ikm = stackalloc byte[ikmText.Length];
        byte* salt = stackalloc byte[saltText.Length];
        byte* info = stackalloc byte[infoText.Length];

        for (int i = 0; i < ikmText.Length; i++)
            ikm[i] = (byte)ikmText[i];

        for (int i = 0; i < saltText.Length; i++)
            salt[i] = (byte)saltText[i];

        for (int i = 0; i < infoText.Length; i++)
            info[i] = (byte)infoText[i];

        byte* output = stackalloc byte[32];

        NativeCrypto.HkdfSha256(
            ikm,
            ikmText.Length,
            salt,
            saltText.Length,
            info,
            infoText.Length,
            output,
            32);

        Console.WriteLine("HKDF-SHA256:");

        for (int i = 0; i < 32; i++)
        {
            int high = output[i] >> 4;
            int low = output[i] & 0x0F;

            Console.Write(
                high < 10
                    ? (char)('0' + high)
                    : (char)('a' + high - 10));

            Console.Write(
                low < 10
                    ? (char)('0' + low)
                    : (char)('a' + low - 10));
        }

        Console.WriteLine();
    }


    public static void HkdfSha384Example()
    {
        const string ikmText = "input-key-material";
        const string saltText = "my-salt";
        const string infoText = "my-info";

        byte* ikm = stackalloc byte[ikmText.Length];
        byte* salt = stackalloc byte[saltText.Length];
        byte* info = stackalloc byte[infoText.Length];

        for (int i = 0; i < ikmText.Length; i++)
            ikm[i] = (byte)ikmText[i];

        for (int i = 0; i < saltText.Length; i++)
            salt[i] = (byte)saltText[i];

        for (int i = 0; i < infoText.Length; i++)
            info[i] = (byte)infoText[i];

        byte* output = stackalloc byte[48];

        NativeCrypto.HkdfSha384(
            ikm,
            ikmText.Length,
            salt,
            saltText.Length,
            info,
            infoText.Length,
            output,
            48);

        Console.WriteLine("HKDF-SHA384:");

        for (int i = 0; i < 48; i++)
        {
            int high = output[i] >> 4;
            int low = output[i] & 0x0F;

            Console.Write(
                high < 10
                    ? (char)('0' + high)
                    : (char)('a' + high - 10));

            Console.Write(
                low < 10
                    ? (char)('0' + low)
                    : (char)('a' + low - 10));
        }

        Console.WriteLine();
    }

    public static void Run()
    {
        bool ecdsaResult =
            EcdsaP256Test.Run();

        Console.WriteLine(
            $"ECDSA-P256: {ecdsaResult}");
    }

}