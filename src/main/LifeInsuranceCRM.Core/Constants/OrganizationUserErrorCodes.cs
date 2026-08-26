namespace LifeInsuranceCRM.Core.Constants;

public static class OrganizationUserErrorCodes
{
    public const string ActorNotAuthenticated = "actor.not_authenticated";
    public const string ActorNotAdmin = "organization_user.actor.not_admin";
    public const string TenantIdRequired = "organization_user.tenant_id.required";
    public const string TenantNotFound = "organization_user.tenant_not_found";
    public const string LastSuperAdmin = "organization_user.last_super_admin";
    public const string SuperAdminRoleLocked = "organization_user.super_admin_role_locked";
    public const string UserIdInvalid = "organization_user.user_id.invalid";
    public const string UserIdRequired = "organization_user.user_id.required";
    public const string UserAlreadyExists = "organization_user.already_exists";
    public const string UserNotFound = "organization_user.not_found";
    public const string OrganizationUserIdInvalid = "organization_user.id.invalid";
    public const string RoleInvalid = "organization_user.role.invalid";
    public const string DisplayNameRequired = "organization_user.display_name.required";
    public const string DisplayNameTooLong = "organization_user.display_name.too_long";
    public const string EmailAddressTooLong = "organization_user.email_address.too_long";
    public const string EmailAddressInvalid = "organization_user.email_address.invalid";
    public const string LastAdmin = "organization_user.last_admin";
}
