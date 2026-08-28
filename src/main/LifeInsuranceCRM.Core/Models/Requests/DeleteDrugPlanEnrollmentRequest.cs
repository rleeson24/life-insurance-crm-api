using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class DeleteDrugPlanEnrollmentRequest
{
    public Guid ClientId { get; init; }
    public Guid DrugPlanEnrollmentId { get; init; }
}
