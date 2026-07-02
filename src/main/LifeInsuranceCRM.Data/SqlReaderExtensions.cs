using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data;

internal static class SqlReaderExtensions
{
    public static Guid GetGuid(this SqlDataReader reader, string name) => reader.GetGuid(reader.GetOrdinal(name));

    public static string? GetNullableString(this SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public static DateOnly? GetNullableDateOnly(this SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : DateOnly.FromDateTime(reader.GetDateTime(ordinal));
    }

    public static DateTimeOffset GetDateTimeOffset(this SqlDataReader reader, string name) =>
        reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal(name));

    public static DateTimeOffset? GetNullableDateTimeOffset(this SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<DateTimeOffset>(ordinal);
    }

    public static bool GetBoolean(this SqlDataReader reader, string name) => reader.GetBoolean(reader.GetOrdinal(name));
}
