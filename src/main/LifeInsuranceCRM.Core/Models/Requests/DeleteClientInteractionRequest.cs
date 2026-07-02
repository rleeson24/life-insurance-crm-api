namespace LifeInsuranceCRM.Core.Models.Requests;

public sealed class DeleteClientInteractionRequest
{
    public Guid ClientId { get; init; }
    public Guid ClientInteractionId { get; init; }
}
