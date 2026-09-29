using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]
public sealed class ListAuthSecurityEventsRequest
{
    public string? Search { get; init; }

    public string? EventType { get; init; }

    public bool? Success { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;
}
