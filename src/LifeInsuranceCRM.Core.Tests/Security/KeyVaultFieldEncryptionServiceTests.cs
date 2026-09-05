using System.Security.Cryptography;
using LifeInsuranceCRM.Core.Config;
using LifeInsuranceCRM.Providers.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.Security;

public class KeyVaultFieldEncryptionServiceTests
{
    [Fact]
    public void EncryptDecrypt_RoundTripsMedicareNumberAndDateOfBirth()
    {
        var service = CreateService(Environments.Development);

        var medicare = service.Encrypt("1EG4TE5MK72");
        var dateOfBirth = service.EncryptDateOnly(new DateOnly(1950, 6, 15));
        var partA = service.EncryptDateOnly(new DateOnly(2015, 3, 1));
        var partB = service.EncryptDateOnly(new DateOnly(2015, 3, 1));

        Assert.NotNull(medicare);
        Assert.NotEqual("1EG4TE5MK72"u8.ToArray(), medicare);
        Assert.Equal("1EG4TE5MK72", service.Decrypt(medicare));
        Assert.Equal(new DateOnly(1950, 6, 15), service.DecryptDateOnly(dateOfBirth));
        Assert.Equal(new DateOnly(2015, 3, 1), service.DecryptDateOnly(partA));
        Assert.Equal(new DateOnly(2015, 3, 1), service.DecryptDateOnly(partB));
    }

    [Fact]
    public void Encrypt_WhenValueMissing_ReturnsNull()
    {
        var service = CreateService(Environments.Development);

        Assert.Null(service.Encrypt("  "));
        Assert.Null(service.EncryptDateOnly(null));
        Assert.Null(service.Decrypt(null));
        Assert.Null(service.DecryptDateOnly(null));
    }

    [Fact]
    public void Constructor_InDevelopmentWithoutKey_UsesStableLocalDek()
    {
        var first = CreateService(Environments.Development);
        var second = CreateService(Environments.Development);

        var ciphertext = first.Encrypt("1EG4TE5MK72");

        Assert.Equal("1EG4TE5MK72", second.Decrypt(ciphertext));
    }

    [Fact]
    public void Constructor_OutsideDevelopmentWithoutKey_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateService(Environments.Production));

        Assert.Contains("FieldEncryption:Key", exception.Message);
    }

    [Fact]
    public void Decrypt_WhenKeyVersionDiffers_Throws()
    {
        var dek = Convert.ToBase64String(SHA256.HashData("test-field-encryption-dek"u8.ToArray()));
        var versionOne = CreateService(Environments.Development, keyVersion: 1, key: dek);
        var ciphertext = versionOne.Encrypt("1EG4TE5MK72");
        var versionTwo = CreateService(Environments.Development, keyVersion: 2, key: dek);

        var exception = Assert.Throws<InvalidOperationException>(() => versionTwo.Decrypt(ciphertext));

        Assert.Contains("key version", exception.Message);
    }

    [Fact]
    public void ComputeMedicareNumberBlindIndex_NormalizesBeforeHashing()
    {
        var service = CreateService(Environments.Development);

        var dashed = service.ComputeMedicareNumberBlindIndex("1EG4-TE5-MK72");
        var plain = service.ComputeMedicareNumberBlindIndex("1eg4te5mk72");

        Assert.NotNull(dashed);
        Assert.Equal(dashed, plain);
        Assert.Equal(32, dashed.Length);
    }

    [Fact]
    public void ComputeMedicareNumberBlindIndex_WhenValueMissing_ReturnsNull()
    {
        var service = CreateService(Environments.Development);

        Assert.Null(service.ComputeMedicareNumberBlindIndex(null));
        Assert.Null(service.ComputeMedicareNumberBlindIndex("  "));
    }

    [Fact]
    public void ComputeMedicareNumberBlindIndex_UsesDistinctKeyFromEncryptionDek()
    {
        var dek = Convert.ToBase64String(SHA256.HashData("shared-looking-material-for-dek-test"u8.ToArray()));
        var blindIndexKey = Convert.ToBase64String(SHA256.HashData("different-blind-index-key-material"u8.ToArray()));
        var service = CreateService(Environments.Development, key: dek, blindIndexKey: blindIndexKey);

        var ciphertext = service.Encrypt("1EG4TE5MK72");
        var blindIndex = service.ComputeMedicareNumberBlindIndex("1EG4TE5MK72");

        Assert.NotNull(ciphertext);
        Assert.NotNull(blindIndex);
        Assert.NotEqual(ciphertext, blindIndex);
    }

    [Fact]
    public void Constructor_OutsideDevelopmentWithoutBlindIndexKey_Throws()
    {
        var dek = Convert.ToBase64String(SHA256.HashData("production-dek"u8.ToArray()));
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreateService(Environments.Production, key: dek));

        Assert.Contains("BlindIndexKey", exception.Message);
    }

    private static KeyVaultFieldEncryptionService CreateService(
        string environmentName,
        int keyVersion = 1,
        string key = "",
        string blindIndexKey = "")
    {
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns(environmentName);

        return new KeyVaultFieldEncryptionService(
            Options.Create(new FieldEncryptionOptions
            {
                Key = key,
                BlindIndexKey = blindIndexKey,
                KeyVersion = keyVersion,
            }),
            Options.Create(new KeyVaultOptions()),
            environment.Object);
    }
}
