using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class ListClientInteractionsRequest
{
    public Guid ClientId { get; init; }
}