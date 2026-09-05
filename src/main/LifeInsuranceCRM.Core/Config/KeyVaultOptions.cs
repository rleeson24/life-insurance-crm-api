namespace LifeInsuranceCRM.Core.Config;

public sealed class KeyVaultOptions
{
    public const string SectionName = "KeyVault";

    /// <summary>
    /// Azure Key Vault URI, for example https://contoso.vault.azure.net/.
    /// Required outside Development. Leave empty for local Aspire / user secrets.
    /// </summary>
    public string VaultUri { get; set; } = string.Empty;

    /// <summary>
    /// Allows a developer workstation to load Key Vault via DefaultAzureCredential
    /// (Azure CLI / Visual Studio after JIT/PIM). Never enable for daily local work
    /// and never commit a true value.
    /// </summary>
    public bool AllowLocalAccess { get; set; }

    /// <summary>
    /// Name of the Key Vault key used for field-level PII envelope encryption (phase 2.1).
    /// The key material stays in Key Vault; this is only the key identifier.
    /// </summary>
    public string FieldEncryptionKeyName { get; set; } = "field-encryption";

    /// <summary>
    /// Secret names as stored in Key Vault. The configuration provider maps <c>--</c> to <c>:</c>.
    /// </summary>
    public static class SecretNames
    {
        public const string DatabaseConnectionString = "Database--ConnectionString";
        public const string ApplicationInsightsConnectionString = "ApplicationInsights--ConnectionString";
        public const string AzureAdTenantId = "AzureAd--TenantId";
        public const string AzureAdClientId = "AzureAd--ClientId";
        public const string AzureAdAudience = "AzureAd--Audience";
        public const string FieldEncryptionKey = "FieldEncryption--Key";
        public const string FieldEncryptionWrappedDek = "FieldEncryption--WrappedDek";
        public const string FieldEncryptionBlindIndexKey = "FieldEncryption--BlindIndexKey";
    }
}
