using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Input;

[ExcludeFromCodeCoverage]

public sealed record UpdateSupplementalEnrollmentModel
{
    public Guid ClientId { get; init; }
    public Guid SupplementalEnrollmentId { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
    public string? PlanOrCarrierName { get; init; }
    public DateOnly? CoverageStartDate { get; init; }
    public bool IsActiveCoverage { get; init; }
    public string? Notes { get; init; }
}
