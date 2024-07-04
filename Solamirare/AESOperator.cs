
namespace Solamirare
{
    internal class AESOperator
    {
        static void Main()
        {
            try
            {
                string original = "Here is some data to encrypt!";

                // Create a new instance of the Aes class.
                using (Aes myAes = Aes.Create())
                {
                    // Generate a random key and IV.
                    myAes.GenerateKey();
                    myAes.GenerateIV();

                    // Encrypt the string to an array of bytes.
                    byte[] encrypted = EncryptStringToBytes(original, myAes.Key, myAes.IV).Value;

                    // Decrypt the bytes to a string.
                    string roundtrip = DecryptStringFromBytes(encrypted, myAes.Key, myAes.IV).Value;

                    // Display the original data and the decrypted data.
                    Console.WriteLine("Original:   {0}", original);
                    Console.WriteLine("Round Trip: {0}", roundtrip);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
            }
        }

        internal static (bool Success, byte[] Value) EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
        {


            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }

                    var value = msEncrypt.ToArray();
                    
                    return (Success:true, Value:value);

                }
            }
        }

        internal static (bool Success, string Value) DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            var result = srDecrypt.ReadToEnd();

                            return (Success: true, Value: result);
                        }
                    }
                }
            }
        }
    }
}
