namespace LifeInsuranceCRM.Core.Config;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int AnonymousRequestsPerMinute { get; set; } = 30;

    public int AuthenticatedRequestsPerMinute { get; set; } = 300;

    public int SecuritySensitiveRequestsPerMinute { get; set; } = 20;

    public int AuthFailuresPerFiveMinutes { get; set; } = 10;
}
