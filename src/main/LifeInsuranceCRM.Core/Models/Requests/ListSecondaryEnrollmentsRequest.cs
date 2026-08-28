using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class ListSecondaryEnrollmentsRequest
{
    public Guid ClientId { get; init; }
}
