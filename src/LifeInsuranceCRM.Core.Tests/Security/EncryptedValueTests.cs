using System.Security.Cryptography;
using System.Text;
using LifeInsuranceCRM.Core.Security;

namespace LifeInsuranceCRM.Core.Tests.Security;

public class EncryptedValueTests
{
    private static readonly byte[] Key = SHA256.HashData("test-field-encryption-key"u8.ToArray());

    [Fact]
    public void EncryptDecrypt_RoundTripsPlaintext()
    {
        var payload = EncryptedValue.Encrypt(Key, 1, "1EG4TE5MK72"u8.ToArray());

        var plaintext = EncryptedValue.Decrypt(Key, payload);

        Assert.Equal("1EG4TE5MK72", Encoding.UTF8.GetString(plaintext));
    }

    [Fact]
    public void Encrypt_WritesFormatAndKeyVersion()
    {
        var payload = EncryptedValue.Encrypt(Key, 7, "x"u8.ToArray());

        Assert.Equal(EncryptedValue.CurrentFormatVersion, payload[0]);
        Assert.Equal((ushort)7, EncryptedValue.ReadKeyVersion(payload));
    }

    [Fact]
    public void Encrypt_UsesUniqueNoncePerCall()
    {
        var first = EncryptedValue.Encrypt(Key, 1, "same"u8.ToArray());
        var second = EncryptedValue.Encrypt(Key, 1, "same"u8.ToArray());

        Assert.NotEqual(first, second);
        Assert.Equal("same", Encoding.UTF8.GetString(EncryptedValue.Decrypt(Key, first)));
        Assert.Equal("same", Encoding.UTF8.GetString(EncryptedValue.Decrypt(Key, second)));
    }

    [Fact]
    public void Decrypt_WithWrongKey_Throws()
    {
        var payload = EncryptedValue.Encrypt(Key, 1, "secret"u8.ToArray());
        var otherKey = SHA256.HashData("other-field-encryption-key"u8.ToArray());

        Assert.ThrowsAny<CryptographicException>(() => EncryptedValue.Decrypt(otherKey, payload));
    }

    [Fact]
    public void Decrypt_WhenPayloadTruncated_Throws()
    {
        Assert.Throws<CryptographicException>(() => EncryptedValue.Decrypt(Key, [1, 0]));
    }
}
