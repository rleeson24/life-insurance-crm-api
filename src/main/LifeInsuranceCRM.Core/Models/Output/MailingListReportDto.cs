using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Output;

[ExcludeFromCodeCoverage]

public sealed class MailingListRowDto
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

    public bool HasContactConsent { get; init; }

    public string? MedicarePlanName { get; init; }

    public DateOnly? MedicareCoverageStartDate { get; init; }

    public string? EnrollmentLocation { get; init; }

    public string? EnrollmentPlatform { get; init; }
}

[ExcludeFromCodeCoverage]

public sealed class MailingListReportDto
{
    public IReadOnlyList<MailingListRowDto> Items { get; init; } = [];
}
