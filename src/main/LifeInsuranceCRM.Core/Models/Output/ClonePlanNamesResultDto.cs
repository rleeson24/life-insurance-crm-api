using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Output;

[ExcludeFromCodeCoverage]

public sealed class ClonePlanNamesResultDto
{
    public int SourceCount { get; init; }
    public int ClonedCount { get; init; }
    public int SkippedCount { get; init; }
    public IReadOnlyList<PlanNameDto> Items { get; init; } = [];
}
