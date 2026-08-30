using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Solamirare;

internal static unsafe class NativeCryptoLinux
{
    private const int Success = 1;

    private const int EVP_CTRL_GCM_GET_TAG = 0x10;

    private const int EVP_CTRL_GCM_SET_TAG = 0x11;

    private const int NID_X9_62_prime256v1 = 415;

    private const int POINT_CONVERSION_UNCOMPRESSED = 4;


    private static nint s_libCrypto;


    private static delegate* unmanaged<
        byte*,
        nuint,
        byte*,
        byte*>
        SHA256;

    private static delegate* unmanaged<
        byte*,
        nuint,
        byte*,
        byte*>
        SHA384;

    private static delegate* unmanaged<
        nint,
        byte*,
        int,
        byte*,
        nuint,
        byte*,
        uint*,
        byte*>
        HMAC;


    private static delegate* unmanaged<
        nint>
        EVP_aes_256_gcm;

    private static delegate* unmanaged<
        nint>
        EVP_aes_128_gcm;

    private static delegate* unmanaged<
        nint>
        EVP_sha256;

    private static delegate* unmanaged<
        nint>
        EVP_sha384;


    private static delegate* unmanaged<
        nint>
        EVP_CIPHER_CTX_new;

    private static delegate* unmanaged<
        nint,
        void>
        EVP_CIPHER_CTX_free;

    private static delegate* unmanaged<
        nint,
        nint,
        nint,
        byte*,
        byte*,
        int>
        EVP_EncryptInit_ex;

    private static delegate* unmanaged<
        nint,
        byte*,
        int*,
        byte*,
        int,
        int>
        EVP_EncryptUpdate;

    private static delegate* unmanaged<
        nint,
        byte*,
        int*,
        int>
        EVP_EncryptFinal_ex;

    private static delegate* unmanaged<
        nint,
        nint,
        nint,
        byte*,
        byte*,
        int>
        EVP_DecryptInit_ex;

    private static delegate* unmanaged<
        nint,
        byte*,
        int*,
        byte*,
        int,
        int>
        EVP_DecryptUpdate;

    private static delegate* unmanaged<
        nint,
        byte*,
        int*,
        int>
        EVP_DecryptFinal_ex;

    private static delegate* unmanaged<
        nint,
        int,
        int,
        void*,
        int>
        EVP_CIPHER_CTX_ctrl;

    private static delegate* unmanaged<
        byte*,
        int,
        int>
        RAND_bytes;


    // ============================================================
    // ECDHE-P256
    // ============================================================

    private static delegate* unmanaged<
        int,
        nint>
        EC_KEY_new_by_curve_name;

    private static delegate* unmanaged<
        nint,
        int>
        EC_KEY_generate_key;

    private static delegate* unmanaged<
        nint,
        nint>
        EC_KEY_get0_group;

    private static delegate* unmanaged<
        nint,
        nint>
        EC_KEY_get0_public_key;

    private static delegate* unmanaged<
        nint,
        void>
        EC_KEY_free;

    private static delegate* unmanaged<
        nint,
        nint,
        int>
        EC_KEY_set_public_key;


    // EC_POINT_new(const EC_GROUP *group)
    private static delegate* unmanaged<
        nint,
        nint>
        EC_POINT_new;

    private static delegate* unmanaged<
        nint,
        void>
        EC_POINT_free;


    // size_t EC_POINT_point2oct(
    //     const EC_GROUP *group,
    //     const EC_POINT *p,
    //     point_conversion_form_t form,
    //     unsigned char *buf,
    //     size_t len,
    //     BN_CTX *ctx
    // );
    private static delegate* unmanaged<
        nint,
        nint,
        int,
        byte*,
        nuint,
        nint,
        nuint>
        EC_POINT_point2oct;


