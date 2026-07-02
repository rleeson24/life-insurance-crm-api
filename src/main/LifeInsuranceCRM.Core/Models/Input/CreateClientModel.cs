namespace LifeInsuranceCRM.Core.Models.Input;

public sealed record CreateClientModel
{
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
    public bool IsActive { get; init; } = true;
    public bool IsAcaClient { get; init; }
    public bool HasContactConsent { get; init; }
    public string? Notes { get; init; }
}
