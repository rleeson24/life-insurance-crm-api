namespace LifeInsuranceCRM.Core.Models.Requests;

public sealed class DeleteMedicareEnrollmentRequest
{
    public Guid ClientId { get; init; }
    public Guid MedicareEnrollmentId { get; init; }
}
