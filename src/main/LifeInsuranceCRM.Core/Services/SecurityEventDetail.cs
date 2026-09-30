using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models.Input;

namespace LifeInsuranceCRM.Core.Services;

public static class SecurityEventDetail
{
    public static string ListPage(int page, int pageSize, int totalCount) =>
        $"page={page};pageSize={pageSize};total={totalCount}";

    public static string PlanYear(short planYear) => $"planYear={planYear}";

    public static string ClientParts(int interactions, int majorMedical, int drug, int secondary) =>
        $"interactions={interactions};majorMedical={majorMedical};drug={drug};secondary={secondary}";

    public static string ClientId(Guid clientId) => $"clientId={clientId:D}";

    public static string ClientUpdate(Client? before, UpdateClientModel after)
    {
        if (before is null)
        {
            return "profile=updated";
        }

        var fields = new List<string>();
        AddText(fields, "firstName", before.FirstName, after.FirstName);
        AddText(fields, "lastName", before.LastName, after.LastName);
        AddText(fields, "legalName", before.LegalName, after.LegalName);
        AddText(fields, "householdName", before.HouseholdName, after.HouseholdName);
        AddText(fields, "primaryPhone", before.PrimaryPhone, after.PrimaryPhone);
        AddText(fields, "addressLine1", before.AddressLine1, after.AddressLine1);
        AddText(fields, "addressLine2", before.AddressLine2, after.AddressLine2);
        AddText(fields, "city", before.City, after.City);
        AddText(fields, "state", before.State, after.State);
        AddText(fields, "postalCode", before.PostalCode, after.PostalCode);
        AddText(fields, "emailAddress", before.EmailAddress, after.EmailAddress);
        AddDate(fields, "dateOfBirth", before.DateOfBirth, after.DateOfBirth);
        AddText(fields, "medicareNumber", before.MedicareNumber, after.MedicareNumber);
        AddDate(fields, "medicarePartAEffectiveDate", before.MedicarePartAEffectiveDate, after.MedicarePartAEffectiveDate);
        AddDate(fields, "medicarePartBEffectiveDate", before.MedicarePartBEffectiveDate, after.MedicarePartBEffectiveDate);
        AddBool(fields, "isActive", before.IsActive, after.IsActive);
        AddBool(fields, "isAcaClient", before.IsAcaClient, after.IsAcaClient);
        AddBool(fields, "hasContactConsent", before.HasContactConsent, after.HasContactConsent);
        AddText(fields, "notes", before.Notes, after.Notes);
        return fields.Count == 0 ? "unchanged" : "fields=" + string.Join(',', fields);
    }

    public static string ClientStatus(Client? before, UpdateClientStatusModel after)
    {
        if (before is null)
        {
            return "status=updated";
        }

        var fields = new List<string>();
        AddOptionalBool(fields, "isActive", before.IsActive, after.IsActive);
        AddOptionalBool(fields, "isAcaClient", before.IsAcaClient, after.IsAcaClient);
        AddOptionalBool(fields, "hasContactConsent", before.HasContactConsent, after.HasContactConsent);
        return fields.Count == 0 ? "unchanged" : "fields=" + string.Join(',', fields);
    }

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

    public static string CreatedTenant(bool isActive) =>
        $"created;isActive={Bit(isActive)}";

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

    private static void AddText(List<string> fields, string name, string? before, string? after)
    {
        if (!string.Equals(Normalize(before), Normalize(after), StringComparison.Ordinal))
        {
            fields.Add(name);
        }
    }

    private static void AddDate(List<string> fields, string name, DateOnly? before, DateOnly? after)
    {
        if (before != after)
        {
            fields.Add(name);
        }
    }

    private static void AddBool(List<string> fields, string name, bool before, bool after)
    {
        if (before != after)
        {
            fields.Add($"{name}={Bit(before)}->{Bit(after)}");
        }
    }

    private static void AddOptionalBool(List<string> fields, string name, bool before, bool? after)
    {
        if (after is { } next && before != next)
        {
            fields.Add($"{name}={Bit(before)}->{Bit(next)}");
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
