namespace LifeInsuranceCRM.Core.Constants;

public static class TenantErrorCodes
{
    public const string ActorNotSuperAdmin = "tenant.actor.not_super_admin";
    public const string TenantNotFound = "tenant.not_found";
    public const string TenantIdInvalid = "tenant.id.invalid";
    public const string NameRequired = "tenant.name.required";
    public const string NameTooLong = "tenant.name.too_long";
    public const string AddressLine1TooLong = "tenant.address_line1.too_long";
    public const string AddressLine2TooLong = "tenant.address_line2.too_long";
    public const string CityTooLong = "tenant.city.too_long";
    public const string StateInvalid = "tenant.state.invalid";
    public const string PostalCodeTooLong = "tenant.postal_code.too_long";
    public const string NoticesEmailTooLong = "tenant.notices_email.too_long";
    public const string NoticesEmailInvalid = "tenant.notices_email.invalid";
    public const string IncidentContactTooLong = "tenant.incident_contact.too_long";
    public const string IncidentTelephoneTooLong = "tenant.incident_telephone.too_long";
    public const string NoChanges = "tenant.no_changes";
}
