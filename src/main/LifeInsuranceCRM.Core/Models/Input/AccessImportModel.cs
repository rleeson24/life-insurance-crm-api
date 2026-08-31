using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace LifeInsuranceCRM.Core.Models.Input;

[ExcludeFromCodeCoverage]
public sealed class AccessImportModel
{
    public IReadOnlyList<Dictionary<string, JsonElement>>? Clients { get; init; }

    public IReadOnlyList<Dictionary<string, JsonElement>>? MedEnrollments { get; init; }

    public IReadOnlyList<Dictionary<string, JsonElement>>? OtherEnrollments { get; init; }

    public IReadOnlyList<Dictionary<string, JsonElement>>? Contacts { get; init; }

    /// <summary>
    /// Minutes to add to local time to get UTC, matching JavaScript <c>Date#getTimezoneOffset()</c>.
    /// </summary>
    public int TimeZoneOffsetMinutes { get; init; }
}
