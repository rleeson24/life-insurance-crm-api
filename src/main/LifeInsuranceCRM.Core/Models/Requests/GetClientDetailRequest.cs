using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]

public sealed class GetClientDetailRequest
{
    public Guid ClientId { get; init; }
}
