using Microsoft.Extensions.Configuration;

namespace LifeInsuranceCRM.Core.Config;

public static class ApplicationInsightsConnectionStringResolver
{
    public static string? Resolve(IConfiguration configuration)
    {
        var fromEnvironment = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var fromKeyVault = configuration["ApplicationInsights:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(fromKeyVault))
        {
            return fromKeyVault;
        }

        return null;
    }
}
