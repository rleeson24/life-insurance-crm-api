using System.Diagnostics.CodeAnalysis;
using LifeInsuranceCRM.Core.Constants;

namespace LifeInsuranceCRM.Core.Models.Input;

[ExcludeFromCodeCoverage]

public sealed record UpdatePlanNameModel
{
    public PlanNameKind Kind { get; init; }
    public Guid PlanNameId { get; init; }
    public string? Name { get; init; }
}
