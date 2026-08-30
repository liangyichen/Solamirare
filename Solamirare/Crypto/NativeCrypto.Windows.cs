using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Solamirare;

internal static unsafe class NativeCryptoWindows
{
    private const int STATUS_SUCCESS = 0;

    private const int STATUS_AUTH_TAG_MISMATCH =
        unchecked((int) 0xC000A002);

    private const uint BCRYPT_USE_SYSTEM_PREFERRED_RNG = 2;

    private const uint BCRYPT_ALG_HANDLE_HMAC_FLAG = 8;

    private const uint BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO_VERSION = 1;


    // ============================================================
    // ECDHE-P256 constants
    // ============================================================

    private const uint BCRYPT_ECDH_P256_KEY_LENGTH = 256;

    private const uint BCRYPT_ECDH_PUBLIC_P256_MAGIC =
        0x314B4345;

    private const uint BCRYPT_ECCPUBLIC_BLOB_LENGTH =
        72;

    private const uint BCRYPT_KDF_RAW_SECRET =
        0;


    private static readonly string s_ecdhP256AlgorithmName =
        "ECDH_P256";

    private static readonly string s_ecdsaP256AlgorithmName =
        "ECDSA_P256";

    private static readonly string s_eccPublicBlobName =
        "ECCPUBLICBLOB";

    private static readonly string s_rawSecretName =
        "TRUNCATE";


    private static nint s_bcrypt;

    private static nint s_aesAlgorithm;

    private static nint s_sha256Algorithm;

    private static nint s_sha384Algorithm;

    private static nint s_hmacSha256Algorithm;

    private static nint s_hmacSha384Algorithm;

    private static nint s_ecdhP256Algorithm;

    private static nint s_ecdsaP256Algorithm;


    // ============================================================
    // ECDSA-P256 constants
    // ============================================================

    private const uint BCRYPT_ECDSA_P256_KEY_LENGTH = 256;

    private const uint BCRYPT_ECDSA_PUBLIC_P256_MAGIC = 0x31534345;



    // ============================================================
    // BCrypt function pointers
    // ============================================================

    private static delegate* unmanaged<
        nint*,
        char*,
        char*,
        uint,
        int>
        BCryptOpenAlgorithmProvider;

    private static delegate* unmanaged<
        nint,
        uint,
        int>
        BCryptCloseAlgorithmProvider;

    private static delegate* unmanaged<
        nint,
        char*,
        byte*,
        uint,
        uint,
        int>
        BCryptSetProperty;

    private static delegate* unmanaged<
        nint,
        char*,
        byte*,
        uint,
        uint*,
        uint,
        int>
        BCryptGetProperty;

    private static delegate* unmanaged<
        nint,
        byte*,
        uint,
        uint,
        int>
        BCryptGenRandom;

    private static delegate* unmanaged<
        nint,
        nint*,
        byte*,
        uint,
        byte*,
        uint,
        uint,
        int>
        BCryptGenerateSymmetricKey;

    private static delegate* unmanaged<
        nint,
        byte*,
        uint,
        void*,
        byte*,
        uint,
        byte*,
        uint,
        uint*,
        uint,
        int>
        BCryptEncrypt;

    private static delegate* unmanaged<
        nint,
        byte*,
        uint,
        void*,
        byte*,
        uint,
        byte*,
        uint,
        uint*,
        uint,
        int>
        BCryptDecrypt;

    private static delegate* unmanaged<
        nint,
        int>
        BCryptDestroyKey;

    private static delegate* unmanaged<
        nint,
        nint*,
        byte*,
        uint,
        byte*,
        uint,
        uint,
        int>
        BCryptCreateHash;

    private static delegate* unmanaged<
        nint,
        byte*,
        uint,
        uint,
        int>
        BCryptHashData;

    private static delegate* unmanaged<
        nint,
        byte*,
        uint,
        uint,
        int>
        BCryptFinishHash;

    private static delegate* unmanaged<
        nint,
        int>
        BCryptDestroyHash;


    // ============================================================
    // ECDH function pointers
    // ============================================================

    /*
     * NTSTATUS BCryptGenerateKeyPair(
     *     BCRYPT_ALG_HANDLE hAlgorithm,
     *     BCRYPT_KEY_HANDLE *phKey,
     *     ULONG dwLength,
     *     ULONG dwFlags
     * );
     */
    private static delegate* unmanaged<
        nint,
        nint*,
        uint,
        uint,
        int>
        BCryptGenerateKeyPair;



    // ============================================================
    // ECDSA function pointers
    // ============================================================

    /*
     * NTSTATUS BCryptSignHash(
     *     BCRYPT_KEY_HANDLE hKey,
     *     VOID *pPaddingInfo,
     *     PUCHAR pbInput,
     *     ULONG cbInput,
     *     PUCHAR pbOutput,
     *     ULONG cbOutput,
     *     ULONG *pcbResult,
     *     ULONG dwFlags
     * );
     */
    private static delegate* unmanaged<
        nint,
        void*,
        byte*,
        uint,
        byte*,
        uint,
        uint*,
        uint,
        int>
        BCryptSignHash;


