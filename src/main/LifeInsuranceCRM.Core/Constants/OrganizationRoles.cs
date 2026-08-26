namespace LifeInsuranceCRM.Core.Constants;

public static class OrganizationRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Agent = "Agent";
    public const string ReadOnly = "ReadOnly";

    public static bool IsSuperAdmin(string? role) =>
        string.Equals(role, SuperAdmin, StringComparison.OrdinalIgnoreCase);

    public static bool CanManageOrganizationUsers(string? role) =>
        IsSuperAdmin(role) || string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase);
}

