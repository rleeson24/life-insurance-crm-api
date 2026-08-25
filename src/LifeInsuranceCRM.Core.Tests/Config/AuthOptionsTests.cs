using LifeInsuranceCRM.Core.Config;
using Microsoft.Extensions.Configuration;

namespace LifeInsuranceCRM.Core.Tests.Config;

public class AuthOptionsTests
{
    [Fact]
    public void ShouldUseDevelopmentScheme_WhenFlagOff_IsFalse()
    {
        var options = new AuthOptions { UseDevelopmentAuthentication = false };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Assert.False(options.ShouldUseDevelopmentScheme(config));
    }

    [Fact]
    public void ShouldUseDevelopmentScheme_WhenFlagOnAndEntraMissing_IsTrue()
    {
        var options = new AuthOptions { UseDevelopmentAuthentication = true };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Assert.True(options.ShouldUseDevelopmentScheme(config));
    }

    [Fact]
    public void ShouldUseDevelopmentScheme_WhenFlagOnAndEntraConfigured_IsFalse()
    {
        var options = new AuthOptions { UseDevelopmentAuthentication = true };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:TenantId"] = "tenant-id",
                ["AzureAd:ClientId"] = "client-id",
            })
            .Build();

        Assert.False(options.ShouldUseDevelopmentScheme(config));
    }
}
