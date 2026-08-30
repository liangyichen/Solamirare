using System;
using System.Collections.Generic;
using System.Text;

namespace Solamirare.Tests;

public static unsafe class Crypt_Test
{

    public static bool AES256GCM()
    {

        ReadOnlySpan<char> value = "Hello World";

        ReadOnlySpan<char> key = "0123456789ABCDEF";

        UnManagedString encrypted = NativeCrypto.Aes256Encrypt(value, key);

        UnManagedString decrypted = NativeCrypto.Aes256Decrypt(encrypted, key);

        bool result = decrypted.Equals(value);

        decrypted.Dispose();
        encrypted.Dispose();



        return result;
    }


    public static bool AES128GCM()
    {

        ReadOnlySpan<char> value = "Hello World";

        ReadOnlySpan<char> key = "0123456789ABCDEF";

        UnManagedString encrypted = NativeCrypto.Aes128Encrypt(value, key);

        UnManagedString decrypted = NativeCrypto.Aes128Decrypt(encrypted, key);

        bool result = decrypted.Equals(value);

        decrypted.Dispose();
        encrypted.Dispose();



        return result;
    }

    public static bool HMACSha256()
    {
        ReadOnlySpan<char> keyText = "secret-key";

        ReadOnlySpan<char> message = "Hello World";

        UnManagedString result = NativeCrypto.HmacSha256(keyText, message);

        bool equals = result.SequenceEqualIgnoreCase("34319C99921C41F8BE510CD5ED0ECA0F55B7ED7EA6198C5511794760C059709C"); //正确输出值

        result.Dispose();

        return equals;
    }

    public static bool Sha256()
    {
        ReadOnlySpan<char> text = "Hello World";

        UnManagedString result = NativeCrypto.Sha256(text);

        bool equals = result.SequenceEqualIgnoreCase("a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e"); //正确输出值

        result.Dispose();

        return equals;
    }


    public static bool Sha384()
    {
        ReadOnlySpan<char> text = "Hello World";

        byte* input = stackalloc byte[text.Length];

        for (int i = 0; i < text.Length; i++)
            input[i] = (byte)text[i];

        UnManagedString hash = NativeCrypto.Sha384(input, text.Length);

        bool result = hash.Equals("99514329186B2F6AE4A1329E7EE6C610A729636335174AC6B740F9028396FCC803D0E93863A7C3D90F86BEEE782F4F3F");

        hash.Dispose();

        return result;
    }

    public static bool HmacSha384()
    {
        ReadOnlySpan<char> keyText = "secret-key";
        ReadOnlySpan<char> messageText = "Hello World";

        byte* key = stackalloc byte[keyText.Length];
        byte* message = stackalloc byte[messageText.Length];

        for (int i = 0; i < keyText.Length; i++)
            key[i] = (byte)keyText[i];

        for (int i = 0; i < messageText.Length; i++)
            message[i] = (byte)messageText[i];

        UnManagedString hmac = NativeCrypto.HmacSha384(key, keyText.Length, message, messageText.Length);

        bool result = hmac.Equals("A2D766A6B672A4805D48910871B248106EBB6A316D524B4F1B8C5F0ADE2A325638335E6B24AF4E5F8B35E84723EF1FA2");

        hmac.Dispose();

        return result;
    }


    public static bool EcdheP256()
    {
        bool random =
            EcdheP256Test.Run();

        bool kat =
            EcdheP256KatTest.Run();

        bool ecdhe =
            random && kat;

        return ecdhe;
    }


}

