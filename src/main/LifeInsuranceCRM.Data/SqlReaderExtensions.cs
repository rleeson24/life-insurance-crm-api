using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data;

internal static class SqlReaderExtensions
{
    public static Guid GetGuid(this SqlDataReader reader, string name) => reader.GetGuid(reader.GetOrdinal(name));

    public static Guid? GetNullableGuid(this SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    }

    public static short GetInt16(this SqlDataReader reader, string name) =>
        reader.GetInt16(reader.GetOrdinal(name));

    public static int GetInt32(this SqlDataReader reader, string name) =>
        reader.GetInt32(reader.GetOrdinal(name));

    public static short? GetNullableInt16(this SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt16(ordinal);
    }

    public static int? GetNullableInt32(this SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

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

    public static byte[]? GetNullableBytes(this SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<byte[]>(ordinal);
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
