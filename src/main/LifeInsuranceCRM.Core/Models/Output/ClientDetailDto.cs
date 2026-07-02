namespace LifeInsuranceCRM.Core.Models.Output;

public sealed class ClientDetailDto
{
    public ClientDto Client { get; init; } = null!;
    public IReadOnlyList<ClientInteractionDto> Interactions { get; init; } = [];
    public IReadOnlyList<MedicareEnrollmentDto> MedicareEnrollments { get; init; } = [];
    public IReadOnlyList<SupplementalEnrollmentDto> SupplementalEnrollments { get; init; } = [];
}
