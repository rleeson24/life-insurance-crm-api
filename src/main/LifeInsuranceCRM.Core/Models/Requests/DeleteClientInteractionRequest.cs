using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class DeleteClientInteractionRequest
{
    public Guid ClientId { get; init; }
    public Guid ClientInteractionId { get; init; }
}
