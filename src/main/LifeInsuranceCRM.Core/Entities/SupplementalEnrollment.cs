namespace LifeInsuranceCRM.Core.Entities;

public sealed class SupplementalEnrollment
{
    public Guid SupplementalEnrollmentId { get; init; }
    public Guid TenantId { get; init; }
    public Guid ClientId { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
    public string? PlanOrCarrierName { get; init; }
    public DateOnly? CoverageStartDate { get; init; }
    public bool IsActiveCoverage { get; init; }
    public string? Notes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid CreatedByUserId { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public Guid UpdatedByUserId { get; init; }
}
