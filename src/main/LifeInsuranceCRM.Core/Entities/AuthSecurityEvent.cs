namespace LifeInsuranceCRM.Core.Entities;

public sealed class AuthSecurityEvent
{
    public Guid AuthSecurityEventId { get; init; }

    public Guid? TenantId { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    public required string EventType { get; init; }

    public Guid? UserId { get; init; }

    public string? UserEmail { get; init; }

    public bool Success { get; init; }

    public string? FailureReason { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string? CorrelationId { get; init; }

    public string? Resource { get; init; }
}
