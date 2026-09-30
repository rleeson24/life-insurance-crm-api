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
    public const string ReportExported = "ReportExported";
    public const string ReportViewed = "ReportViewed";
    public const string ClientListed = "ClientListed";
    public const string ClientViewed = "ClientViewed";
    public const string ClientDetailViewed = "ClientDetailViewed";
    public const string EnrollmentListed = "EnrollmentListed";
    public const string InteractionListed = "InteractionListed";
    public const string FollowUpsListed = "FollowUpsListed";
    public const string DataImported = "DataImported";
    public const string ClientUpdated = "ClientUpdated";
    public const string ClientDeleted = "ClientDeleted";
    public const string EnrollmentDeleted = "EnrollmentDeleted";
    public const string InteractionDeleted = "InteractionDeleted";
    public const string OrganizationUserChanged = "OrganizationUserChanged";
    public const string TenantChanged = "TenantChanged";
}
