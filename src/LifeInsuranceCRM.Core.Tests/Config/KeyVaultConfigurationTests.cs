using LifeInsuranceCRM.Core.Config;
using Microsoft.Extensions.Configuration;

namespace LifeInsuranceCRM.Core.Tests.Config;

public class KeyVaultConfigurationTests
{
    private const string VaultUri = "https://licrm-dev-kv.vault.azure.net/";

    [Fact]
    public void Evaluate_SkipsInDevelopmentWhenVaultUriIsMissing()
    {
        var decision = KeyVaultConfiguration.Evaluate(
            vaultUri: null,
            "Development",
            allowLocalAccess: false,
            managedIdentityAvailable: false);

        Assert.False(decision.ShouldLoad);
        Assert.Null(decision.Error);
    }

    [Fact]
    public void Evaluate_SkipsInDevelopmentWhenVaultUriIsSetWithoutOptIn()
    {
        var decision = KeyVaultConfiguration.Evaluate(
            VaultUri,
            "Development",
            allowLocalAccess: false,
            managedIdentityAvailable: false);

        Assert.False(decision.ShouldLoad);
        Assert.Null(decision.Error);
    }

    [Fact]
    public void Evaluate_LoadsInDevelopmentWhenAllowLocalAccessIsTrue()
    {
        var decision = KeyVaultConfiguration.Evaluate(
            VaultUri,
            "Development",
            allowLocalAccess: true,
            managedIdentityAvailable: false);

        Assert.True(decision.ShouldLoad);
        Assert.Equal(VaultUri, decision.VaultUri);
        Assert.True(decision.AllowDeveloperCredentials);
        Assert.Null(decision.Error);
    }

    [Fact]
    public void Evaluate_LoadsWithManagedIdentityWithoutDeveloperCredentials()
    {
        var decision = KeyVaultConfiguration.Evaluate(
            VaultUri,
            "Production",
            allowLocalAccess: false,
            managedIdentityAvailable: true);

        Assert.True(decision.ShouldLoad);
        Assert.Equal(VaultUri, decision.VaultUri);
        Assert.False(decision.AllowDeveloperCredentials);
        Assert.Null(decision.Error);
    }

    [Fact]
    public void Evaluate_IgnoresAllowLocalAccessWhenManagedIdentityIsAvailable()
    {
        var decision = KeyVaultConfiguration.Evaluate(
            VaultUri,
            "Production",
            allowLocalAccess: true,
            managedIdentityAvailable: true);

        Assert.True(decision.ShouldLoad);
        Assert.False(decision.AllowDeveloperCredentials);
    }

    [Fact]
    public void Evaluate_FailsOutsideDevelopmentWhenVaultUriIsMissing()
    {
        var decision = KeyVaultConfiguration.Evaluate(
            vaultUri: "  ",
            "Production",
            allowLocalAccess: false,
            managedIdentityAvailable: false);

        Assert.False(decision.ShouldLoad);
        Assert.Contains("KeyVault:VaultUri is required", decision.Error);
        var error = Assert.Throws<InvalidOperationException>(decision.EnsureSuccess);
        Assert.Equal(decision.Error, error.Message);
    }

    [Fact]
    public void Evaluate_FailsOutsideDevelopmentWithoutManagedIdentityOrOptIn()
    {
        var decision = KeyVaultConfiguration.Evaluate(
            VaultUri,
            "Production",
            allowLocalAccess: false,
            managedIdentityAvailable: false);

        Assert.False(decision.ShouldLoad);
        Assert.Contains("AllowLocalAccess", decision.Error);
    }

    [Fact]
    public void Evaluate_LoadsFromWorkstationWhenAllowLocalAccessIsTrue()
    {
        var decision = KeyVaultConfiguration.Evaluate(
            $" {VaultUri} ",
            "Production",
            allowLocalAccess: true,
            managedIdentityAvailable: false);

        Assert.True(decision.ShouldLoad);
        Assert.Equal(VaultUri, decision.VaultUri);
        Assert.True(decision.AllowDeveloperCredentials);
    }

    [Theory]
    [InlineData("not-a-uri")]
    [InlineData("http://licrm-dev-kv.vault.azure.net/")]
    [InlineData("/relative")]
    public void Evaluate_FailsWhenVaultUriIsNotHttps(string vaultUri)
    {
        var decision = KeyVaultConfiguration.Evaluate(
            vaultUri,
            "Production",
            allowLocalAccess: false,
            managedIdentityAvailable: true);

        Assert.False(decision.ShouldLoad);
        Assert.Contains("https URI", decision.Error);
    }

    [Fact]
    public void EnsureSuccess_DoesNotThrowWhenDecisionSucceeded()
    {
        var decision = KeyVaultConfiguration.Evaluate(
            vaultUri: null,
            "Development",
            allowLocalAccess: false,
            managedIdentityAvailable: false);

        decision.EnsureSuccess();
    }
}

public class ApplicationInsightsConnectionStringResolverTests
{
    [Fact]
    public void Resolve_PrefersEnvironmentStyleKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "InstrumentationKey=env",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=vault",
            })
            .Build();

        var connectionString = ApplicationInsightsConnectionStringResolver.Resolve(configuration);

        Assert.Equal("InstrumentationKey=env", connectionString);
    }

    [Fact]
    public void Resolve_UsesKeyVaultStyleKeyWhenEnvironmentStyleIsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=vault",
            })
            .Build();

        var connectionString = ApplicationInsightsConnectionStringResolver.Resolve(configuration);

        Assert.Equal("InstrumentationKey=vault", connectionString);
    }

    [Fact]
    public void Resolve_ReturnsNullWhenNeitherKeyIsSet()
    {
        var configuration = new ConfigurationBuilder().Build();

        Assert.Null(ApplicationInsightsConnectionStringResolver.Resolve(configuration));
    }
}

public class KeyVaultOptionsTests
{
    [Fact]
    public void SecretNames_UseKeyVaultHierarchyDelimiter()
    {
        Assert.Equal("Database--ConnectionString", KeyVaultOptions.SecretNames.DatabaseConnectionString);
        Assert.Equal("ApplicationInsights--ConnectionString", KeyVaultOptions.SecretNames.ApplicationInsightsConnectionString);
        Assert.Equal("AzureAd--TenantId", KeyVaultOptions.SecretNames.AzureAdTenantId);
        Assert.Equal("AzureAd--ClientId", KeyVaultOptions.SecretNames.AzureAdClientId);
        Assert.Equal("AzureAd--Audience", KeyVaultOptions.SecretNames.AzureAdAudience);
    }

    [Fact]
    public void FieldEncryptionKeyName_DefaultsToFieldEncryption()
    {
        Assert.Equal("field-encryption", new KeyVaultOptions().FieldEncryptionKeyName);
    }
}
