using System.Diagnostics.CodeAnalysis;
using LifeInsuranceCRM.Core.Constants;

namespace LifeInsuranceCRM.Core.Models.Input;

[ExcludeFromCodeCoverage]

public sealed record CreatePlanNameModel
{
    public PlanNameKind Kind { get; init; }
    public short PlanYear { get; init; }
    public string? Name { get; init; }
}
