using System.Diagnostics.CodeAnalysis;
using LifeInsuranceCRM.Core.Constants;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class DeletePlanNameRequest
{
    public PlanNameKind Kind { get; init; }
    public Guid PlanNameId { get; init; }
}
