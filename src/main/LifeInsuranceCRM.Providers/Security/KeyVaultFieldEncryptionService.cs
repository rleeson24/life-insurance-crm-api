using System.Security.Cryptography;
using System.Text;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using LifeInsuranceCRM.Core.Abstractions.Security;
using LifeInsuranceCRM.Core.Config;
using LifeInsuranceCRM.Core.Security;
using Hmac = System.Security.Cryptography.HMACSHA256;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LifeInsuranceCRM.Providers.Security;

public sealed class KeyVaultFieldEncryptionService : IFieldEncryptionService
{
    internal const string DevelopmentKeyMaterial = "LifeInsuranceCRM.Development.FieldEncryption.DoNotUseInProduction";
    internal const string DevelopmentBlindIndexKeyMaterial =
        "LifeInsuranceCRM.Development.FieldEncryption.BlindIndex.DoNotUseInProduction";

    private readonly FieldEncryptionOptions _encryptionOptions;
    private readonly KeyVaultOptions _keyVaultOptions;
    private readonly IHostEnvironment _environment;
    private readonly object _dekGate = new();
    private readonly object _blindIndexKeyGate = new();
    private byte[]? _dek;
    private byte[]? _blindIndexKey;
    private readonly ushort _keyVersion;

    public KeyVaultFieldEncryptionService(
        IOptions<FieldEncryptionOptions> encryptionOptions,
        IOptions<KeyVaultOptions> keyVaultOptions,
        IHostEnvironment environment)
    {
        _encryptionOptions = encryptionOptions.Value;
        _keyVaultOptions = keyVaultOptions.Value;
        _environment = environment;
        _keyVersion = ToKeyVersion(_encryptionOptions.KeyVersion);
        GetDek();
        GetBlindIndexKey();
    }

    public byte[]? Encrypt(string? plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            return null;
        }

