using System.Diagnostics;
using LifeInsuranceCRM.Utilities;
using OpenTelemetry;

namespace LifeInsuranceCRM.ServiceDefaults;

internal sealed class SqlStatementRedactingProcessor : BaseProcessor<Activity>
{
    private static readonly string[] SqlTextTagKeys =
    [
        "db.query.text",
        "db.statement",
        "db.query.summary",
    ];

    private readonly bool _omitSqlText;

    public SqlStatementRedactingProcessor(bool omitSqlText)
    {
        _omitSqlText = omitSqlText;
    }

    public override void OnEnd(Activity activity)
    {
        var containsPhi = IsTruthy(activity.GetTagItem(TelemetryConstants.ContainsPhiSqlTag));
        if (_omitSqlText || containsPhi)
        {
            OmitSqlText(activity);
            activity.SetTag(TelemetryConstants.DbStatementOmittedTag, true);
            return;
        }

        RedactSqlText(activity);
    }

    private static void OmitSqlText(Activity activity)
    {
        foreach (var key in SqlTextTagKeys)
        {
            if (activity.GetTagItem(key) is not null)
            {
                activity.SetTag(key, TelemetryConstants.RedactedPlaceholder);
            }
        }

        foreach (var tag in activity.TagObjects)
        {
            if (tag.Key.StartsWith("db.query.parameter", StringComparison.OrdinalIgnoreCase))
            {
                activity.SetTag(tag.Key, TelemetryConstants.RedactedPlaceholder);
            }
        }
    }

    private static void RedactSqlText(Activity activity)
    {
        foreach (var key in SqlTextTagKeys)
        {
            if (activity.GetTagItem(key) is string text)
            {
                activity.SetTag(key, PiiRedactor.Redact(text));
            }
        }
    }

    private static bool IsTruthy(object? value) =>
        value is true or "true" or "True" or "1";
}