    /*
     * NTSTATUS BCryptVerifySignature(
     *     BCRYPT_KEY_HANDLE hKey,
     *     VOID *pPaddingInfo,
     *     PUCHAR pbHash,
     *     ULONG cbHash,
     *     PUCHAR pbSignature,
     *     ULONG cbSignature,
     *     ULONG dwFlags
     * );
     */
    private static delegate* unmanaged<
        nint,
        void*,
        byte*,
        uint,
        byte*,
        uint,
        uint,
        int>
        BCryptVerifySignature;




    /*
     * NTSTATUS BCryptFinalizeKeyPair(
     *     BCRYPT_KEY_HANDLE hKey,
     *     ULONG dwFlags
     * );
     */
    private static delegate* unmanaged<
        nint,
        uint,
        int>
        BCryptFinalizeKeyPair;


    /*
     * NTSTATUS BCryptExportKey(
     *     BCRYPT_KEY_HANDLE hKey,
     *     BCRYPT_KEY_HANDLE hExportKey,
     *     LPCWSTR pszBlobType,
     *     PUCHAR pbOutput,
     *     ULONG cbOutput,
     *     ULONG *pcbResult,
     *     ULONG dwFlags
     * );
     */
    private static delegate* unmanaged<
        nint,
        nint,
        char*,
        byte*,
        uint,
        uint*,
        uint,
        int>
        BCryptExportKey;


    /*
     * NTSTATUS BCryptImportKeyPair(
     *     BCRYPT_ALG_HANDLE hAlgorithm,
     *     BCRYPT_KEY_HANDLE hImportKey,
     *     LPCWSTR pszBlobType,
     *     BCRYPT_KEY_HANDLE *phKey,
     *     PUCHAR pbInput,
     *     ULONG cbInput,
     *     ULONG dwFlags
     * );
     */
    private static delegate* unmanaged<
        nint,
        nint,
        char*,
        nint*,
        byte*,
        uint,
        uint,
        int>
        BCryptImportKeyPair;


    /*
     * NTSTATUS BCryptSecretAgreement(
     *     BCRYPT_KEY_HANDLE hPrivKey,
     *     BCRYPT_KEY_HANDLE hPubKey,
     *     BCRYPT_SECRET_HANDLE *phAgreedSecret,
     *     ULONG dwFlags
     * );
     */
    private static delegate* unmanaged<
        nint,
        nint,
        nint*,
        uint,
        int>
        BCryptSecretAgreement;


    /*
     * NTSTATUS BCryptDeriveKey(
     *     BCRYPT_SECRET_HANDLE hSharedSecret,
     *     LPCWSTR pwszKDF,
     *     BCryptBufferDesc *pParameterList,
     *     PUCHAR pbDerivedKey,
     *     ULONG cbDerivedKey,
     *     ULONG *pcbResult,
     *     ULONG dwFlags
     * );
     */
    private static delegate* unmanaged<
        nint,
        char*,
        void*,
        byte*,
        uint,
        uint*,
        uint,
        int>
        BCryptDeriveKey;


    /*
     * NTSTATUS BCryptDestroySecret(
     *     BCRYPT_SECRET_HANDLE hSharedSecret
     * );
     */
    private static delegate* unmanaged<
        nint,
        int>
        BCryptDestroySecret;


    // ============================================================
    // Initialize
    // ============================================================

