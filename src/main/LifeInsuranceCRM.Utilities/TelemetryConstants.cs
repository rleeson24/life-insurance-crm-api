using System.Diagnostics;

namespace LifeInsuranceCRM.Utilities;

public static class TelemetryConstants
{
    public const string ActivitySourceName = "LifeInsuranceCRM";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
