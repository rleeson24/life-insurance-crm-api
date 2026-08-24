using Azure.Core;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using LifeInsuranceCRM.Core.Config;
using Microsoft.Extensions.Configuration;

namespace LifeInsuranceCRM.API.Configuration;

internal static class HostConfigurationExtensions
{
    public static void AddAzureKeyVaultConfiguration(
        this ConfigurationManager configuration,
        IHostEnvironment environment,
        bool? managedIdentityAvailable = null)
    {
        var options = configuration.GetSection(KeyVaultOptions.SectionName).Get<KeyVaultOptions>()
            ?? new KeyVaultOptions();

        var decision = KeyVaultConfiguration.Evaluate(
            options.VaultUri,
            environment.EnvironmentName,
            options.AllowLocalAccess,
            managedIdentityAvailable ?? KeyVaultConfiguration.IsManagedIdentityAvailable());

        decision.EnsureSuccess();
        if (!decision.ShouldLoad || string.IsNullOrWhiteSpace(decision.VaultUri))
        {
            return;
        }

        configuration.AddAzureKeyVault(
            new Uri(decision.VaultUri),
            CreateCredential(decision.AllowDeveloperCredentials));
    }

    public static void ConfigureDatabaseOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(options =>
        {
            configuration.GetSection(DatabaseOptions.SectionName).Bind(options);

            var resolvedConnectionString = DatabaseConnectionStringResolver.Resolve(configuration);
            if (!string.IsNullOrWhiteSpace(resolvedConnectionString))
            {
                options.ConnectionString = resolvedConnectionString;
            }
        });
    }

    private static TokenCredential CreateCredential(bool allowDeveloperCredentials) =>
        allowDeveloperCredentials
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
