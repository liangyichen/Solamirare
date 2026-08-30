using System;
using System.Security.Cryptography;

namespace Solamirare;

public static class EcdheP256KatTest
{
    private const string AlicePrivateKey =
        "C88F01F510D9AC3F70A292DAA2316DE544E9AAB8AFE84049C62A9C57862D1433";

    private const string AlicePublicKey =
        "04" +
        "DAD0B65394221CF9B051E1FECA5787D098DFE637FC90B9EF945D0C3772581180" +
        "5271A0461CDB8252D61F1C456FA3E59AB1F45B33ACCF5F58389E0577B8990BB3";

    private const string BobPrivateKey =
        "C6EF9C5D78AE012A011164ACB397CE2088685D8F06BF9BE0B283AB46476BEE53";

    private const string BobPublicKey =
        "04" +
        "D12DFB5289C8D4F81208B70270398C342296970A0BCCB74C736FC7554494BF63" +
        "56FBF3CA366CC23E8157854C13C58D6AAC23F046ADA30F8353E74F33039872AB";

    private const string ExpectedSharedSecret =
        "D6840F6B42F6EDAFD13116E0E12565202FEF8E9ECE7DCE03812464D04B9442DE";


    public static bool Run()
    {
        try
        {
            byte[] alicePrivate =
                Convert.FromHexString(
                    AlicePrivateKey);

            byte[] alicePublic =
                Convert.FromHexString(
                    AlicePublicKey);

            byte[] bobPrivate =
                Convert.FromHexString(
                    BobPrivateKey);

            byte[] bobPublic =
                Convert.FromHexString(
                    BobPublicKey);

            byte[] expected =
                Convert.FromHexString(
                    ExpectedSharedSecret);


            using ECDiffieHellman alice =
                ECDiffieHellman.Create();

            using ECDiffieHellman bob =
                ECDiffieHellman.Create();


            ImportP256PrivateKey(
                alice,
                alicePrivate,
                alicePublic);

            ImportP256PrivateKey(
                bob,
                bobPrivate,
                bobPublic);


            byte[] aliceSecret =
                alice.DeriveRawSecretAgreement(
                    bob.PublicKey);

            byte[] bobSecret =
                bob.DeriveRawSecretAgreement(
                    alice.PublicKey);


            bool aliceCorrect =
                CryptographicOperations.FixedTimeEquals(
                    aliceSecret,
                    expected);

            bool bobCorrect =
                CryptographicOperations.FixedTimeEquals(
                    bobSecret,
                    expected);

            bool symmetric =
                CryptographicOperations.FixedTimeEquals(
                    aliceSecret,
                    bobSecret);

            bool result =
                aliceCorrect &&
                bobCorrect &&
                symmetric;


            return result;
        }
        catch (Exception ex)
        {

            return false;
        }
    }


    private static void ImportP256PrivateKey(
        ECDiffieHellman ecdh,
        byte[] privateKey,
        byte[] publicKey)
    {
        if (privateKey.Length != 32)
            throw new CryptographicException(
                "Invalid P-256 private key length.");

        if (publicKey.Length != 65 ||
            publicKey[0] != 0x04)
        {
            throw new CryptographicException(
                "Invalid P-256 public key.");
        }


        byte[] x =
            publicKey.AsSpan(
                1,
                32)
            .ToArray();

        byte[] y =
            publicKey.AsSpan(
                33,
                32)
            .ToArray();


        ECParameters parameters =
            new ECParameters
            {
                Curve =
                    ECCurve.NamedCurves.nistP256,

                D = privateKey,

                Q =
                    new ECPoint
                    {
                        X = x,
                        Y = y
                    }
            };


        ecdh.ImportParameters(
            parameters);
    }
}