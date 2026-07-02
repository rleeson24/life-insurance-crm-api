namespace LifeInsuranceCRM.Core.Models.Requests;

public sealed class DeleteSupplementalEnrollmentRequest
{
    public Guid ClientId { get; init; }
    public Guid SupplementalEnrollmentId { get; init; }
}
