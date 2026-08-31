using LifeInsuranceCRM.Core.Constants;

namespace LifeInsuranceCRM.Core.Models.Import;

public sealed class MappedAccessImport
{
    public IReadOnlyList<MappedImportClient> Clients { get; init; } = [];

    public IReadOnlyList<MappedImportMajorMedicalEnrollment> MajorMedicalEnrollments { get; init; } = [];

    public IReadOnlyList<MappedImportDrugPlanEnrollment> DrugPlanEnrollments { get; init; } = [];

    public IReadOnlyList<MappedImportSecondaryEnrollment> SecondaryEnrollments { get; init; } = [];

    public IReadOnlyList<MappedImportInteraction> Interactions { get; init; } = [];

    public IReadOnlyList<MappedImportPlanName> PlanNames { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed class MappedImportClient
{
    public long AccessClientId { get; init; }

    public Guid ClientId { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

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
}

public sealed class MappedImportMajorMedicalEnrollment
{
    public Guid MajorMedicalEnrollmentId { get; init; }

    public Guid ClientId { get; init; }

    public DateTimeOffset RecordedAt { get; init; }

    public bool IsActivePlan { get; init; }

    public string? PlanName { get; init; }

    public DateOnly? CoverageStartDate { get; init; }

    public bool IsNewEnrollment { get; init; }

    public bool HealthReimbursementArrangement { get; init; }

    public string? EnrollmentPlatform { get; init; }

    public string? EnrollmentLocation { get; init; }

    public string? Notes { get; init; }
}

public sealed class MappedImportDrugPlanEnrollment
{
    public Guid DrugPlanEnrollmentId { get; init; }

    public Guid ClientId { get; init; }

    public DateTimeOffset RecordedAt { get; init; }

    public bool IsActivePlan { get; init; }

    public string? PlanName { get; init; }

    public DateOnly? CoverageStartDate { get; init; }

    public bool IsNewEnrollment { get; init; }

    public bool HealthReimbursementArrangement { get; init; }

    public string? EnrollmentPlatform { get; init; }

    public string? EnrollmentLocation { get; init; }

    public string? Notes { get; init; }
}

public sealed class MappedImportSecondaryEnrollment
{
    public Guid SecondaryEnrollmentId { get; init; }

    public Guid ClientId { get; init; }

    public DateTimeOffset RecordedAt { get; init; }

    public string? PlanOrCarrierName { get; init; }

    public DateOnly? CoverageStartDate { get; init; }

    public bool IsActiveCoverage { get; init; }

    public string? Notes { get; init; }
}

public sealed class MappedImportInteraction
{
    public Guid ClientInteractionId { get; init; }

    public Guid ClientId { get; init; }

    public DateTimeOffset ContactedAt { get; init; }

    public string? Summary { get; init; }

    public string? Notes { get; init; }

    public bool RequiresFollowUp { get; init; }
}

public sealed class MappedImportPlanName
{
    public PlanNameKind Kind { get; init; }

    public short PlanYear { get; init; }

    public string Name { get; init; } = string.Empty;
}

public sealed class AccessImportPersistResult
{
    public bool TenantAlreadyHasClients { get; init; }

    public bool LockNotAcquired { get; init; }

    public int MedicarePlanNamesInserted { get; init; }

    public int DrugPlanNamesInserted { get; init; }

    public int SecondaryPlanNamesInserted { get; init; }
}
