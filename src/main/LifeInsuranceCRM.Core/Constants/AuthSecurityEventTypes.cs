namespace LifeInsuranceCRM.Core.Constants;

public static class AuthSecurityEventTypes
{
    public const string LoginSucceeded = "LoginSucceeded";
    public const string LoginFailed = "LoginFailed";
    public const string Logout = "Logout";
    public const string TokenValidationFailed = "TokenValidationFailed";
    public const string TokenExpired = "TokenExpired";
    public const string TenantResolved = "TenantResolved";
    public const string TenantAccessDenied = "TenantAccessDenied";
    public const string Forbidden = "Forbidden";
    public const string Unauthorized = "Unauthorized";
    public const string RateLimitExceeded = "RateLimitExceeded";
}
