using System.Diagnostics.CodeAnalysis;
using LifeInsuranceCRM.Core.Constants;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class ListPlanNamesRequest
{
    public PlanNameKind Kind { get; init; }
    public short PlanYear { get; init; }
}