        return EncryptedValue.Encrypt(GetDek(), _keyVersion, Encoding.UTF8.GetBytes(plaintext));
    }

    public string? Decrypt(byte[]? ciphertext)
    {
        var plaintext = DecryptBytes(ciphertext);
        return plaintext is null ? null : Encoding.UTF8.GetString(plaintext);
    }

    public byte[]? EncryptDateOnly(DateOnly? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var text = value.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        return EncryptedValue.Encrypt(GetDek(), _keyVersion, Encoding.UTF8.GetBytes(text));
    }

    public DateOnly? DecryptDateOnly(byte[]? ciphertext)
    {
        var plaintext = DecryptBytes(ciphertext);
        if (plaintext is null)
        {
            return null;
        }

        var text = Encoding.UTF8.GetString(plaintext);
        if (!DateOnly.TryParseExact(text, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var date))
        {
            throw new InvalidOperationException("Decrypted date of birth is not a valid calendar date.");
        }

        return date;
    }

    public byte[]? ComputeMedicareNumberBlindIndex(string? medicareNumber)
    {
        var normalized = MedicareNumberNormalizer.Normalize(medicareNumber);
        if (normalized is null)
        {
            return null;
        }

        return Hmac.HashData(GetBlindIndexKey(), Encoding.UTF8.GetBytes(normalized));
    }

    internal static byte[] CreateDevelopmentDek() => SHA256.HashData(Encoding.UTF8.GetBytes(DevelopmentKeyMaterial));

    internal static byte[] CreateDevelopmentBlindIndexKey() =>
        SHA256.HashData(Encoding.UTF8.GetBytes(DevelopmentBlindIndexKeyMaterial));

    private byte[]? DecryptBytes(byte[]? ciphertext)
    {
        if (ciphertext is null || ciphertext.Length == 0)
        {
            return null;
        }

        try
        {
            var version = EncryptedValue.ReadKeyVersion(ciphertext);
            var key = ResolveKey(version);
            return EncryptedValue.Decrypt(key, ciphertext);
        }
        catch (CryptographicException exception)
        {
            throw new InvalidOperationException("Failed to decrypt a protected client field.", exception);
        }
    }

    private byte[] GetDek()
    {
        if (_dek is not null)
        {
            return _dek;
        }

        lock (_dekGate)
        {
            _dek ??= ResolveDek();
            return _dek;
        }
    }

    private byte[] GetBlindIndexKey()
    {
        if (_blindIndexKey is not null)
        {
            return _blindIndexKey;
        }

        lock (_blindIndexKeyGate)
        {
            _blindIndexKey ??= ResolveBlindIndexKey();
            return _blindIndexKey;
        }
    }

    private byte[] ResolveKey(ushort keyVersion)
    {
        if (keyVersion != _keyVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported field encryption key version {keyVersion}. Current version is {_keyVersion}.");
        }

        return GetDek();
    }

    private byte[] ResolveDek()
    {
        if (TryDecodeKey(_encryptionOptions.Key, out var dek))
        {
            return dek;
        }

        if (!string.IsNullOrWhiteSpace(_encryptionOptions.WrappedDek))
        {
            return UnwrapDek(_encryptionOptions.WrappedDek);
        }

        if (_environment.IsDevelopment())
        {
            return CreateDevelopmentDek();
        }

        throw new InvalidOperationException(
            "Field encryption requires FieldEncryption:Key or FieldEncryption:WrappedDek outside Development. " +
            "Store FieldEncryption--Key (raw DEK) or FieldEncryption--WrappedDek (RSA-wrapped DEK) in Key Vault.");
    }

    private byte[] ResolveBlindIndexKey()
    {
        if (TryDecodeKey(_encryptionOptions.BlindIndexKey, out var blindIndexKey))
        {
            return blindIndexKey;
        }

        if (_environment.IsDevelopment())
        {
            return CreateDevelopmentBlindIndexKey();
        }

        throw new InvalidOperationException(
            "Medicare blind index requires FieldEncryption:BlindIndexKey outside Development. " +
            "Store FieldEncryption--BlindIndexKey (32-byte HMAC key, base64) in Key Vault.");
    }

    private byte[] UnwrapDek(string wrappedDekBase64)
    {
        if (string.IsNullOrWhiteSpace(_keyVaultOptions.VaultUri)
            || string.IsNullOrWhiteSpace(_keyVaultOptions.FieldEncryptionKeyName))
        {
            throw new InvalidOperationException(
                "FieldEncryption:WrappedDek requires KeyVault:VaultUri and KeyVault:FieldEncryptionKeyName.");
        }

        byte[] wrappedBytes;
        try
        {
            wrappedBytes = Convert.FromBase64String(wrappedDekBase64.Trim());
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("FieldEncryption:WrappedDek is not valid base64.", exception);
        }

        try
        {
            var credential = CreateCredential();
            var keyClient = new KeyClient(new Uri(_keyVaultOptions.VaultUri), credential);
            var cryptoClient = keyClient.GetCryptographyClient(_keyVaultOptions.FieldEncryptionKeyName);
            var result = cryptoClient.UnwrapKey(KeyWrapAlgorithm.RsaOaep256, wrappedBytes);
            ValidateDek(result.Key, "Unwrapped Key Vault DEK");
            return result.Key;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "Failed to unwrap the field-encryption DEK with the Key Vault RSA key.",
                exception);
        }
    }

    private TokenCredential CreateCredential()
    {
        var decision = KeyVaultConfiguration.Evaluate(
            _keyVaultOptions.VaultUri,
            _environment.EnvironmentName,
            _keyVaultOptions.AllowLocalAccess,
            KeyVaultConfiguration.IsManagedIdentityAvailable());

        decision.EnsureSuccess();

        return decision.AllowDeveloperCredentials
            ? new DefaultAzureCredential()
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeAzureCliCredential = true,
                ExcludeAzureDeveloperCliCredential = true,
                ExcludeAzurePowerShellCredential = true,
                ExcludeInteractiveBrowserCredential = true,
                ExcludeVisualStudioCredential = true,
                ExcludeVisualStudioCodeCredential = true,
            });
    }

    private static bool TryDecodeKey(string? value, out byte[] dek)
    {
        dek = [];
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            dek = Convert.FromBase64String(value.Trim());
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("FieldEncryption:Key is not valid base64.", exception);
        }

        ValidateDek(dek, "FieldEncryption:Key");
        return true;
    }

    private static void ValidateDek(byte[] dek, string name)
    {
        if (dek.Length != 32)
        {
            throw new InvalidOperationException($"{name} must be a 32-byte AES-256 key.");
        }
    }

    private static ushort ToKeyVersion(int keyVersion)
    {
        if (keyVersion is < 1 or > ushort.MaxValue)
        {
            throw new InvalidOperationException("FieldEncryption:KeyVersion must be between 1 and 65535.");
        }

        return (ushort)keyVersion;
    }
}
