using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class DeleteClientRequest
{
    public Guid ClientId { get; init; }
}
