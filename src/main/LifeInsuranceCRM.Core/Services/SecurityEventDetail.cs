using LifeInsuranceCRM.Core.Constants;

namespace LifeInsuranceCRM.Core.Services;

public static class SecurityEventDetail
{
    public static string ListPage(int page, int pageSize, int totalCount) =>
        $"page={page};pageSize={pageSize};total={totalCount}";

    public static string PlanYear(short planYear) => $"planYear={planYear}";

    public static string ClientParts(int interactions, int majorMedical, int drug, int secondary) =>
        $"interactions={interactions};majorMedical={majorMedical};drug={drug};secondary={secondary}";

    public static string ClientId(Guid clientId) => $"clientId={clientId:D}";

    public static string Import(
        long? bytes,
        int clients,
        int majorMedical,
        int drug,
        int secondary,
        int interactions,
        int warnings,
        string outcome)
    {
        var parts = new List<string>();
        if (bytes is >= 0)
        {
            parts.Add($"bytes={bytes.Value}");
        }

        parts.Add($"clients={Math.Max(0, clients)}");
        parts.Add($"majorMedical={Math.Max(0, majorMedical)}");
        parts.Add($"drug={Math.Max(0, drug)}");
        parts.Add($"secondary={Math.Max(0, secondary)}");
        parts.Add($"interactions={Math.Max(0, interactions)}");
        parts.Add($"warnings={Math.Max(0, warnings)}");
        parts.Add($"outcome={NormalizeOutcome(outcome)}");
        return string.Join(';', parts);
    }

    public static string? AccessChange(
        string? previousRole,
        string? nextRole,
        bool previousActive,
        bool nextActive)
    {
        var parts = new List<string>();
        if (!string.Equals(previousRole, nextRole, StringComparison.Ordinal))
        {
            parts.Add($"role={Role(previousRole)}->{Role(nextRole)}");
        }

        if (previousActive != nextActive)
        {
            parts.Add($"isActive={Bit(previousActive)}->{Bit(nextActive)}");
        }

        return parts.Count == 0 ? null : string.Join(';', parts);
    }

    public static string CreatedAccess(string? role, bool isActive) =>
        $"created;role={Role(role)};isActive={Bit(isActive)}";

    public static string? TenantChange(bool previousActive, bool nextActive, bool nameChanged)
    {
        var parts = new List<string>();
        if (previousActive != nextActive)
        {
            parts.Add($"isActive={Bit(previousActive)}->{Bit(nextActive)}");
        }

        if (nameChanged)
        {
            parts.Add("nameChanged=true");
        }

        return parts.Count == 0 ? null : string.Join(';', parts);
    }

    private static string NormalizeOutcome(string outcome) => outcome switch
    {
        "inserted" or "empty" or "rejected" or "inProgress" => outcome,
        _ => "unknown",
    };

    private static string Role(string? role) => role switch
    {
        OrganizationRoles.SuperAdmin => OrganizationRoles.SuperAdmin,
        OrganizationRoles.Admin => OrganizationRoles.Admin,
        OrganizationRoles.Agent => OrganizationRoles.Agent,
        OrganizationRoles.ReadOnly => OrganizationRoles.ReadOnly,
        _ => "unknown",
    };

    private static string Bit(bool value) => value ? "true" : "false";
}
