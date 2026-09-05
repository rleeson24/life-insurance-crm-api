using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Input;

[ExcludeFromCodeCoverage]

public sealed record CreateMajorMedicalEnrollmentModel
{
    public Guid ClientId { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
    public bool IsActivePlan { get; init; } = true;
    public string? PlanName { get; init; }
    public DateOnly? CoverageStartDate { get; init; }
    public bool IsNewEnrollment { get; init; }
    public bool HealthReimbursementArrangement { get; init; }
    public string? EnrollmentPlatform { get; init; }
    public string? EnrollmentLocation { get; init; }
    public string? Notes { get; init; }
}