    internal static void Initialize()
    {
        if (s_bcrypt != 0)
            return;


        s_bcrypt =
            NativeLibrary.Load(
                "bcrypt.dll");


        BCryptOpenAlgorithmProvider =
            (delegate* unmanaged<
                nint*,
                char*,
                char*,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptOpenAlgorithmProvider");


        BCryptCloseAlgorithmProvider =
            (delegate* unmanaged<
                nint,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptCloseAlgorithmProvider");


        BCryptSetProperty =
            (delegate* unmanaged<
                nint,
                char*,
                byte*,
                uint,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptSetProperty");


        BCryptGetProperty =
            (delegate* unmanaged<
                nint,
                char*,
                byte*,
                uint,
                uint*,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptGetProperty");


        BCryptGenRandom =
            (delegate* unmanaged<
                nint,
                byte*,
                uint,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptGenRandom");


        BCryptGenerateSymmetricKey =
            (delegate* unmanaged<
                nint,
                nint*,
                byte*,
                uint,
                byte*,
                uint,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptGenerateSymmetricKey");


        BCryptEncrypt =
            (delegate* unmanaged<
                nint,
                byte*,
                uint,
                void*,
                byte*,
                uint,
                byte*,
                uint,
                uint*,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptEncrypt");


        BCryptDecrypt =
            (delegate* unmanaged<
                nint,
                byte*,
                uint,
                void*,
                byte*,
                uint,
                byte*,
                uint,
                uint*,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptDecrypt");


        BCryptDestroyKey =
            (delegate* unmanaged<
                nint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptDestroyKey");


        BCryptCreateHash =
            (delegate* unmanaged<
                nint,
                nint*,
                byte*,
                uint,
                byte*,
                uint,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptCreateHash");


        BCryptHashData =
            (delegate* unmanaged<
                nint,
                byte*,
                uint,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptHashData");


        BCryptFinishHash =
            (delegate* unmanaged<
                nint,
                byte*,
                uint,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptFinishHash");


        BCryptDestroyHash =
            (delegate* unmanaged<
                nint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptDestroyHash");


        // ========================================================
        // ECDH
        // ========================================================

        BCryptGenerateKeyPair =
            (delegate* unmanaged<
                nint,
                nint*,
                uint,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptGenerateKeyPair");


        BCryptFinalizeKeyPair =
            (delegate* unmanaged<
                nint,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptFinalizeKeyPair");


        BCryptExportKey =
            (delegate* unmanaged<
                nint,
                nint,
                char*,
                byte*,
                uint,
                uint*,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptExportKey");


        BCryptImportKeyPair =
            (delegate* unmanaged<
                nint,
                nint,
                char*,
                nint*,
                byte*,
                uint,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptImportKeyPair");


        BCryptSecretAgreement =
            (delegate* unmanaged<
                nint,
                nint,
                nint*,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptSecretAgreement");


        BCryptDeriveKey =
            (delegate* unmanaged<
                nint,
                char*,
                void*,
                byte*,
                uint,
                uint*,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptDeriveKey");


        BCryptDestroySecret =
            (delegate* unmanaged<
                nint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptDestroySecret");


        BCryptSignHash =
            (delegate* unmanaged<
                nint,
                void*,
                byte*,
                uint,
                byte*,
                uint,
                uint*,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptSignHash");


        BCryptVerifySignature =
            (delegate* unmanaged<
                nint,
                void*,
                byte*,
                uint,
                byte*,
                uint,
                uint,
                int>)
            NativeLibrary.GetExport(
                s_bcrypt,
                "BCryptVerifySignature");



        // ========================================================
        // Algorithms
        // ========================================================

        s_aesAlgorithm =
            OpenAlgorithm(
                "AES");


        s_sha256Algorithm =
            OpenAlgorithm(
                "SHA256");


        s_sha384Algorithm =
            OpenAlgorithm(
                "SHA384");


        s_hmacSha256Algorithm =
            OpenAlgorithm(
                "SHA256",
                BCRYPT_ALG_HANDLE_HMAC_FLAG);


        s_hmacSha384Algorithm =
            OpenAlgorithm(
                "SHA384",
                BCRYPT_ALG_HANDLE_HMAC_FLAG);


        /*
         * This is the important part for ECDHE-P256.
         *
         * Do NOT use "ECDH".
         * Do NOT use "ECDSA_P256".
         *
         * The CNG provider must be ECDH_P256.
         */
        s_ecdhP256Algorithm =
            OpenAlgorithm(
                s_ecdhP256AlgorithmName);


        s_ecdsaP256Algorithm =
            OpenAlgorithm(
                s_ecdsaP256AlgorithmName);


        SetProperty(
            s_aesAlgorithm,
            "ChainingMode",
            "ChainingModeGCM");


        // ========================================================
        // NativeCrypto registration
        // ========================================================

        NativeCrypto.SetRandom(
            &Random);

        NativeCrypto.SetSha256(
            &Sha256);

        NativeCrypto.SetSha384(
            &Sha384);

        NativeCrypto.SetHmacSha256(
            &HmacSha256);

        NativeCrypto.SetHmacSha384(
            &HmacSha384);

        NativeCrypto.SetAes256GcmEncrypt(
            &Aes256GcmEncrypt);

        NativeCrypto.SetAes256GcmDecrypt(
            &Aes256GcmDecrypt);

        NativeCrypto.SetAes128GcmEncrypt(
            &Aes128GcmEncrypt);

        NativeCrypto.SetAes128GcmDecrypt(
            &Aes128GcmDecrypt);

        NativeCrypto.SetEcdheP256Generate(
            &EcdheP256Generate);

        NativeCrypto.SetEcdheP256Derive(
            &EcdheP256Derive);

        NativeCrypto.SetEcdheP256Destroy(
            &EcdheP256Destroy);

        NativeCrypto.SetEcdsaP256Generate(
            &EcdsaP256Generate);

        NativeCrypto.SetEcdsaP256Sign(
            &EcdsaP256Sign);

        NativeCrypto.SetEcdsaP256Verify(
            &EcdsaP256Verify);

        NativeCrypto.SetEcdsaP256Destroy(
            &EcdsaP256Destroy);

    }


    // ============================================================
    // Algorithm helpers
    // ============================================================

    private static nint OpenAlgorithm(
        string name,
        uint flags = 0)
    {
        fixed (char* namePtr = name)
        {
            nint algorithm = 0;

            int status =
                BCryptOpenAlgorithmProvider(
                    &algorithm,
                    namePtr,
                    null,
                    flags);

            if (status != STATUS_SUCCESS)
            {
                throw new InvalidOperationException(
                    $"BCryptOpenAlgorithmProvider failed: 0x{status:X8}");
            }

            return algorithm;
        }
    }


    private static void SetProperty(
        nint algorithm,
        string property,
        string value)
    {
        fixed (char* propertyPtr = property)
        fixed (char* valuePtr = value)
        {
            int status =
                BCryptSetProperty(
                    algorithm,
                    propertyPtr,
                    (byte*) valuePtr,
                    (uint) ((value.Length + 1) * sizeof(char)),
                    0);

            if (status != STATUS_SUCCESS)
            {
                throw new InvalidOperationException(
                    $"BCryptSetProperty failed: 0x{status:X8}");
            }
        }
    }


    private static uint GetObjectLength(
        nint algorithm)
    {
        const string property =
            "ObjectLength";

        fixed (char* propertyPtr = property)
        {
            uint objectLength = 0;

            uint resultLength = 0;

            int status =
                BCryptGetProperty(
                    algorithm,
                    propertyPtr,
                    (byte*) &objectLength,
                    sizeof(uint),
                    &resultLength,
                    0);

            if (status != STATUS_SUCCESS)
            {
                throw new InvalidOperationException(
                    $"BCryptGetProperty failed: 0x{status:X8}");
            }

            return objectLength;
        }
    }


    // ============================================================
    // Random
    // ============================================================

    [UnmanagedCallersOnly]
    internal static void Random(
        byte* output,
        int length)
    {
        if (length <= 0)
            return;

        int status =
            BCryptGenRandom(
                0,
                output,
                (uint) length,
                BCRYPT_USE_SYSTEM_PREFERRED_RNG);

        if (status != STATUS_SUCCESS)
        {
            throw new InvalidOperationException(
                $"BCryptGenRandom failed: 0x{status:X8}");
        }
    }


    // ============================================================
    // SHA
    // ============================================================

    [UnmanagedCallersOnly]
    internal static void Sha256(
        byte* input,
        int inputLength,
        byte* output) =>
        Hash(
            s_sha256Algorithm,
            input,
            inputLength,
            output,
            32,
            "SHA256");


    [UnmanagedCallersOnly]
    internal static void Sha384(
        byte* input,
        int inputLength,
        byte* output) =>
        Hash(
            s_sha384Algorithm,
            input,
            inputLength,
            output,
            48,
            "SHA384");


    private static void Hash(
        nint algorithm,
        byte* input,
        int inputLength,
        byte* output,
        uint hashLength,
        string name)
    {
        nint hash = 0;

        int status =
            BCryptCreateHash(
                algorithm,
                &hash,
                null,
                0,
                null,
                0,
                0);

        if (status != STATUS_SUCCESS)
        {
            throw new InvalidOperationException(
                $"BCryptCreateHash({name}) failed: 0x{status:X8}");
        }

        try
        {
            if (inputLength > 0)
            {
                status =
                    BCryptHashData(
                        hash,
                        input,
                        (uint) inputLength,
                        0);

                if (status != STATUS_SUCCESS)
                {
                    throw new InvalidOperationException(
                        $"BCryptHashData({name}) failed: 0x{status:X8}");
                }
            }

            status =
                BCryptFinishHash(
                    hash,
                    output,
                    hashLength,
                    0);

            if (status != STATUS_SUCCESS)
            {
                throw new InvalidOperationException(
                    $"BCryptFinishHash({name}) failed: 0x{status:X8}");
            }
        }
        finally
        {
            BCryptDestroyHash(
                hash);
        }
    }


    // ============================================================
    // HMAC
    // ============================================================

    [UnmanagedCallersOnly]
    internal static void HmacSha256(
        byte* key,
        int keyLength,
        byte* input,
        int inputLength,
        byte* output) =>
        Hmac(
            s_hmacSha256Algorithm,
            key,
            keyLength,
            input,
            inputLength,
            output,
            32,
            "HMAC-SHA256");


    [UnmanagedCallersOnly]
    internal static void HmacSha384(
        byte* key,
        int keyLength,
        byte* input,
        int inputLength,
        byte* output) =>
        Hmac(
            s_hmacSha384Algorithm,
            key,
            keyLength,
            input,
            inputLength,
            output,
            48,
            "HMAC-SHA384");


    private static void Hmac(
        nint algorithm,
        byte* key,
        int keyLength,
        byte* input,
        int inputLength,
        byte* output,
        uint hashLength,
        string name)
    {
        nint hash = 0;

        int status =
            BCryptCreateHash(
                algorithm,
                &hash,
                null,
                0,
                key,
                (uint) keyLength,
                0);

        if (status != STATUS_SUCCESS)
        {
            throw new InvalidOperationException(
                $"BCryptCreateHash({name}) failed: 0x{status:X8}");
        }

        try
        {
            if (inputLength > 0)
            {
                status =
                    BCryptHashData(
                        hash,
                        input,
                        (uint) inputLength,
                        0);

                if (status != STATUS_SUCCESS)
                {
                    throw new InvalidOperationException(
                        $"BCryptHashData({name}) failed: 0x{status:X8}");
                }
            }

            status =
                BCryptFinishHash(
                    hash,
                    output,
                    hashLength,
                    0);

            if (status != STATUS_SUCCESS)
            {
                throw new InvalidOperationException(
                    $"BCryptFinishHash({name}) failed: 0x{status:X8}");
            }
        }
        finally
        {
            BCryptDestroyHash(
                hash);
        }
    }


    // ============================================================
    // ECDHE-P256 Generate
    // ============================================================

    [UnmanagedCallersOnly]
    internal static void EcdheP256Generate(
        byte* publicKey,
        nint* privateKey)
    {
        if (publicKey == null)
        {
            throw new ArgumentNullException(
                nameof(publicKey));
        }

        if (privateKey == null)
        {
            throw new ArgumentNullException(
                nameof(privateKey));
        }

        *privateKey = 0;


        nint key = 0;


        /*
         * ECDH_P256 requires exactly 256 bits.
         */
        int status =
            BCryptGenerateKeyPair(
                s_ecdhP256Algorithm,
                &key,
                BCRYPT_ECDH_P256_KEY_LENGTH,
                0);

        if (status != STATUS_SUCCESS)
        {
            throw new CryptographicException(
                $"BCryptGenerateKeyPair failed: 0x{status:X8}");
        }


        try
        {
            status =
                BCryptFinalizeKeyPair(
                    key,
                    0);

            if (status != STATUS_SUCCESS)
            {
                throw new CryptographicException(
                    $"BCryptFinalizeKeyPair failed: 0x{status:X8}");
            }


            /*
             * ECCPUBLICBLOB:
             *
             * BCRYPT_ECCKEY_BLOB
             * X[32]
             * Y[32]
             *
             * Total = 8 + 32 + 32 = 72 bytes.
             */
            byte* blob =
                stackalloc byte[
                    (int) BCRYPT_ECCPUBLIC_BLOB_LENGTH];


            uint resultLength = 0;


            fixed (char* blobType =
                s_eccPublicBlobName)
            {
                status =
                    BCryptExportKey(
                        key,
                        0,
                        blobType,
                        blob,
                        BCRYPT_ECCPUBLIC_BLOB_LENGTH,
                        &resultLength,
                        0);
            }


            if (status != STATUS_SUCCESS)
            {
                throw new CryptographicException(
                    $"BCryptExportKey failed: 0x{status:X8}");
            }


            if (resultLength !=
                BCRYPT_ECCPUBLIC_BLOB_LENGTH)
            {
                throw new CryptographicException(
                    $"Unexpected ECDH public key blob length: {resultLength}.");
            }


            uint magic =
                ReadUInt32(
                    blob);


            uint keySize =
                ReadUInt32(
                    blob + 4);


            if (magic !=
                BCRYPT_ECDH_PUBLIC_P256_MAGIC)
            {
                throw new CryptographicException(
                    $"Unexpected ECDH public key magic: 0x{magic:X8}");
            }


            if (keySize != 32)
            {
                throw new CryptographicException(
                    $"Unexpected ECDH P-256 key size: {keySize}.");
            }


            /*
             * Public key exposed by NativeCrypto:
             *
             * 04 || X || Y
             */
            publicKey[0] =
                0x04;


            for (int i = 0; i < 32; i++)
            {
                publicKey[1 + i] =
                    blob[8 + i];

                publicKey[33 + i] =
                    blob[40 + i];
            }


            *privateKey =
                key;

            key = 0;
        }
        finally
        {
            if (key != 0)
            {
                BCryptDestroyKey(
                    key);
            }
        }
    }


    // ============================================================
    // ECDHE-P256 Derive
    // ============================================================

    [UnmanagedCallersOnly]
    internal static int EcdheP256Derive(
        nint privateKey,
        byte* peerPublicKey,
        byte* sharedSecret)
    {
        if (privateKey == 0)
        {
            throw new ArgumentNullException(
                nameof(privateKey));
        }

        if (peerPublicKey == null)
        {
            throw new ArgumentNullException(
                nameof(peerPublicKey));
        }

        if (sharedSecret == null)
        {
            throw new ArgumentNullException(
                nameof(sharedSecret));
        }


        /*
         * NativeCrypto uses:
         *
         * 04 || X(32) || Y(32)
         */
        if (peerPublicKey[0] != 0x04)
        {
            throw new CryptographicException(
                "Invalid P-256 public key format.");
        }


        /*
         * Convert:
         *
         * 04 || X || Y
         *
         * into:
         *
         * BCRYPT_ECCKEY_BLOB
         * X
         * Y
         */
        byte* publicBlob =
            stackalloc byte[
                (int) BCRYPT_ECCPUBLIC_BLOB_LENGTH];


        WriteUInt32(
            publicBlob,
            BCRYPT_ECDH_PUBLIC_P256_MAGIC);


        WriteUInt32(
            publicBlob + 4,
            32);


        for (int i = 0; i < 32; i++)
        {
            publicBlob[8 + i] =
                peerPublicKey[1 + i];

            publicBlob[40 + i] =
                peerPublicKey[33 + i];
        }


        nint publicKeyHandle = 0;


        fixed (char* blobType =
            s_eccPublicBlobName)
        {
            int status =
                BCryptImportKeyPair(
                    s_ecdhP256Algorithm,
                    0,
                    blobType,
                    &publicKeyHandle,
                    publicBlob,
                    BCRYPT_ECCPUBLIC_BLOB_LENGTH,
                    0);

            if (status != STATUS_SUCCESS)
            {
                throw new CryptographicException(
                    $"BCryptImportKeyPair failed: 0x{status:X8}");
            }
        }


        nint secret = 0;


        try
        {
            /*
             * Both key handles must come from the same
             * CNG algorithm provider.
             */
            int status =
                BCryptSecretAgreement(
                    privateKey,
                    publicKeyHandle,
                    &secret,
                    0);

            if (status != STATUS_SUCCESS)
            {
                throw new CryptographicException(
                    $"BCryptSecretAgreement failed: 0x{status:X8}");
            }


            byte* rawSecret =
                stackalloc byte[32];


            uint resultLength = 0;


            fixed (char* kdf =
                s_rawSecretName)
            {
                status =
                    BCryptDeriveKey(
                        secret,
                        kdf,
                        null,
                        rawSecret,
                        32,
                        &resultLength,
                        0);
            }


            if (status != STATUS_SUCCESS)
            {
                throw new CryptographicException(
                    $"BCryptDeriveKey failed: 0x{status:X8}");
            }


            if (resultLength != 32)
            {
                throw new CryptographicException(
                    $"Unexpected ECDH shared secret length: {resultLength}.");
            }


            /*
             * BCryptDeriveKey RAW_SECRET returns the raw
             * secret in little-endian representation.
             *
             * TLS/ECDH representation here is:
             *
             * fixed 32-byte big-endian.
             *
             * Reverse the complete 32-byte value.
             */
            for (int i = 0; i < 32; i++)
            {
                sharedSecret[i] =
                    rawSecret[31 - i];
            }


            return 32;
        }
        finally
        {
            if (secret != 0)
            {
                BCryptDestroySecret(
                    secret);
            }

            BCryptDestroyKey(
                publicKeyHandle);
        }
    }


    // ============================================================
    // ECDHE-P256 Destroy
    // ============================================================

    [UnmanagedCallersOnly]
    internal static void EcdheP256Destroy(
        nint privateKey)
    {
        if (privateKey == 0)
            return;

        BCryptDestroyKey(
            privateKey);
    }



    // ============================================================
    // ECDSA-P256 Generate
    // ============================================================

    [UnmanagedCallersOnly]
    internal static void EcdsaP256Generate(
        byte* publicKey,
        nint* privateKey)
    {
        if (publicKey == null)
        {
            throw new ArgumentNullException(
                nameof(publicKey));
        }

        if (privateKey == null)
        {
            throw new ArgumentNullException(
                nameof(privateKey));
        }

        *privateKey = 0;

        nint key = 0;

        int status =
            BCryptGenerateKeyPair(
                s_ecdsaP256Algorithm,
                &key,
                BCRYPT_ECDSA_P256_KEY_LENGTH,
                0);

        if (status != STATUS_SUCCESS)
        {
            throw new CryptographicException(
                $"BCryptGenerateKeyPair(ECDSA_P256) failed: 0x{status:X8}");
        }

        try
        {
            status =
                BCryptFinalizeKeyPair(
                    key,
                    0);

            if (status != STATUS_SUCCESS)
            {
                throw new CryptographicException(
                    $"BCryptFinalizeKeyPair(ECDSA_P256) failed: 0x{status:X8}");
            }

            byte* blob =
                stackalloc byte[
                    (int) BCRYPT_ECCPUBLIC_BLOB_LENGTH];

            uint resultLength = 0;

            fixed (char* blobType =
                s_eccPublicBlobName)
            {
                status =
                    BCryptExportKey(
                        key,
                        0,
                        blobType,
                        blob,
                        BCRYPT_ECCPUBLIC_BLOB_LENGTH,
                        &resultLength,
                        0);
            }

            if (status != STATUS_SUCCESS)
            {
                throw new CryptographicException(
                    $"BCryptExportKey(ECDSA_P256) failed: 0x{status:X8}");
            }

            if (resultLength !=
                BCRYPT_ECCPUBLIC_BLOB_LENGTH)
            {
                throw new CryptographicException(
                    $"Unexpected ECDSA public key blob length: {resultLength}.");
            }

            uint magic =
                ReadUInt32(blob);

            uint keySize =
                ReadUInt32(blob + 4);

            if (magic !=
                BCRYPT_ECDSA_PUBLIC_P256_MAGIC)
            {
                throw new CryptographicException(
                    $"Unexpected ECDSA public key magic: 0x{magic:X8}");
            }

            if (keySize != 32)
            {
                throw new CryptographicException(
                    $"Unexpected ECDSA P-256 key size: {keySize}.");
            }

            publicKey[0] =
                0x04;

            for (int i = 0; i < 32; i++)
            {
                publicKey[1 + i] =
                    blob[8 + i];

                publicKey[33 + i] =
                    blob[40 + i];
            }

            *privateKey =
                key;

            key = 0;
        }
        finally
        {
            if (key != 0)
            {
                BCryptDestroyKey(
                    key);
            }
        }
    }


    // ============================================================
    // ECDSA-P256 Sign
    // ============================================================

    [UnmanagedCallersOnly]
    internal static int EcdsaP256Sign(
        nint privateKey,
        byte* hash,
        byte* signature)
    {
        if (privateKey == 0)
        {
            throw new ArgumentNullException(
                nameof(privateKey));
        }

        if (hash == null)
        {
            throw new ArgumentNullException(
                nameof(hash));
        }

        if (signature == null)
        {
            throw new ArgumentNullException(
                nameof(signature));
        }

        uint resultLength = 0;

        int status =
            BCryptSignHash(
                privateKey,
                null,
                hash,
                32,
                signature,
                64,
                &resultLength,
                0);

        if (status != STATUS_SUCCESS)
        {
            throw new CryptographicException(
                $"BCryptSignHash(ECDSA_P256) failed: 0x{status:X8}");
        }

        if (resultLength != 64)
        {
            throw new CryptographicException(
                $"Unexpected ECDSA-P256 signature length: {resultLength}.");
        }

        return 64;
    }


    // ============================================================
    // ECDSA-P256 Verify
    // ============================================================

    [UnmanagedCallersOnly]
    internal static int EcdsaP256Verify(
        byte* publicKey,
        byte* hash,
        byte* signature)
    {
        if (publicKey == null)
        {
            throw new ArgumentNullException(
                nameof(publicKey));
        }

        if (hash == null)
        {
            throw new ArgumentNullException(
                nameof(hash));
        }

        if (signature == null)
        {
            throw new ArgumentNullException(
                nameof(signature));
        }

        // P-256 uncompressed public key:
        //
        // 04 || X(32) || Y(32)
        //
        if (publicKey[0] != 0x04)
        {
            return 0;
        }

        // --------------------------------------------------------
        // Convert the 65-byte uncompressed public key into
        // BCRYPT_ECCKEY_BLOB format.
        //
        // BCRYPT_ECCPUBLIC_BLOB:
        //
        //   Magic      4 bytes
        //   cbKey      4 bytes
        //   X         32 bytes
        //   Y         32 bytes
        //
        // Total = 72 bytes
        // --------------------------------------------------------

        byte* publicBlob =
            stackalloc byte[
                (int) BCRYPT_ECCPUBLIC_BLOB_LENGTH];

        WriteUInt32(
            publicBlob,
            BCRYPT_ECDSA_PUBLIC_P256_MAGIC);

        WriteUInt32(
            publicBlob + 4,
            32);

        for (int i = 0; i < 32; i++)
        {
            publicBlob[8 + i] =
                publicKey[1 + i];

            publicBlob[40 + i] =
                publicKey[33 + i];
        }

        nint publicKeyHandle = 0;

        try
        {
            // ----------------------------------------------------
            // Import public key
            // ----------------------------------------------------

            fixed (char* blobType =
                s_eccPublicBlobName)
            {
                int status =
                    BCryptImportKeyPair(
                        s_ecdsaP256Algorithm,
                        0,
                        blobType,
                        &publicKeyHandle,
                        publicBlob,
                        BCRYPT_ECCPUBLIC_BLOB_LENGTH,
                        0);

                if (status != STATUS_SUCCESS)
                {
                    throw new CryptographicException(
                        $"BCryptImportKeyPair(ECDSA_P256) failed: 0x{status:X8}");
                }
            }

            // ----------------------------------------------------
            // Verify
            //
            // hash:
            //     32-byte SHA-256 digest
            //
            // signature:
            //     64-byte P1363 format
            //
            //     R(32) || S(32)
            // ----------------------------------------------------

            int verifyStatus =
                BCryptVerifySignature(
                    publicKeyHandle,
                    null,
                    hash,
                    32,
                    signature,
                    64,
                    0);

            if (verifyStatus == STATUS_SUCCESS)
            {
                return 1;
            }

            // STATUS_INVALID_SIGNATURE
            if (verifyStatus ==
                unchecked((int) 0xC000A000))
            {
                return 0;
            }

            throw new CryptographicException(
                $"BCryptVerifySignature(ECDSA_P256) failed: 0x{verifyStatus:X8}");
        }
        finally
        {
            if (publicKeyHandle != 0)
            {
                BCryptDestroyKey(
                    publicKeyHandle);
            }
        }
    }


    // ============================================================
    // ECDSA-P256 Destroy
    // ============================================================

    [UnmanagedCallersOnly]
    internal static void EcdsaP256Destroy(
        nint privateKey)
    {
        if (privateKey == 0)
            return;

        BCryptDestroyKey(
            privateKey);
    }


    // ============================================================
    // AES-GCM
    // ============================================================

    [UnmanagedCallersOnly]
    internal static int Aes256GcmEncrypt(
        byte* key,
        byte* nonce,
        byte* aad,
        int aadLength,
        byte* plaintext,
        int plaintextLength,
        byte* ciphertext,
        byte* tag) =>
        AesGcmEncrypt(
            key,
            32,
            nonce,
            aad,
            aadLength,
            plaintext,
            plaintextLength,
            ciphertext,
            tag);


    [UnmanagedCallersOnly]
    internal static int Aes128GcmEncrypt(
        byte* key,
        byte* nonce,
        byte* aad,
        int aadLength,
        byte* plaintext,
        int plaintextLength,
        byte* ciphertext,
        byte* tag) =>
        AesGcmEncrypt(
            key,
            16,
            nonce,
            aad,
            aadLength,
            plaintext,
            plaintextLength,
            ciphertext,
            tag);


    private static int AesGcmEncrypt(
        byte* key,
        int keyLength,
        byte* nonce,
        byte* aad,
        int aadLength,
        byte* plaintext,
        int plaintextLength,
        byte* ciphertext,
        byte* tag)
    {
        uint objectLength =
            GetObjectLength(
                s_aesAlgorithm);

        byte* keyObject =
            stackalloc byte[(int) objectLength];

        nint keyHandle = 0;


        int status =
            BCryptGenerateSymmetricKey(
                s_aesAlgorithm,
                &keyHandle,
                keyObject,
                objectLength,
                key,
                (uint) keyLength,
                0);

        if (status != STATUS_SUCCESS)
        {
            throw new InvalidOperationException(
                $"BCryptGenerateSymmetricKey failed: 0x{status:X8}");
        }


        try
        {
            BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO auth =
                CreateAuth(
                    nonce,
                    aad,
                    aadLength,
                    tag);


            uint resultLength = 0;


            status =
                BCryptEncrypt(
                    keyHandle,
                    plaintext,
                    (uint) plaintextLength,
                    &auth,
                    null,
                    0,
                    ciphertext,
                    (uint) plaintextLength,
                    &resultLength,
                    0);


            if (status != STATUS_SUCCESS)
            {
                throw new InvalidOperationException(
                    $"BCryptEncrypt failed: 0x{status:X8}");
            }


            return (int) resultLength;
        }
        finally
        {
            BCryptDestroyKey(
                keyHandle);
        }
    }


    [UnmanagedCallersOnly]
    internal static int Aes256GcmDecrypt(
        byte* key,
        byte* nonce,
        byte* aad,
        int aadLength,
        byte* ciphertext,
        int ciphertextLength,
        byte* tag,
        byte* plaintext) =>
        AesGcmDecrypt(
            key,
            32,
            nonce,
            aad,
            aadLength,
            ciphertext,
            ciphertextLength,
            tag,
            plaintext);


    [UnmanagedCallersOnly]
    internal static int Aes128GcmDecrypt(
        byte* key,
        byte* nonce,
        byte* aad,
        int aadLength,
        byte* ciphertext,
        int ciphertextLength,
        byte* tag,
        byte* plaintext) =>
        AesGcmDecrypt(
            key,
            16,
            nonce,
            aad,
            aadLength,
            ciphertext,
            ciphertextLength,
            tag,
            plaintext);


    private static int AesGcmDecrypt(
        byte* key,
        int keyLength,
        byte* nonce,
        byte* aad,
        int aadLength,
        byte* ciphertext,
        int ciphertextLength,
        byte* tag,
        byte* plaintext)
    {
        uint objectLength =
            GetObjectLength(
                s_aesAlgorithm);

        byte* keyObject =
            stackalloc byte[(int) objectLength];

        nint keyHandle = 0;


        int status =
            BCryptGenerateSymmetricKey(
                s_aesAlgorithm,
                &keyHandle,
                keyObject,
                objectLength,
                key,
                (uint) keyLength,
                0);


        if (status != STATUS_SUCCESS)
        {
            throw new InvalidOperationException(
                $"BCryptGenerateSymmetricKey failed: 0x{status:X8}");
        }


        try
        {
            BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO auth =
                CreateAuth(
                    nonce,
                    aad,
                    aadLength,
                    tag);


            uint resultLength = 0;


            status =
                BCryptDecrypt(
                    keyHandle,
                    ciphertext,
                    (uint) ciphertextLength,
                    &auth,
                    null,
                    0,
                    plaintext,
                    (uint) ciphertextLength,
                    &resultLength,
                    0);


            if (status ==
                STATUS_AUTH_TAG_MISMATCH)
            {
                return -1;
            }


            if (status != STATUS_SUCCESS)
            {
                throw new InvalidOperationException(
                    $"BCryptDecrypt failed: 0x{status:X8}");
            }


            return (int) resultLength;
        }
        finally
        {
            BCryptDestroyKey(
                keyHandle);
        }
    }


    // ============================================================
    // AES-GCM authentication structure
    // ============================================================

    private static BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO CreateAuth(
        byte* nonce,
        byte* aad,
        int aadLength,
        byte* tag)
    {
        BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO auth =
            default;


        auth.cbSize =
            (uint) sizeof(
                BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO);

        auth.dwInfoVersion =
            BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO_VERSION;

        auth.pbNonce =
            nonce;

        auth.cbNonce =
            12;

        auth.pbAuthData =
            aad;

        auth.cbAuthData =
            (uint) aadLength;

        auth.pbTag =
            tag;

        auth.cbTag =
            16;


        return auth;
    }


    // ============================================================
    // Native integer helpers
    // ============================================================

    private static uint ReadUInt32(
        byte* value)
    {
        return
            (uint) value[0] |
            ((uint) value[1] << 8) |
            ((uint) value[2] << 16) |
            ((uint) value[3] << 24);
    }


    private static void WriteUInt32(
        byte* destination,
        uint value)
    {
        destination[0] =
            (byte) value;

        destination[1] =
            (byte) (value >> 8);

        destination[2] =
            (byte) (value >> 16);

        destination[3] =
            (byte) (value >> 24);
    }


    // ============================================================
    // BCrypt authenticated cipher structure
    // ============================================================

    [StructLayout(
        LayoutKind.Sequential)]
    private struct BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO
    {
        public uint cbSize;

        public uint dwInfoVersion;

        public byte* pbNonce;

        public uint cbNonce;

        public byte* pbAuthData;

        public uint cbAuthData;

        public byte* pbTag;

        public uint cbTag;

        public byte* pbMacContext;

        public uint cbMacContext;

        public uint cbAAD;

        public ulong cbData;

        public uint dwFlags;
    }
}