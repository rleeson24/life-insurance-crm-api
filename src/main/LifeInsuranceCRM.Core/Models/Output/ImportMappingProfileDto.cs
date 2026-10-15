using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Output;

[ExcludeFromCodeCoverage]
public sealed class ImportMappingProfileDto
{
    public Guid ImportMappingProfileId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Shape { get; init; } = string.Empty;

    public Dictionary<string, string?> Tables { get; init; } = [];

    public Dictionary<string, Dictionary<string, string>> Columns { get; init; } = [];
}
