using System.Net.Mail;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Validation;

public interface IOrganizationUserInputValidator
{
    ProcessResponse<CreateOrganizationUserModel> ValidateCreate(CreateOrganizationUserModel model);

    ProcessResponse<UpdateOrganizationUserModel> ValidateUpdate(UpdateOrganizationUserModel model);
}

public sealed class OrganizationUserInputValidator : IOrganizationUserInputValidator
{
    private static readonly HashSet<string> AllowedRoles =
    [
        OrganizationRoles.Admin,
        OrganizationRoles.Agent,
        OrganizationRoles.ReadOnly,
    ];

    public ProcessResponse<CreateOrganizationUserModel> ValidateCreate(CreateOrganizationUserModel model)
    {
        if (model.UserId == Guid.Empty)
        {
            return ProcessResponse<CreateOrganizationUserModel>.InvalidRequestResponse(
                "Entra object ID (oid) is required",
                OrganizationUserErrorCodes.UserIdRequired);
        }

        var fieldValidation = ValidateFields(
            model.DisplayName,
            model.EmailAddress,
            model.Role,
            requireDisplayName: true);
        if (fieldValidation.IsFailed(out ProcessResponse<CreateOrganizationUserModel> failure))
        {
            return failure;
        }

        if (model.CreateNewTenant)
        {
            if (string.IsNullOrWhiteSpace(model.NewTenantName))
            {
                return ProcessResponse<CreateOrganizationUserModel>.InvalidRequestResponse(
                    "Organization name is required when creating a new CRM tenant",
                    OrganizationUserErrorCodes.TenantNameRequired);
            }

            if (model.NewTenantName.Trim().Length > 200)
            {
                return ProcessResponse<CreateOrganizationUserModel>.InvalidRequestResponse(
                    "Organization name is too long",
                    OrganizationUserErrorCodes.TenantNameTooLong);
            }
        }

        return ProcessResponse<CreateOrganizationUserModel>.Succeeded(model);
    }

    public ProcessResponse<UpdateOrganizationUserModel> ValidateUpdate(UpdateOrganizationUserModel model)
    {
        if (model.OrganizationUserId == Guid.Empty)
        {
            return ProcessResponse<UpdateOrganizationUserModel>.InvalidRequestResponse(
                "Organization user id is required",
                OrganizationUserErrorCodes.OrganizationUserIdInvalid);
        }

        var fieldValidation = ValidateFields(
            model.DisplayName,
            model.EmailAddress,
            model.Role,
            requireDisplayName: true);
        if (fieldValidation.IsFailed(out ProcessResponse<UpdateOrganizationUserModel> failure))
        {
            return failure;
        }

        return ProcessResponse<UpdateOrganizationUserModel>.Succeeded(model);
    }

    private static ProcessResponse<bool> ValidateFields(
        string? displayName,
        string? emailAddress,
        string? role,
        bool requireDisplayName)
    {
        if (requireDisplayName && string.IsNullOrWhiteSpace(displayName))
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Display name is required",
                OrganizationUserErrorCodes.DisplayNameRequired);
        }

        if (displayName is { Length: > 200 })
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Display name is too long",
                OrganizationUserErrorCodes.DisplayNameTooLong);
        }

        if (emailAddress is { Length: > 320 })
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Email address is too long",
                OrganizationUserErrorCodes.EmailAddressTooLong);
        }

        if (!string.IsNullOrWhiteSpace(emailAddress) && !IsValidEmail(emailAddress))
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Email address is invalid",
                OrganizationUserErrorCodes.EmailAddressInvalid);
        }

        if (string.IsNullOrWhiteSpace(role) || !AllowedRoles.Contains(role))
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Role must be Admin, Agent, or ReadOnly",
                OrganizationUserErrorCodes.RoleInvalid);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
