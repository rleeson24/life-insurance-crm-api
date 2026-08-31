using System.Globalization;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LifeInsuranceCRM.Core.Mappers;

internal static class AccessRowReader
{
    private static readonly Regex NonKeyChars = new(@"[\s\[\]*#]", RegexOptions.Compiled);

    public static IReadOnlyDictionary<string, JsonElement> Index(
        IReadOnlyDictionary<string, JsonElement> row)
    {
        var index = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in row)
        {
            var key = NormalizeKey(pair.Key);
            if (key.Length == 0)
            {
                continue;
            }

            index[key] = pair.Value;
        }

        return index;
    }

    public static string NormalizeKey(string key) =>
        NonKeyChars.Replace(key.Trim(), string.Empty).ToLowerInvariant();

    public static string? GetString(
        IReadOnlyDictionary<string, JsonElement> row,
        int maxLength,
        params string[] names)
    {
        var raw = GetRawString(row, names);
        return Truncate(raw, maxLength);
    }

    public static string? GetRawString(
        IReadOnlyDictionary<string, JsonElement> row,
        params string[] names)
    {
        if (!TryGet(row, names, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        var text = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return text.Trim();
    }

    public static bool GetBool(
        IReadOnlyDictionary<string, JsonElement> row,
        bool defaultValue,
        params string[] names)
    {
        if (!TryGet(row, names, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return defaultValue;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => value.TryGetInt64(out var number)
                ? number != 0
                : value.TryGetDouble(out var real) && Math.Abs(real) > 0.0000001,
            JsonValueKind.String => IsTruthy(value.GetString()),
            _ => defaultValue,
        };
    }

    public static long? GetInt64(
        IReadOnlyDictionary<string, JsonElement> row,
        params string[] names)
    {
        if (!TryGet(row, names, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number)
        {
            if (value.TryGetInt64(out var number))
            {
                return number;
            }

            if (value.TryGetDouble(out var real))
            {
                return (long)real;
            }

            return null;
        }

        if (value.ValueKind == JsonValueKind.String
            && long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    public static DateTime? GetDateTime(
        IReadOnlyDictionary<string, JsonElement> row,
        params string[] names)
    {
        if (!TryGet(row, names, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var offset))
            {
                return offset.UtcDateTime;
            }

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var oaDate))
        {
            try
            {
                return DateTime.FromOADate(oaDate);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        return null;
    }

    public static string? GetPostalCode(IReadOnlyDictionary<string, JsonElement> row, params string[] names)
    {
        if (!TryGet(row, names, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        string? text = null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
        {
            text = Math.Abs(number - Math.Truncate(number)) < 0.0000001
                ? ((long)number).ToString(CultureInfo.InvariantCulture)
                : number.ToString(CultureInfo.InvariantCulture);
        }
        else if (value.ValueKind == JsonValueKind.String)
        {
            text = value.GetString();
        }

        return Truncate(string.IsNullOrWhiteSpace(text) ? null : text.Trim(), 10);
    }

    public static string? GetEmail(IReadOnlyDictionary<string, JsonElement> row, params string[] names)
    {
        var raw = GetRawString(row, names);
        var extracted = ExtractEmail(raw);
        if (extracted is null || extracted.Length > 320 || !IsValidEmail(extracted))
        {
            return null;
        }

        return extracted;
    }

    public static string? GetState(IReadOnlyDictionary<string, JsonElement> row, params string[] names)
    {
        var raw = GetRawString(row, names);
        if (raw is null)
        {
            return null;
        }

        return raw.Length == 2 ? raw.ToUpperInvariant() : null;
    }

    private static bool TryGet(
        IReadOnlyDictionary<string, JsonElement> row,
        string[] names,
        out JsonElement value)
    {
        foreach (var name in names)
        {
            if (row.TryGetValue(NormalizeKey(name), out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool IsTruthy(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        return trimmed.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("y", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("true", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("1", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("x", StringComparison.OrdinalIgnoreCase);
    }

    internal static string? ExtractEmail(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var text = raw.Trim();
        if (text.Contains('#', StringComparison.Ordinal))
        {
            var parts = text.Split('#', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                var candidate = StripMailto(part);
                if (IsValidEmail(candidate))
                {
                    return candidate;
                }
            }
        }

        text = StripMailto(text);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string StripMailto(string value)
    {
        const string prefix = "mailto:";
        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? value[prefix.Length..].Trim()
            : value.Trim();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }

    public static string FormatAccessClientId(long? accessClientId) =>
        accessClientId is null ? "unknown" : accessClientId.Value.ToString(CultureInfo.InvariantCulture);
}
