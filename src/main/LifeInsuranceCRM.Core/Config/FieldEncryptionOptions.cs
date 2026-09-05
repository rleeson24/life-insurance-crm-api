namespace LifeInsuranceCRM.Core.Config;

public sealed class FieldEncryptionOptions
{
    public const string SectionName = "FieldEncryption";

    /// <summary>
    /// Base64-encoded 32-byte AES-256 data encryption key (DEK).
    /// Local: user secrets. Azure: Key Vault secret <c>FieldEncryption--Key</c>.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Base64-encoded DEK wrapped with the Key Vault RSA key named by
    /// <see cref="KeyVaultOptions.FieldEncryptionKeyName"/>. Preferred in Azure over a raw DEK secret.
    /// </summary>
    public string WrappedDek { get; set; } = string.Empty;

    /// <summary>
    /// Version written into new ciphertexts. Decrypt supports this version of the resolved DEK.
    /// </summary>
    public int KeyVersion { get; set; } = 1;

    /// <summary>
    /// Base64-encoded 32-byte HMAC key for Medicare number blind indexes.
    /// Local: user secrets. Azure: Key Vault secret <c>FieldEncryption--BlindIndexKey</c>.
    /// Must be distinct from <see cref="Key"/>.
    /// </summary>
    public string BlindIndexKey { get; set; } = string.Empty;
}
