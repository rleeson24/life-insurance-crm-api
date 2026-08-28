using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class DeleteSecondaryEnrollmentRequest
{
    public Guid ClientId { get; init; }
    public Guid SecondaryEnrollmentId { get; init; }
}
