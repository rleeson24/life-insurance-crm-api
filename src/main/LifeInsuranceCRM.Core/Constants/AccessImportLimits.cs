namespace LifeInsuranceCRM.Core.Constants;

public static class AccessImportLimits
{
    public const long MaxRequestBodyBytes = 20 * 1024 * 1024;

    public const int MaxWarnings = 50;

    public const int CommandTimeoutSeconds = 180;

    public const int MinPlanYear = 1990;

    public const int MaxPlanYear = 2100;
}
