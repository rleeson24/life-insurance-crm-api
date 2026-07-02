namespace LifeInsuranceCRM.Core.Models.Input;

public sealed record CreateSupplementalEnrollmentModel
{
    public Guid ClientId { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
    public string? PlanOrCarrierName { get; init; }
    public DateOnly? CoverageStartDate { get; init; }
    public bool IsActiveCoverage { get; init; }
    public string? Notes { get; init; }
}
