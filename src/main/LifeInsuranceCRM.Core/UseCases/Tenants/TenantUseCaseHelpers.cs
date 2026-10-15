using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Tenants;

internal static class TenantUseCaseHelpers
{
    public static ProcessResponse<bool> ValidateSuperAdmin(
        IActorTracker actorTracker,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        var validation = clientUseCaseHelpers.ValidateActor(actorTracker);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        if (!OrganizationRoles.IsSuperAdmin(actorTracker.Role))
        {
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.Forbidden,
                "SuperAdmin role is required",
                TenantErrorCodes.ActorNotSuperAdmin);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }

    public static TenantContactFields NormalizeContact(CreateTenantModel model) =>
        new(
            TrimToNull(model.AddressLine1),
            TrimToNull(model.AddressLine2),
            TrimToNull(model.City),
            NormalizeState(model.State),
            TrimToNull(model.PostalCode),
            TrimToNull(model.NoticesEmail),
            TrimToNull(model.IncidentContact),
            TrimToNull(model.IncidentTelephone));

    public static TenantContactFields MergeContact(UpdateTenantModel model, TenantDto existing) =>
        new(
            Resolve(model.AddressLine1, existing.AddressLine1),
            Resolve(model.AddressLine2, existing.AddressLine2),
            Resolve(model.City, existing.City),
            model.State is null ? NormalizeState(existing.State) : NormalizeState(model.State),
            Resolve(model.PostalCode, existing.PostalCode),
            Resolve(model.NoticesEmail, existing.NoticesEmail),
            Resolve(model.IncidentContact, existing.IncidentContact),
            Resolve(model.IncidentTelephone, existing.IncidentTelephone));

    public static IReadOnlyList<string> ChangedContactFields(TenantDto existing, TenantContactFields next)
    {
        var fields = new List<string>();
        Add(fields, "addressLine1", existing.AddressLine1, next.AddressLine1);
        Add(fields, "addressLine2", existing.AddressLine2, next.AddressLine2);
        Add(fields, "city", existing.City, next.City);
        Add(fields, "state", NormalizeState(existing.State), next.State);
        Add(fields, "postalCode", existing.PostalCode, next.PostalCode);
        Add(fields, "noticesEmail", existing.NoticesEmail, next.NoticesEmail);
        Add(fields, "incidentContact", existing.IncidentContact, next.IncidentContact);
        Add(fields, "incidentTelephone", existing.IncidentTelephone, next.IncidentTelephone);
        return fields;
    }

    private static void Add(List<string> fields, string name, string? before, string? after)
    {
        if (!string.Equals(TrimToNull(before), TrimToNull(after), StringComparison.Ordinal))
        {
            fields.Add(name);
        }
    }

    private static string? Resolve(string? incoming, string? existing) =>
        incoming is null ? TrimToNull(existing) : TrimToNull(incoming);

    private static string? NormalizeState(string? state)
    {
        var trimmed = TrimToNull(state);
        return trimmed?.ToUpperInvariant();
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
