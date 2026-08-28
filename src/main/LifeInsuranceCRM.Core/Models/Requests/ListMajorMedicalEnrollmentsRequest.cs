using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class ListMajorMedicalEnrollmentsRequest
{
    public Guid ClientId { get; init; }
}
