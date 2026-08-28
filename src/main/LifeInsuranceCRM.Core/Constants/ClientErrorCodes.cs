namespace LifeInsuranceCRM.Core.Constants;

public static class ClientErrorCodes
{
    public const string ClientIdInvalid = "client.id.invalid";
    public const string ClientNotFound = "client.not_found";
    public const string InteractionIdInvalid = "client.interaction.id.invalid";
    public const string InteractionNotFound = "client.interaction.not_found";
    public const string MajorMedicalEnrollmentIdInvalid = "client.major_medical_enrollment.id.invalid";
    public const string MajorMedicalEnrollmentNotFound = "client.major_medical_enrollment.not_found";
    public const string DrugPlanEnrollmentIdInvalid = "client.drug_plan_enrollment.id.invalid";
    public const string DrugPlanEnrollmentNotFound = "client.drug_plan_enrollment.not_found";
    public const string SecondaryEnrollmentIdInvalid = "client.secondary_enrollment.id.invalid";
    public const string SecondaryEnrollmentNotFound = "client.secondary_enrollment.not_found";
    public const string ActorNotAuthenticated = "actor.not_authenticated";
    public const string FirstNameRequired = "client.first_name.required";
    public const string LastNameRequired = "client.last_name.required";
    public const string FirstNameTooLong = "client.first_name.too_long";
    public const string LastNameTooLong = "client.last_name.too_long";
    public const string LegalNameTooLong = "client.legal_name.too_long";
    public const string HouseholdNameTooLong = "client.household_name.too_long";
    public const string PrimaryPhoneTooLong = "client.primary_phone.too_long";
    public const string StateInvalid = "client.state.invalid";
    public const string PostalCodeTooLong = "client.postal_code.too_long";
    public const string EmailAddressTooLong = "client.email_address.too_long";
    public const string EmailAddressInvalid = "client.email_address.invalid";
    public const string MedicareNumberTooLong = "client.medicare_number.too_long";
    public const string NotesTooLong = "client.notes.too_long";
    public const string InteractionSummaryRequired = "client.interaction.summary.required";
    public const string InteractionSummaryTooLong = "client.interaction.summary.too_long";
    public const string PlanNameTooLong = "client.major_medical_enrollment.plan_name.too_long";
    public const string EnrollmentPlatformTooLong = "client.major_medical_enrollment.platform.too_long";
    public const string EnrollmentLocationTooLong = "client.major_medical_enrollment.location.too_long";
    public const string DrugPlanPlanNameTooLong = "client.drug_plan_enrollment.plan_name.too_long";
    public const string DrugPlanEnrollmentPlatformTooLong = "client.drug_plan_enrollment.platform.too_long";
    public const string DrugPlanEnrollmentLocationTooLong = "client.drug_plan_enrollment.location.too_long";
    public const string PlanOrCarrierNameTooLong = "client.secondary_enrollment.plan_or_carrier_name.too_long";
}
