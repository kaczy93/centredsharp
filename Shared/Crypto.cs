using System.Security.Cryptography;
using System.Text;

namespace Shared; 

public class Crypto {
    public static string Md5Hash(String input)
    {
        return Hash(MD5.Create(), new MemoryStream(Encoding.ASCII.GetBytes(input)));
    }

    public static string Hash(HashAlgorithm alg, Stream stream)
    {
        return Convert.ToHexString(alg.ComputeHash(stream));
    }
}