using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Output;

[ExcludeFromCodeCoverage]

public sealed class SupplementalEnrollmentDto
{
    public Guid SupplementalEnrollmentId { get; init; }
    public Guid ClientId { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
    public string? PlanOrCarrierName { get; init; }
    public DateOnly? CoverageStartDate { get; init; }
    public bool IsActiveCoverage { get; init; }
    public string? Notes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
