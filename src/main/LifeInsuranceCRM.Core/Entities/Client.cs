namespace LifeInsuranceCRM.Core.Entities;

public sealed class Client
{
    public Guid ClientId { get; init; }
    public Guid TenantId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? LegalName { get; init; }
    public string? HouseholdName { get; init; }
    public string? PrimaryPhone { get; init; }
    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? EmailAddress { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? MedicareNumber { get; init; }
    public DateOnly? MedicarePartAEffectiveDate { get; init; }
    public DateOnly? MedicarePartBEffectiveDate { get; init; }
    public bool IsActive { get; init; }
    public bool IsAcaClient { get; init; }
    public bool HasContactConsent { get; init; }
    public string? Notes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid CreatedByUserId { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public Guid UpdatedByUserId { get; init; }
}
