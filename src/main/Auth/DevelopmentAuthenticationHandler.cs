using System.Security.Claims;
using System.Text.Encodings.Web;
using LifeInsuranceCRM.Core.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace LifeInsuranceCRM.API.Auth;

public static class DevelopmentAuthenticationDefaults
{
    public const string Scheme = "Development";
}

public sealed class DevelopmentAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AuthOptions _authOptions;

    public DevelopmentAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<AuthOptions> authOptions)
        : base(options, logger, encoder)
    {
        _authOptions = authOptions.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _authOptions.DevelopmentUserId.ToString()),
            new Claim("oid", _authOptions.DevelopmentUserId.ToString()),
            new Claim(ClaimTypes.Email, _authOptions.DevelopmentUserEmail),
            new Claim(ClaimTypes.Name, _authOptions.DevelopmentUserEmail),
        };

        var identity = new ClaimsIdentity(claims, DevelopmentAuthenticationDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, DevelopmentAuthenticationDefaults.Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
