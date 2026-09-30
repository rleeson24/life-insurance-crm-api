using LifeInsuranceCRM.API.Auth;
using LifeInsuranceCRM.Core.Constants;
using Microsoft.IdentityModel.Tokens;

namespace LifeInsuranceCRM.API.Tests.Auth;

public class JwtAuthenticationFailureTests
{
    [Fact]
    public void EventTypeFor_WhenTokenExpired_ReturnsTokenExpired()
    {
        var exception = new SecurityTokenExpiredException("expired");

        Assert.Equal(AuthSecurityEventTypes.TokenExpired, JwtAuthenticationFailure.EventTypeFor(exception));
        Assert.Equal("Token expired", JwtAuthenticationFailure.ReasonFor(exception));
    }

    [Fact]
    public void EventTypeFor_WhenTokenIsOtherwiseInvalid_ReturnsTokenValidationFailed()
    {
        var exception = new SecurityTokenInvalidSignatureException("bad signature");

        Assert.Equal(
            AuthSecurityEventTypes.TokenValidationFailed,
            JwtAuthenticationFailure.EventTypeFor(exception));
        Assert.Equal("Token validation failed", JwtAuthenticationFailure.ReasonFor(exception));
    }
}
