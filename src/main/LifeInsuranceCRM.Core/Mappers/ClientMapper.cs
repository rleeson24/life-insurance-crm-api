using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models.Output;

namespace LifeInsuranceCRM.Core.Mappers;

public interface IClientMapper
{
    ClientDto ToDto(Client client);

    ClientInteractionDto ToDto(ClientInteraction interaction);

    MedicareEnrollmentDto ToDto(MedicareEnrollment enrollment);

    SupplementalEnrollmentDto ToDto(SupplementalEnrollment enrollment);
}

public sealed class ClientMapper : IClientMapper
{
    public ClientDto ToDto(Client client) => new()
    {
        ClientId = client.ClientId,
        FirstName = client.FirstName,
        LastName = client.LastName,
        LegalName = client.LegalName,
        HouseholdName = client.HouseholdName,
        PrimaryPhone = client.PrimaryPhone,
        AddressLine1 = client.AddressLine1,
        AddressLine2 = client.AddressLine2,
        City = client.City,
        State = client.State,
        PostalCode = client.PostalCode,
        EmailAddress = client.EmailAddress,
        DateOfBirth = client.DateOfBirth,
        MedicareNumber = client.MedicareNumber,
        MedicarePartAEffectiveDate = client.MedicarePartAEffectiveDate,
        MedicarePartBEffectiveDate = client.MedicarePartBEffectiveDate,
        IsActive = client.IsActive,
        IsAcaClient = client.IsAcaClient,
        HasContactConsent = client.HasContactConsent,
        Notes = client.Notes,
        CreatedAt = client.CreatedAt,
        UpdatedAt = client.UpdatedAt,
    };

    public ClientInteractionDto ToDto(ClientInteraction interaction) => new()
    {
        ClientInteractionId = interaction.ClientInteractionId,
        ClientId = interaction.ClientId,
        ContactedAt = interaction.ContactedAt,
        Summary = interaction.Summary,
        Notes = interaction.Notes,
        RequiresFollowUp = interaction.RequiresFollowUp,
        CreatedAt = interaction.CreatedAt,
        UpdatedAt = interaction.UpdatedAt,
    };

    public MedicareEnrollmentDto ToDto(MedicareEnrollment enrollment) => new()
    {
        MedicareEnrollmentId = enrollment.MedicareEnrollmentId,
        ClientId = enrollment.ClientId,
        RecordedAt = enrollment.RecordedAt,
        IsActivePlan = enrollment.IsActivePlan,
        PlanName = enrollment.PlanName,
        PrescriptionDrugPlan = enrollment.PrescriptionDrugPlan,
        CoverageStartDate = enrollment.CoverageStartDate,
        IsNewEnrollment = enrollment.IsNewEnrollment,
        HealthReimbursementArrangement = enrollment.HealthReimbursementArrangement,
        EnrollmentPlatform = enrollment.EnrollmentPlatform,
        EnrollmentLocation = enrollment.EnrollmentLocation,
        Notes = enrollment.Notes,
        CreatedAt = enrollment.CreatedAt,
        UpdatedAt = enrollment.UpdatedAt,
    };

    public SupplementalEnrollmentDto ToDto(SupplementalEnrollment enrollment) => new()
    {
        SupplementalEnrollmentId = enrollment.SupplementalEnrollmentId,
        ClientId = enrollment.ClientId,
        RecordedAt = enrollment.RecordedAt,
        PlanOrCarrierName = enrollment.PlanOrCarrierName,
        CoverageStartDate = enrollment.CoverageStartDate,
        IsActiveCoverage = enrollment.IsActiveCoverage,
        Notes = enrollment.Notes,
        CreatedAt = enrollment.CreatedAt,
        UpdatedAt = enrollment.UpdatedAt,
    };
}
