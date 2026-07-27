using LifeInsuranceCRM.Core.Config;
using Microsoft.Extensions.Configuration;

namespace LifeInsuranceCRM.Core.Tests.Config;

public class DatabaseConnectionStringResolverTests
{
    [Fact]
    public void Resolve_PrefersAspireConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LifeInsuranceCRM"] = "Server=aspire;",
                [$"{DatabaseOptions.SectionName}:ConnectionString"] = "Server=explicit;",
                [$"{DatabaseOptions.SectionName}:Server"] = "prod-sql.database.windows.net",
                [$"{DatabaseOptions.SectionName}:Name"] = "LifeInsuranceCRM",
            })
            .Build();

        var connectionString = DatabaseConnectionStringResolver.Resolve(configuration);

        Assert.Equal("Server=aspire;", connectionString);
    }

    [Fact]
    public void Resolve_UsesExplicitConnectionStringWhenAspireMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{DatabaseOptions.SectionName}:ConnectionString"] = "Server=explicit;",
                [$"{DatabaseOptions.SectionName}:Server"] = "prod-sql.database.windows.net",
                [$"{DatabaseOptions.SectionName}:Name"] = "LifeInsuranceCRM",
            })
            .Build();

        var connectionString = DatabaseConnectionStringResolver.Resolve(configuration);

        Assert.Equal("Server=explicit;", connectionString);
    }

    [Fact]
    public void Resolve_BuildsManagedIdentityConnectionStringFromServerAndName()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{DatabaseOptions.SectionName}:Server"] = "licrm-dev-sql.database.windows.net",
                [$"{DatabaseOptions.SectionName}:Name"] = "LifeInsuranceCRM",
            })
            .Build();

        var connectionString = DatabaseConnectionStringResolver.Resolve(configuration);

        Assert.Equal(
            DatabaseConnectionStringResolver.BuildManagedIdentityConnectionString(
                "licrm-dev-sql.database.windows.net",
                "LifeInsuranceCRM"),
            connectionString);
    }

    [Fact]
    public void Resolve_ReturnsNullWhenNoDatabaseConfigurationExists()
    {
        var configuration = new ConfigurationBuilder().Build();

        var connectionString = DatabaseConnectionStringResolver.Resolve(configuration);

        Assert.Null(connectionString);
    }

    [Fact]
    public void BuildManagedIdentityConnectionString_UsesActiveDirectoryDefaultAuthentication()
    {
        var connectionString = DatabaseConnectionStringResolver.BuildManagedIdentityConnectionString(
            "licrm-prod-sql.database.windows.net",
            "LifeInsuranceCRM");

        Assert.Contains($"Authentication={DatabaseConnectionStringResolver.ManagedIdentityAuthentication};", connectionString);
        Assert.Contains("Encrypt=True;", connectionString);
        Assert.Contains("Initial Catalog=LifeInsuranceCRM;", connectionString);
    }
}
