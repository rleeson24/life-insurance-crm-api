using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Input;

[ExcludeFromCodeCoverage]

public sealed record UpdateTenantModel
{
    public Guid TenantId { get; init; }

    public string? Name { get; init; }

    public bool? IsActive { get; init; }

    public string? AddressLine1 { get; init; }

    public string? AddressLine2 { get; init; }

    public string? City { get; init; }

    public string? State { get; init; }

    public string? PostalCode { get; init; }

    public string? NoticesEmail { get; init; }

    public string? IncidentContact { get; init; }

    public string? IncidentTelephone { get; init; }
}
