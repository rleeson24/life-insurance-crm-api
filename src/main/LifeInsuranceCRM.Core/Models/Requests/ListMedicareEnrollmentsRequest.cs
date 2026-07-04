using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class ListMedicareEnrollmentsRequest
{
    public Guid ClientId { get; init; }
}
