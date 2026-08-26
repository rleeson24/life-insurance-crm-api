namespace LifeInsuranceCRM.Core.Constants;

public static class TenantErrorCodes
{
    public const string ActorNotSuperAdmin = "tenant.actor.not_super_admin";
    public const string TenantNotFound = "tenant.not_found";
    public const string TenantIdInvalid = "tenant.id.invalid";
    public const string NameRequired = "tenant.name.required";
    public const string NameTooLong = "tenant.name.too_long";
    public const string NoChanges = "tenant.no_changes";
}
