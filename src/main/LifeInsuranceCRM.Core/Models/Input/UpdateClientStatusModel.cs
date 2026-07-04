using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Input;

[ExcludeFromCodeCoverage]

public sealed record UpdateClientStatusModel
{
    public Guid ClientId { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsAcaClient { get; init; }
    public bool? HasContactConsent { get; init; }
}
