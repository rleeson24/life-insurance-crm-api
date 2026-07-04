using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class ListSupplementalEnrollmentsRequest
{
    public Guid ClientId { get; init; }
}
