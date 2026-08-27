using LifeInsuranceCRM.Utilities;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace LifeInsuranceCRM.ServiceDefaults;

internal sealed class PiiRedactingLogProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord data)
    {
        if (data.FormattedMessage is not null)
        {
            data.FormattedMessage = PiiRedactor.Redact(data.FormattedMessage);
        }

        if (data.Body is string body)
        {
            data.Body = PiiRedactor.Redact(body);
        }

        if (data.Exception is not null)
        {
            data.Exception = PiiRedactor.ToSanitizedException(data.Exception);
        }

        if (data.Attributes is { Count: > 0 } attributes)
        {
            data.Attributes = RedactAttributes(attributes);
        }
    }

    private static IReadOnlyList<KeyValuePair<string, object?>> RedactAttributes(
        IReadOnlyList<KeyValuePair<string, object?>> attributes)
    {
        var redacted = new KeyValuePair<string, object?>[attributes.Count];
        for (var i = 0; i < attributes.Count; i++)
        {
            var attribute = attributes[i];
            if (PiiRedactor.IsPhiAttributeKey(attribute.Key))
            {
                redacted[i] = new KeyValuePair<string, object?>(attribute.Key, TelemetryConstants.RedactedPlaceholder);
                continue;
            }

            redacted[i] = attribute.Value is string text
                ? new KeyValuePair<string, object?>(attribute.Key, PiiRedactor.Redact(text))
                : attribute;
        }

        return redacted;
    }
}
