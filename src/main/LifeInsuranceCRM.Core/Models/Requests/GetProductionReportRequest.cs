using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class GetProductionReportRequest
{
    public short PlanYear { get; init; }
}
