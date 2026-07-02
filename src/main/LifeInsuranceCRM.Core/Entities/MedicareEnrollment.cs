namespace LifeInsuranceCRM.Core.Entities;

public sealed class MedicareEnrollment
{
    public Guid MedicareEnrollmentId { get; init; }
    public Guid TenantId { get; init; }
    public Guid ClientId { get; init; }
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
    public DateTimeOffset CreatedAt { get; init; }
    public Guid CreatedByUserId { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public Guid UpdatedByUserId { get; init; }
}
