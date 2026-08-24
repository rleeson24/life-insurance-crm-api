using Microsoft.Extensions.Hosting;

namespace LifeInsuranceCRM.Core.Config;

public readonly record struct KeyVaultLoadDecision(
    bool ShouldLoad,
    string? VaultUri,
    bool AllowDeveloperCredentials,
    string? Error)
{
    public void EnsureSuccess()
    {
        if (!string.IsNullOrEmpty(Error))
        {
            throw new InvalidOperationException(Error);
        }
    }
}

public static class KeyVaultConfiguration
{
    public static bool IsManagedIdentityAvailable() =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT"))
        || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MSI_ENDPOINT"));

    public static KeyVaultLoadDecision Evaluate(
        string? vaultUri,
        string environmentName,
        bool allowLocalAccess,
        bool managedIdentityAvailable)
    {
        var isDevelopment = string.Equals(
            environmentName,
            Environments.Development,
            StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(vaultUri))
        {
            return isDevelopment
                ? Skip()
                : Fail(
                    "KeyVault:VaultUri is required when ASPNETCORE_ENVIRONMENT is not Development. " +
                    "Set KeyVault__VaultUri on the container app.");
        }

        if (!Uri.TryCreate(vaultUri, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            return Fail(
                "KeyVault:VaultUri must be an https URI (for example https://contoso.vault.azure.net/).");
        }

        if (managedIdentityAvailable || allowLocalAccess)
        {
            return Load(vaultUri.Trim(), allowDeveloperCredentials: allowLocalAccess && !managedIdentityAvailable);
        }

        return isDevelopment
            ? Skip()
            : Fail(
                "Refusing to load Azure Key Vault from a workstation. Production uses the container app " +
                "managed identity. For JIT/PIM access only, set KeyVault:AllowLocalAccess to true in user secrets.");
    }

    private static KeyVaultLoadDecision Skip() =>
        new(ShouldLoad: false, VaultUri: null, AllowDeveloperCredentials: false, Error: null);

    private static KeyVaultLoadDecision Load(string vaultUri, bool allowDeveloperCredentials) =>
        new(ShouldLoad: true, vaultUri, allowDeveloperCredentials, Error: null);

    private static KeyVaultLoadDecision Fail(string error) =>
        new(ShouldLoad: false, VaultUri: null, AllowDeveloperCredentials: false, error);
}
