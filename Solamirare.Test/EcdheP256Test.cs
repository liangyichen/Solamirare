using System;

namespace Solamirare;

public static unsafe class EcdheP256Test
{
    public static bool Run()
    {
        const int count = 1000;

        for (int i = 0; i < count; i++)
        {
            if (!RunOnce())
            {
                return false;
            }
        }

        return true;
    }

    public static unsafe bool RunPropertyTest(
        int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!RunOnce())
                return false;
        }

        return true;
    }
    private static bool RunOnce()
    {
        byte* alicePublicKey =
            stackalloc byte[65];

        byte* bobPublicKey =
            stackalloc byte[65];

        byte* aliceSharedSecret =
            stackalloc byte[32];

        byte* bobSharedSecret =
            stackalloc byte[32];

        nint alicePrivateKey = 0;
        nint bobPrivateKey = 0;

        try
        {
            alicePrivateKey =
                NativeCrypto.EcdheP256Generate(
                    alicePublicKey);

            if (alicePrivateKey == 0)
                return false;

            bobPrivateKey =
                NativeCrypto.EcdheP256Generate(
                    bobPublicKey);

            if (bobPrivateKey == 0)
                return false;


            if (alicePublicKey[0] != 0x04)
                return false;

            if (bobPublicKey[0] != 0x04)
                return false;


            int aliceLength =
                NativeCrypto.EcdheP256Derive(
                    alicePrivateKey,
                    bobPublicKey,
                    aliceSharedSecret);

            if (aliceLength != 32)
                return false;


            int bobLength =
                NativeCrypto.EcdheP256Derive(
                    bobPrivateKey,
                    alicePublicKey,
                    bobSharedSecret);

            if (bobLength != 32)
                return false;


            for (int i = 0; i < 32; i++)
            {
                if (aliceSharedSecret[i] !=
                    bobSharedSecret[i])
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (alicePrivateKey != 0)
            {
                NativeCrypto.EcdheP256Destroy(
                    alicePrivateKey);
            }

            if (bobPrivateKey != 0)
            {
                NativeCrypto.EcdheP256Destroy(
                    bobPrivateKey);
            }
        }
    }
}