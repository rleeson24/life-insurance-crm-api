using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Output;

[ExcludeFromCodeCoverage]
public sealed class AuthSecurityEventDto
{
    public Guid AuthSecurityEventId { get; init; }

    public Guid? TenantId { get; init; }

    public string? TenantName { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    public string EventType { get; init; } = string.Empty;

    public Guid? UserId { get; init; }

    public string? UserEmail { get; init; }

    public bool Success { get; init; }

    public string? FailureReason { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string? CorrelationId { get; init; }

    public string? Resource { get; init; }
}
