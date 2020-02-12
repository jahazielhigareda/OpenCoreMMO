using System.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto.Engines;
public class RSA
{
    private static AsymmetricCipherKeyPair asymmetricCipherKeyPair;
    public static byte[] Decrypt(byte[] data)
    {
        var e = new RsaEngine();
        e.Init(false, asymmetricCipherKeyPair.Private);

        return e.ProcessBlock(data, 0, data.Length);
    }

    public static void LoadPem()
    {
        AsymmetricCipherKeyPair keyPair;

        using (var reader = File.OpenText(@"key.pem"))
        {
            keyPair = (AsymmetricCipherKeyPair)new PemReader(reader).ReadObject();

            asymmetricCipherKeyPair = keyPair;
        }
    }
}