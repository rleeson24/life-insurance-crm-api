using System.Diagnostics.CodeAnalysis;
using LifeInsuranceCRM.Core.Constants;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class LookupPlanNamesRequest
{
    public PlanNameKind Kind { get; init; }
    public short FromYear { get; init; }
    public short ToYear { get; init; }
}
