using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Requests;

[ExcludeFromCodeCoverage]
public sealed class GetAuthSecurityEventRequest
{
    public Guid AuthSecurityEventId { get; init; }
}
