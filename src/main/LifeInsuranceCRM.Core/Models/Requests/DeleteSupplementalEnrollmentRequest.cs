using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class DeleteSupplementalEnrollmentRequest
{
    public Guid ClientId { get; init; }
    public Guid SupplementalEnrollmentId { get; init; }
}
