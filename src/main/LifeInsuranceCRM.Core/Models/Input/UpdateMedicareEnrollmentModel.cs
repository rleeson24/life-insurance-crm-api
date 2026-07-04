using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Input;

[ExcludeFromCodeCoverage]

public sealed record UpdateMedicareEnrollmentModel
{
    public Guid ClientId { get; init; }
    public Guid MedicareEnrollmentId { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
    public bool IsActivePlan { get; init; }
    public string? PlanName { get; init; }
    public string? PrescriptionDrugPlan { get; init; }
    public DateOnly? CoverageStartDate { get; init; }
    public bool IsNewEnrollment { get; init; }
    public string? HealthReimbursementArrangement { get; init; }
    public string? EnrollmentPlatform { get; init; }
    public string? EnrollmentLocation { get; init; }
    public string? Notes { get; init; }
}
