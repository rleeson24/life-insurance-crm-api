using System.Text.RegularExpressions;

namespace LifeInsuranceCRM.Utilities;

/// <summary>
/// Masks Medicare numbers, dates of birth, and similarly tagged PHI in log text and exceptions.
/// Auth emails are left intact; do not put client PHI in <c>AuthSecurityEvents</c>.
/// </summary>
public static class PiiRedactor
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly Regex NamedPhiValues = new(
        """
        (?ix)
        (?<prefix>
            @?(MedicareNumber|DateOfBirth)
            |
            ["'\[](?:MedicareNumber|DateOfBirth)["'\]]
        )
        \s*(?<sep>[:=])\s*
        (?<value>
            '(?:''|[^'])*'
            |
            "(?:\\.|[^"\\])*"
            |
            [^\s,;)}\]]+
        )
        """,
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        MatchTimeout);

    // CMS Medicare Beneficiary Identifier (optional dashes). Keep on one line so whitespace is not significant.
    private static readonly Regex MedicareBeneficiaryId = new(
        @"\b[1-9][AC-HJKMNP-RT-Y][0-9AC-HJKMNP-RT-Y][0-9]-?[AC-HJKMNP-RT-Y][0-9AC-HJKMNP-RT-Y][0-9]-?[AC-HJKMNP-RT-Y][AC-HJKMNP-RT-Y][0-9AC-HJKMNP-RT-Y][0-9]\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        MatchTimeout);

    public static bool IsPhiAttributeKey(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        var name = key.AsSpan().TrimStart('@');
        return name.Equals("MedicareNumber", StringComparison.OrdinalIgnoreCase)
            || name.Equals("DateOfBirth", StringComparison.OrdinalIgnoreCase)
            || name.Equals("db.query.text", StringComparison.OrdinalIgnoreCase)
            || name.Equals("db.statement", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("db.query.parameter", StringComparison.OrdinalIgnoreCase);
    }

    public static string? Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        try
        {
            var redacted = NamedPhiValues.Replace(value, ReplaceNamedPhiValue);
            return MedicareBeneficiaryId.Replace(redacted, TelemetryConstants.RedactedPlaceholder);
        }
        catch (RegexMatchTimeoutException)
        {
            return TelemetryConstants.RedactedPlaceholder;
        }
    }

    private static string ReplaceNamedPhiValue(Match match)
    {
        var originalValue = match.Groups["value"].Value;
        if (IsAlreadyRedacted(originalValue))
        {
            return match.Value;
        }

        return $"{match.Groups["prefix"].Value}{match.Groups["sep"].Value}{WrapPlaceholder(originalValue)}";
    }

    private static bool IsAlreadyRedacted(string value)
    {
        var placeholder = TelemetryConstants.RedactedPlaceholder;
        return value.Equals(placeholder, StringComparison.Ordinal)
            || value.Equals($"\"{placeholder}\"", StringComparison.Ordinal)
            || value.Equals($"'{placeholder}'", StringComparison.Ordinal)
            || value.StartsWith("[REDACTED", StringComparison.Ordinal);
    }

    private static string WrapPlaceholder(string originalValue)
    {
        var placeholder = TelemetryConstants.RedactedPlaceholder;
        if (originalValue.Length >= 2
            && ((originalValue[0] == '"' && originalValue[^1] == '"')
                || (originalValue[0] == '\'' && originalValue[^1] == '\'')))
        {
            return $"{originalValue[0]}{placeholder}{originalValue[^1]}";
        }

        return placeholder;
    }

    public static Exception ToSanitizedException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new PiiSanitizedException(exception);
    }
}

/// <summary>
/// Exception whose message and stack text have been run through <see cref="PiiRedactor"/>.
/// Does not wrap the original exception, so inner messages cannot leak PHI.
/// </summary>
public sealed class PiiSanitizedException : Exception
{
    public PiiSanitizedException(Exception source)
        : base(PiiRedactor.Redact(source.ToString()))
    {
        OriginalExceptionType = source.GetType().FullName ?? source.GetType().Name;
    }

    public string OriginalExceptionType { get; }
}
