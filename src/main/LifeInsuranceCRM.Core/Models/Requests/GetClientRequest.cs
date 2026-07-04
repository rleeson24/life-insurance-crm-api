using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class GetClientRequest
{
    public Guid ClientId { get; init; }
}
