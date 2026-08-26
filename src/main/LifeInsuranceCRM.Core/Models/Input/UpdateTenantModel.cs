using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Input;

[ExcludeFromCodeCoverage]

public sealed record UpdateTenantModel
{
    public Guid TenantId { get; init; }

    public string? Name { get; init; }

    public bool? IsActive { get; init; }
}
