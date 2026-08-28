using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Output;

[ExcludeFromCodeCoverage]

public sealed class ClientDetailDto
{
    public ClientDto Client { get; init; } = null!;
    public IReadOnlyList<ClientInteractionDto> Interactions { get; init; } = [];
    public IReadOnlyList<MajorMedicalEnrollmentDto> MajorMedicalEnrollments { get; init; } = [];
    public IReadOnlyList<DrugPlanEnrollmentDto> DrugPlanEnrollments { get; init; } = [];
    public IReadOnlyList<SecondaryEnrollmentDto> SecondaryEnrollments { get; init; } = [];
}
