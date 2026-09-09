using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Output;

[ExcludeFromCodeCoverage]

public sealed class BookOfBusinessRowDto
{
    public Guid ClientId { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? LegalName { get; init; }

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

    public bool HasContactConsent { get; init; }

    public string? Notes { get; init; }

    public string? MedicarePlanName { get; init; }

    public DateOnly? MedicareCoverageStartDate { get; init; }

    public string? DrugPlanName { get; init; }

    public DateOnly? DrugCoverageStartDate { get; init; }

    public string? SecondaryPlanName { get; init; }

    public DateOnly? SecondaryCoverageStartDate { get; init; }
}