    // int EC_POINT_oct2point(
    //     const EC_GROUP *group,
    //     EC_POINT *p,
    //     const unsigned char *buf,
    //     size_t len,
    //     BN_CTX *ctx
    // );
    private static delegate* unmanaged<
        nint,
        nint,
        byte*,
        nuint,
        nint,
        int>
        EC_POINT_oct2point;


    // int ECDH_compute_key(
    //     void *out,
    //     size_t outlen,
    //     const EC_POINT *pub_key,
    //     const EC_KEY *ecdh,
    //     void *(*KDF)(
    //         const void *in,
    //         size_t inlen,
    //         void *out,
    //         size_t *outlen)
    // );
    //
    // The KDF is null because TLS 1.3 requires the raw ECDH
    // shared secret and performs HKDF separately.
    private static delegate* unmanaged<
        byte*,
        nuint,
        nint,
        nint,
        nint,
        int>
        ECDH_compute_key;

    // ============================================================
    // ECDSA-P256
    // ============================================================

    private static delegate* unmanaged<
        byte*,
        int,
        nint,
        nint>
        ECDSA_do_sign;

    private static delegate* unmanaged<
        byte*,
        int,
        nint,
        nint,
        int>
        ECDSA_do_verify;

    private static delegate* unmanaged<
        nint,
        void>
        ECDSA_SIG_free;

    private static delegate* unmanaged<
        nint>
        ECDSA_SIG_new;

    private static delegate* unmanaged<
        nint,
        nint*,
        nint*,
        void>
        ECDSA_SIG_get0;

    private static delegate* unmanaged<
        nint,
        nint,
        nint,
        int>
        ECDSA_SIG_set0;

    private static delegate* unmanaged<
        nint,
        byte*,
        int>
        BN_bn2bin;

    private static delegate* unmanaged<
        byte*,
        int,
        nint,
        nint>
        BN_bin2bn;

    private static delegate* unmanaged<
        nint,
        void>
        BN_free;



    // ============================================================
    // Initialize
    // ============================================================

