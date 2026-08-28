using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class ListDrugPlanEnrollmentsRequest
{
    public Guid ClientId { get; init; }
}
