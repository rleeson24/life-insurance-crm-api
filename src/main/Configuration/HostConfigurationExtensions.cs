using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using LifeInsuranceCRM.Core.Config;
using Microsoft.Extensions.Configuration;

namespace LifeInsuranceCRM.API.Configuration;

internal static class HostConfigurationExtensions
{
    public static void AddAzureKeyVaultConfiguration(this ConfigurationManager configuration)
    {
        var vaultUri = configuration[$"{KeyVaultOptions.SectionName}:VaultUri"];
        if (string.IsNullOrWhiteSpace(vaultUri))
        {
            return;
        }

        configuration.AddAzureKeyVault(new Uri(vaultUri), new DefaultAzureCredential());
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
}