    internal static void Initialize()
    {
        if (s_libCrypto != 0)
            return;


        if (!NativeLibrary.TryLoad(
                "libcrypto.so.3",
                out s_libCrypto) &&
            !NativeLibrary.TryLoad(
                "libcrypto.so.1.1",
                out s_libCrypto) &&
            !NativeLibrary.TryLoad(
                "libcrypto.so",
                out s_libCrypto))
        {
            throw new PlatformNotSupportedException(
                "Unable to load OpenSSL libcrypto.");
        }


        SHA256 =
            (delegate* unmanaged<
                byte*,
                nuint,
                byte*,
                byte*>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "SHA256");

        SHA384 =
            (delegate* unmanaged<
                byte*,
                nuint,
                byte*,
                byte*>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "SHA384");

        HMAC =
            (delegate* unmanaged<
                nint,
                byte*,
                int,
                byte*,
                nuint,
                byte*,
                uint*,
                byte*>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "HMAC");


        EVP_aes_256_gcm =
            (delegate* unmanaged<
                nint>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EVP_aes_256_gcm");

        EVP_aes_128_gcm =
            (delegate* unmanaged<
                nint>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EVP_aes_128_gcm");

        EVP_sha256 =
            (delegate* unmanaged<
                nint>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EVP_sha256");

        EVP_sha384 =
            (delegate* unmanaged<
                nint>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EVP_sha384");


        EVP_CIPHER_CTX_new =
            (delegate* unmanaged<
                nint>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EVP_CIPHER_CTX_new");

        EVP_CIPHER_CTX_free =
            (delegate* unmanaged<
                nint,
                void>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EVP_CIPHER_CTX_free");

        EVP_EncryptInit_ex =
            (delegate* unmanaged<
                nint,
                nint,
                nint,
                byte*,
                byte*,
                int>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EVP_EncryptInit_ex");

        EVP_EncryptUpdate =
            (delegate* unmanaged<
                nint,
                byte*,
                int*,
                byte*,
                int,
                int>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EVP_EncryptUpdate");

        EVP_EncryptFinal_ex =
            (delegate* unmanaged<
                nint,
                byte*,
                int*,
                int>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EVP_EncryptFinal_ex");

        EVP_DecryptInit_ex =
            (delegate* unmanaged<
                nint,
                nint,
                nint,
                byte*,
                byte*,
                int>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EVP_DecryptInit_ex");

        EVP_DecryptUpdate =
            (delegate* unmanaged<
                nint,
                byte*,
                int*,
                byte*,
                int,
                int>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EVP_DecryptUpdate");

        EVP_DecryptFinal_ex =
            (delegate* unmanaged<
                nint,
                byte*,
                int*,
                int>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EVP_DecryptFinal_ex");

        EVP_CIPHER_CTX_ctrl =
            (delegate* unmanaged<
                nint,
                int,
                int,
                void*,
                int>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EVP_CIPHER_CTX_ctrl");

        RAND_bytes =
            (delegate* unmanaged<
                byte*,
                int,
                int>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "RAND_bytes");


        // ========================================================
        // ECDHE-P256
        // ========================================================

        EC_KEY_new_by_curve_name =
            (delegate* unmanaged<
                int,
                nint>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EC_KEY_new_by_curve_name");

        EC_KEY_generate_key =
            (delegate* unmanaged<
                nint,
                int>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EC_KEY_generate_key");

        EC_KEY_get0_group =
            (delegate* unmanaged<
                nint,
                nint>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EC_KEY_get0_group");

        EC_KEY_get0_public_key =
            (delegate* unmanaged<
                nint,
                nint>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EC_KEY_get0_public_key");

        EC_KEY_free =
            (delegate* unmanaged<
                nint,
                void>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EC_KEY_free");

        EC_KEY_set_public_key =
            (delegate* unmanaged<
                nint,
                nint,
                int>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EC_KEY_set_public_key");

        EC_POINT_new =
            (delegate* unmanaged<
                nint,
                nint>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EC_POINT_new");

        EC_POINT_free =
            (delegate* unmanaged<
                nint,
                void>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EC_POINT_free");

        EC_POINT_point2oct =
            (delegate* unmanaged<
                nint,
                nint,
                int,
                byte*,
                nuint,
                nint,
                nuint>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EC_POINT_point2oct");

        EC_POINT_oct2point =
            (delegate* unmanaged<
                nint,
                nint,
                byte*,
                nuint,
                nint,
                int>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "EC_POINT_oct2point");

        ECDH_compute_key =
            (delegate* unmanaged<
                byte*,
                nuint,
                nint,
                nint,
                nint,
                int>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "ECDH_compute_key");

        // ========================================================
        // ECDSA-P256
        // ========================================================

        ECDSA_do_sign =
            (delegate* unmanaged<
                byte*,
                int,
                nint,
                nint>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "ECDSA_do_sign");

        ECDSA_do_verify =
            (delegate* unmanaged<
                byte*,
                int,
                nint,
                nint,
                int>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "ECDSA_do_verify");

        ECDSA_SIG_free =
            (delegate* unmanaged<
                nint,
                void>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "ECDSA_SIG_free");

        ECDSA_SIG_new =
            (delegate* unmanaged<
                nint>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "ECDSA_SIG_new");

        ECDSA_SIG_get0 =
            (delegate* unmanaged<
                nint,
                nint*,
                nint*,
                void>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "ECDSA_SIG_get0");

        ECDSA_SIG_set0 =
            (delegate* unmanaged<
                nint,
                nint,
                nint,
                int>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "ECDSA_SIG_set0");

        BN_bn2bin =
            (delegate* unmanaged<
                nint,
                byte*,
                int>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "BN_bn2bin");

        BN_bin2bn =
            (delegate* unmanaged<
                byte*,
                int,
                nint,
                nint>)
            NativeLibrary.GetExport(
                s_libCrypto,
                "BN_bin2bn");


        // ========================================================
        // Existing NativeCrypto registrations
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
    // Random
    // ============================================================

    [UnmanagedCallersOnly]
    internal static void Random(
        byte* output,
        int length)
    {
        if (length <= 0)
            return;

        if (RAND_bytes(
                output,
                length) != Success)
        {
            throw new InvalidOperationException(
                "OpenSSL RAND_bytes failed.");
        }
    }


    // ============================================================
    // SHA-256
    // ============================================================

    [UnmanagedCallersOnly]
    internal static void Sha256(
        byte* input,
        int inputLength,
        byte* output)
    {
        if (SHA256(
                input,
                (nuint) inputLength,
                output) == null)
        {
            throw new InvalidOperationException(
                "OpenSSL SHA256 failed.");
        }
    }


    // ============================================================
    // SHA-384
    // ============================================================

    [UnmanagedCallersOnly]
    internal static void Sha384(
        byte* input,
        int inputLength,
        byte* output)
    {
        if (SHA384(
                input,
                (nuint) inputLength,
                output) == null)
        {
            throw new InvalidOperationException(
                "OpenSSL SHA384 failed.");
        }
    }


    // ============================================================
    // HMAC-SHA256
    // ============================================================

    [UnmanagedCallersOnly]
    internal static void HmacSha256(
        byte* key,
        int keyLength,
        byte* input,
        int inputLength,
        byte* output)
    {
        nint md =
            EVP_sha256();

        uint length = 0;

        if (HMAC(
                md,
                key,
                keyLength,
                input,
                (nuint) inputLength,
                output,
                &length) == null ||
            length != 32)
        {
            throw new InvalidOperationException(
                "OpenSSL HMAC-SHA256 failed.");
        }
    }


    // ============================================================
    // HMAC-SHA384
    // ============================================================

    [UnmanagedCallersOnly]
    internal static void HmacSha384(
        byte* key,
        int keyLength,
        byte* input,
        int inputLength,
        byte* output)
    {
        nint md =
            EVP_sha384();

        uint length = 0;

        if (HMAC(
                md,
                key,
                keyLength,
                input,
                (nuint) inputLength,
                output,
                &length) == null ||
            length != 48)
        {
            throw new InvalidOperationException(
                "OpenSSL HMAC-SHA384 failed.");
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
            throw new ArgumentNullException(
                nameof(publicKey));

        if (privateKey == null)
            throw new ArgumentNullException(
                nameof(privateKey));

        *privateKey = 0;


        nint key =
            EC_KEY_new_by_curve_name(
                NID_X9_62_prime256v1);

        if (key == 0)
        {
            throw new CryptographicException(
                "EC_KEY_new_by_curve_name failed.");
        }


        try
        {
            if (EC_KEY_generate_key(
                    key) != Success)
            {
                throw new CryptographicException(
                    "EC_KEY_generate_key failed.");
            }


            nint group =
                EC_KEY_get0_group(
                    key);

            if (group == 0)
            {
                throw new CryptographicException(
                    "EC_KEY_get0_group failed.");
            }


            nint point =
                EC_KEY_get0_public_key(
                    key);

            if (point == 0)
            {
                throw new CryptographicException(
                    "EC_KEY_get0_public_key failed.");
            }


            nuint length =
                EC_POINT_point2oct(
                    group,
                    point,
                    POINT_CONVERSION_UNCOMPRESSED,
                    publicKey,
                    65,
                    0);

            if (length != 65)
            {
                throw new CryptographicException(
                    "OpenSSL returned an invalid P-256 public key.");
            }


            *privateKey =
                key;

            key = 0;
        }
        finally
        {
            if (key != 0)
            {
                EC_KEY_free(
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
            throw new ArgumentNullException(
                nameof(privateKey));

        if (peerPublicKey == null)
            throw new ArgumentNullException(
                nameof(peerPublicKey));

        if (sharedSecret == null)
            throw new ArgumentNullException(
                nameof(sharedSecret));


        if (peerPublicKey[0] != 0x04)
        {
            throw new CryptographicException(
                "Invalid P-256 public key format.");
        }


        nint group =
            EC_KEY_get0_group(
                privateKey);

        if (group == 0)
        {
            throw new CryptographicException(
                "EC_KEY_get0_group failed.");
        }


        nint point =
            EC_POINT_new(
                group);

        if (point == 0)
        {
            throw new CryptographicException(
                "EC_POINT_new failed.");
        }


        try
        {
            if (EC_POINT_oct2point(
                    group,
                    point,
                    peerPublicKey,
                    65,
                    0) != Success)
            {
                throw new CryptographicException(
                    "EC_POINT_oct2point failed.");
            }


            int length =
                ECDH_compute_key(
                    sharedSecret,
                    32,
                    point,
                    privateKey,
                    0);

            if (length != 32)
            {
                throw new CryptographicException(
                    $"ECDH_compute_key returned {length} bytes instead of 32.");
            }


            return length;
        }
        finally
        {
            EC_POINT_free(
                point);
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

        EC_KEY_free(
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
            throw new ArgumentNullException(
                nameof(publicKey));

        if (privateKey == null)
            throw new ArgumentNullException(
                nameof(privateKey));

        *privateKey = 0;

        nint key =
            EC_KEY_new_by_curve_name(
                NID_X9_62_prime256v1);

        if (key == 0)
        {
            throw new CryptographicException(
                "EC_KEY_new_by_curve_name failed.");
        }

        try
        {
            if (EC_KEY_generate_key(
                    key) != Success)
            {
                throw new CryptographicException(
                    "EC_KEY_generate_key failed.");
            }

            nint group =
                EC_KEY_get0_group(
                    key);

            if (group == 0)
            {
                throw new CryptographicException(
                    "EC_KEY_get0_group failed.");
            }

            nint point =
                EC_KEY_get0_public_key(
                    key);

            if (point == 0)
            {
                throw new CryptographicException(
                    "EC_KEY_get0_public_key failed.");
            }

            nuint length =
                EC_POINT_point2oct(
                    group,
                    point,
                    POINT_CONVERSION_UNCOMPRESSED,
                    publicKey,
                    65,
                    0);

            if (length != 65)
            {
                throw new CryptographicException(
                    "OpenSSL returned an invalid P-256 public key.");
            }

            *privateKey =
                key;

            key = 0;
        }
        finally
        {
            if (key != 0)
            {
                EC_KEY_free(
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
            throw new ArgumentNullException(
                nameof(privateKey));

        if (hash == null)
            throw new ArgumentNullException(
                nameof(hash));

        if (signature == null)
            throw new ArgumentNullException(
                nameof(signature));

        nint ecdsaSignature =
            ECDSA_do_sign(
                hash,
                32,
                privateKey);

        if (ecdsaSignature == 0)
        {
            throw new CryptographicException(
                "ECDSA_do_sign failed.");
        }

        try
        {
            nint r = 0;
            nint s = 0;

            ECDSA_SIG_get0(
                ecdsaSignature,
                &r,
                &s);

            if (r == 0 || s == 0)
            {
                throw new CryptographicException(
                    "ECDSA signature does not contain R/S.");
            }

            byte* rBuffer =
                stackalloc byte[32];

            byte* sBuffer =
                stackalloc byte[32];

            for (int i = 0; i < 32; i++)
            {
                rBuffer[i] = 0;
                sBuffer[i] = 0;
            }

            int rLength =
                BN_bn2bin(
                    r,
                    rBuffer);

            int sLength =
                BN_bn2bin(
                    s,
                    sBuffer);

            if (rLength <= 0 ||
                rLength > 32 ||
                sLength <= 0 ||
                sLength > 32)
            {
                throw new CryptographicException(
                    "Invalid ECDSA signature values.");
            }

            for (int i = 0; i < 64; i++)
                signature[i] = 0;

            for (int i = 0; i < rLength; i++)
            {
                signature[
                    32 - rLength + i] =
                    rBuffer[i];
            }

            for (int i = 0; i < sLength; i++)
            {
                signature[
                    64 - sLength + i] =
                    sBuffer[i];
            }

            return 64;
        }
        finally
        {
            ECDSA_SIG_free(
                ecdsaSignature);
        }
    }

    // ============================================================
    // ECDSA-P256 Verify
    // ============================================================

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
            throw new ArgumentNullException(
                nameof(publicKey));

        if (hash == null)
            throw new ArgumentNullException(
                nameof(hash));

        if (signature == null)
            throw new ArgumentNullException(
                nameof(signature));

        if (publicKey[0] != 0x04)
            return 0;

        nint key =
            EC_KEY_new_by_curve_name(
                NID_X9_62_prime256v1);

        if (key == 0)
            return 0;

        nint point = 0;
        nint ecdsaSignature = 0;
        nint r = 0;
        nint s = 0;

        try
        {
            nint group =
                EC_KEY_get0_group(
                    key);

            if (group == 0)
                return 0;

            point =
                EC_POINT_new(
                    group);

            if (point == 0)
                return 0;

            if (EC_POINT_oct2point(
                    group,
                    point,
                    publicKey,
                    65,
                    0) != Success)
            {
                return 0;
            }

            if (EC_KEY_set_public_key(
                    key,
                    point) != Success)
            {
                return 0;
            }

            r =
                BN_bin2bn(
                    signature,
                    32,
                    0);

            if (r == 0)
                return 0;

            s =
                BN_bin2bn(
                    signature + 32,
                    32,
                    0);

            if (s == 0)
                return 0;

            ecdsaSignature =
                ECDSA_SIG_new();

            if (ecdsaSignature == 0)
                return 0;

            if (ECDSA_SIG_set0(
                    ecdsaSignature,
                    r,
                    s) != Success)
            {
                return 0;
            }

            r = 0;
            s = 0;

            return
                ECDSA_do_verify(
                    hash,
                    32,
                    ecdsaSignature,
                    key) == Success
                    ? 1
                    : 0;
        }
        finally
        {
            if (r != 0)
                BN_free(r);

            if (s != 0)
                BN_free(s);

            if (ecdsaSignature != 0)
                ECDSA_SIG_free(
                    ecdsaSignature);

            if (point != 0)
                EC_POINT_free(
                    point);

            EC_KEY_free(
                key);
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

        EC_KEY_free(
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
            EVP_aes_256_gcm(),
            key,
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
            EVP_aes_128_gcm(),
            key,
            nonce,
            aad,
            aadLength,
            plaintext,
            plaintextLength,
            ciphertext,
            tag);


    private static int AesGcmEncrypt(
        nint cipher,
        byte* key,
        byte* nonce,
        byte* aad,
        int aadLength,
        byte* plaintext,
        int plaintextLength,
        byte* ciphertext,
        byte* tag)
    {
        nint ctx =
            EVP_CIPHER_CTX_new();

        if (ctx == 0)
        {
            throw new InvalidOperationException(
                "EVP_CIPHER_CTX_new failed.");
        }


        try
        {
            int result =
                EVP_EncryptInit_ex(
                    ctx,
                    cipher,
                    0,
                    null,
                    null);

            if (result != Success)
            {
                throw new InvalidOperationException(
                    "EVP_EncryptInit_ex failed.");
            }


            result =
                EVP_EncryptInit_ex(
                    ctx,
                    0,
                    0,
                    key,
                    nonce);

            if (result != Success)
            {
                throw new InvalidOperationException(
                    "EVP_EncryptInit_ex(key/nonce) failed.");
            }


            int written = 0;

            if (aadLength > 0)
            {
                if (EVP_EncryptUpdate(
                        ctx,
                        null,
                        &written,
                        aad,
                        aadLength) != Success)
                {
                    throw new InvalidOperationException(
                        "EVP_EncryptUpdate(AAD) failed.");
                }
            }


            int ciphertextLength = 0;

            if (plaintextLength > 0)
            {
                if (EVP_EncryptUpdate(
                        ctx,
                        ciphertext,
                        &ciphertextLength,
                        plaintext,
                        plaintextLength) != Success)
                {
                    throw new InvalidOperationException(
                        "EVP_EncryptUpdate(data) failed.");
                }
            }


            int finalLength = 0;

            if (EVP_EncryptFinal_ex(
                    ctx,
                    ciphertext + ciphertextLength,
                    &finalLength) != Success)
            {
                throw new InvalidOperationException(
                    "EVP_EncryptFinal_ex failed.");
            }


            ciphertextLength +=
                finalLength;


            if (EVP_CIPHER_CTX_ctrl(
                    ctx,
                    EVP_CTRL_GCM_GET_TAG,
                    16,
                    tag) != Success)
            {
                throw new InvalidOperationException(
                    "EVP_CTRL_GCM_GET_TAG failed.");
            }


            return ciphertextLength;
        }
        finally
        {
            EVP_CIPHER_CTX_free(
                ctx);
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
            EVP_aes_256_gcm(),
            key,
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
            EVP_aes_128_gcm(),
            key,
            nonce,
            aad,
            aadLength,
            ciphertext,
            ciphertextLength,
            tag,
            plaintext);


    private static int AesGcmDecrypt(
        nint cipher,
        byte* key,
        byte* nonce,
        byte* aad,
        int aadLength,
        byte* ciphertext,
        int ciphertextLength,
        byte* tag,
        byte* plaintext)
    {
        nint ctx =
            EVP_CIPHER_CTX_new();

        if (ctx == 0)
        {
            throw new InvalidOperationException(
                "EVP_CIPHER_CTX_new failed.");
        }


        try
        {
            int result =
                EVP_DecryptInit_ex(
                    ctx,
                    cipher,
                    0,
                    null,
                    null);

            if (result != Success)
            {
                throw new InvalidOperationException(
                    "EVP_DecryptInit_ex failed.");
            }


            result =
                EVP_DecryptInit_ex(
                    ctx,
                    0,
                    0,
                    key,
                    nonce);

            if (result != Success)
            {
                throw new InvalidOperationException(
                    "EVP_DecryptInit_ex(key/nonce) failed.");
            }


            int written = 0;

            if (aadLength > 0)
            {
                if (EVP_DecryptUpdate(
                        ctx,
                        null,
                        &written,
                        aad,
                        aadLength) != Success)
                {
                    throw new InvalidOperationException(
                        "EVP_DecryptUpdate(AAD) failed.");
                }
            }


            int plaintextLength = 0;

            if (ciphertextLength > 0)
            {
                if (EVP_DecryptUpdate(
                        ctx,
                        plaintext,
                        &plaintextLength,
                        ciphertext,
                        ciphertextLength) != Success)
                {
                    throw new InvalidOperationException(
                        "EVP_DecryptUpdate(data) failed.");
                }
            }


            if (EVP_CIPHER_CTX_ctrl(
                    ctx,
                    EVP_CTRL_GCM_SET_TAG,
                    16,
                    tag) != Success)
            {
                throw new InvalidOperationException(
                    "EVP_CTRL_GCM_SET_TAG failed.");
            }


            int finalLength = 0;

            result =
                EVP_DecryptFinal_ex(
                    ctx,
                    plaintext + plaintextLength,
                    &finalLength);


            if (result != Success)
                return -1;


            return
                plaintextLength +
                finalLength;
        }
        finally
        {
            EVP_CIPHER_CTX_free(
                ctx);
        }
    }
}