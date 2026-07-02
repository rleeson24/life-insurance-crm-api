namespace LifeInsuranceCRM.Core.Constants;

public static class ClientErrorCodes
{
    public const string ClientIdInvalid = "client.id.invalid";
    public const string ClientNotFound = "client.not_found";
    public const string InteractionIdInvalid = "client.interaction.id.invalid";
    public const string InteractionNotFound = "client.interaction.not_found";
    public const string MedicareEnrollmentIdInvalid = "client.medicare_enrollment.id.invalid";
    public const string MedicareEnrollmentNotFound = "client.medicare_enrollment.not_found";
    public const string SupplementalEnrollmentIdInvalid = "client.supplemental_enrollment.id.invalid";
    public const string SupplementalEnrollmentNotFound = "client.supplemental_enrollment.not_found";
    public const string ActorNotAuthenticated = "actor.not_authenticated";
    public const string FirstNameRequired = "client.first_name.required";
    public const string LastNameRequired = "client.last_name.required";
}
