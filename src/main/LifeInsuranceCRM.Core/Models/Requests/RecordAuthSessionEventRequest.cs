using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]
public sealed class RecordAuthSessionEventRequest
{
    public required string EventType { get; init; }

    public string? FailureReason { get; init; }
}
