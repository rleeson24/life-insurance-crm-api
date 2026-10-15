using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models;

[ExcludeFromCodeCoverage]

public sealed record TenantContactFields(
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? PostalCode,
    string? NoticesEmail,
    string? IncidentContact,
    string? IncidentTelephone);
