using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class DeleteMajorMedicalEnrollmentRequest
{
    public Guid ClientId { get; init; }
    public Guid MajorMedicalEnrollmentId { get; init; }
}
