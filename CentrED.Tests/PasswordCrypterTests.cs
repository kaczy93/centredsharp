using CentrED.Utils;

namespace CentrED.Tests;

public class PasswordCrypterTests
{
    [Fact]
    public void Test1()
    {
        var testMessage = "Hello world";
        var encrypted = PasswordCrypter.Encrypt(testMessage);
        Assert.NotEqual(testMessage, encrypted);
        var decrypted  = PasswordCrypter.Decrypt(encrypted);
        Assert.Equal(testMessage, decrypted);
    }
}