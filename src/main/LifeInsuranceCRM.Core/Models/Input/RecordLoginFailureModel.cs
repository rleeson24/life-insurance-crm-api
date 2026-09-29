using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Input;

[ExcludeFromCodeCoverage]
public sealed record RecordLoginFailureModel
{
    public string? Reason { get; init; }
}
