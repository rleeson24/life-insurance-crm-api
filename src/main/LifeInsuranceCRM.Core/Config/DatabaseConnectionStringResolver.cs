using Microsoft.Extensions.Configuration;

namespace LifeInsuranceCRM.Core.Config;

public static class DatabaseConnectionStringResolver
{
    public const string ManagedIdentityAuthentication = "Active Directory Default";

    public static string? Resolve(IConfiguration configuration)
    {
        var aspireConnectionString = configuration.GetConnectionString("LifeInsuranceCRM");
        if (!string.IsNullOrWhiteSpace(aspireConnectionString))
        {
            return aspireConnectionString;
        }

        var explicitConnectionString = configuration[$"{DatabaseOptions.SectionName}:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(explicitConnectionString))
        {
            return explicitConnectionString;
        }

        var server = configuration[$"{DatabaseOptions.SectionName}:Server"];
        var database = configuration[$"{DatabaseOptions.SectionName}:Name"];
        if (!string.IsNullOrWhiteSpace(server) && !string.IsNullOrWhiteSpace(database))
        {
            return BuildManagedIdentityConnectionString(server, database);
        }

        return null;
    }

    public static string BuildManagedIdentityConnectionString(string server, string database) =>
        $"Server=tcp:{server},1433;Initial Catalog={database};Authentication={ManagedIdentityAuthentication};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
}
