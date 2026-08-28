using System.Diagnostics.CodeAnalysis;
using LifeInsuranceCRM.Core.Constants;

namespace LifeInsuranceCRM.Core.Models.Input;

[ExcludeFromCodeCoverage]

public sealed record ClonePlanNamesModel
{
    public PlanNameKind Kind { get; init; }
    public short SourceYear { get; init; }
    public short TargetYear { get; init; }
}
