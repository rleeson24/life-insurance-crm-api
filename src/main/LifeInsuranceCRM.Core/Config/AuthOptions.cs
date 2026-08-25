using Microsoft.Extensions.Configuration;

namespace LifeInsuranceCRM.Core.Config;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string? AzureAdInstance { get; set; } = "https://login.microsoftonline.com/";

    public string? TenantId { get; set; }

    public string? ClientId { get; set; }

    public string? Audience { get; set; }

    /// <summary>
    /// When true, accepts a synthetic dev JWT scheme for local testing without Entra.
    /// Ignored when <c>AzureAd:ClientId</c> and <c>AzureAd:TenantId</c> are set.
    /// Never enable in production.
    /// </summary>
    public bool UseDevelopmentAuthentication { get; set; }

    public Guid DevelopmentUserId { get; set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Guid DevelopmentTenantId { get; set; } = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public string DevelopmentUserEmail { get; set; } = "dev-user@localhost";

    public bool ShouldUseDevelopmentScheme(IConfiguration configuration)
    {
        if (!UseDevelopmentAuthentication)
        {
            return false;
        }

        var tenantId = configuration["AzureAd:TenantId"];
        var clientId = configuration["AzureAd:ClientId"];
        return string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId);
    }
}
