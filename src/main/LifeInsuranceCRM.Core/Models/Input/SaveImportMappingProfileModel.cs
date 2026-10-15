using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Input;

[ExcludeFromCodeCoverage]
public sealed class SaveImportMappingProfileModel
{
    public string? Name { get; init; }

    public string? Shape { get; init; }

    public Dictionary<string, string?>? Tables { get; init; }

    public Dictionary<string, Dictionary<string, string?>?>? Columns { get; init; }
}
