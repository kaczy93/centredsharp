using System.Security.Cryptography;
using System.Text;

namespace CentrED.Utils;

public static class PasswordCrypter
{
    private static readonly string key = Environment.MachineName;
    //DES is not really secure, but it should be enough to prevent people leaking their creds
    private static readonly DES des = DES.Create();
    
    static PasswordCrypter()
    {
        var key = Environment.MachineName;
        var newKey = new byte[8];
        Encoding.UTF8.GetBytes(key).AsSpan(0, Math.Min(key.Length, newKey.Length)).CopyTo(newKey);
        des.Key = newKey;
    }

    public static string Encrypt(string password)
    {
        return Convert.ToBase64String(des.EncryptEcb(Encoding.UTF8.GetBytes(password), PaddingMode.PKCS7));
    }

    public static string Decrypt(string password)
    {
        if (password.Length == 0)
        {
            return password;
        }
        return Encoding.UTF8.GetString(des.DecryptEcb(Convert.FromBase64String(password), PaddingMode.PKCS7));
    }
}