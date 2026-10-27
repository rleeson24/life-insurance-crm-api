using System.Net.Mail;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Validation;

public interface ITenantInputValidator
{
    ProcessResponse<CreateTenantModel> ValidateCreate(CreateTenantModel model);

    ProcessResponse<UpdateTenantModel> ValidateUpdate(UpdateTenantModel model);
}

public sealed class TenantInputValidator : ITenantInputValidator
{
    public ProcessResponse<CreateTenantModel> ValidateCreate(CreateTenantModel model)
    {
        var nameValidation = ValidateName(model.Name, required: true);
        if (nameValidation.IsFailed(out ProcessResponse<CreateTenantModel> failure))
        {
            return failure;
        }

        var contactValidation = ValidateContact(model);
        if (contactValidation.IsFailed(out ProcessResponse<CreateTenantModel> contactFailure))
        {
            return contactFailure;
        }

        return ProcessResponse<CreateTenantModel>.Succeeded(model);
    }

    public ProcessResponse<UpdateTenantModel> ValidateUpdate(UpdateTenantModel model)
    {
        if (model.TenantId == Guid.Empty)
        {
            return ProcessResponse<UpdateTenantModel>.InvalidRequestResponse(
                "Tenant id is required",
                TenantErrorCodes.TenantIdInvalid);
        }

        if (string.IsNullOrWhiteSpace(model.Name) && model.IsActive is null && !HasContactChange(model))
        {
            return ProcessResponse<UpdateTenantModel>.InvalidRequestResponse(
                "Provide a name, contact detail, or active flag to update",
                TenantErrorCodes.NoChanges);
        }

        if (!string.IsNullOrWhiteSpace(model.Name))
        {
            var nameValidation = ValidateName(model.Name, required: true);
            if (nameValidation.IsFailed(out ProcessResponse<UpdateTenantModel> failure))
            {
                return failure;
            }
        }

        var contactValidation = ValidateContact(model);
        if (contactValidation.IsFailed(out ProcessResponse<UpdateTenantModel> contactFailure))
        {
            return contactFailure;
        }

        return ProcessResponse<UpdateTenantModel>.Succeeded(model);
    }

    private static bool HasContactChange(UpdateTenantModel model) =>
        model.AddressLine1 is not null
        || model.AddressLine2 is not null
        || model.City is not null
        || model.State is not null
        || model.PostalCode is not null
        || model.NoticesEmail is not null
        || model.IncidentContact is not null
        || model.IncidentTelephone is not null;

    private static ProcessResponse<bool> ValidateContact(CreateTenantModel model) =>
        ValidateContact(
            model.AddressLine1,
            model.AddressLine2,
            model.City,
            model.State,
            model.PostalCode,
            model.NoticesEmail,
            model.IncidentContact,
            model.IncidentTelephone);

    private static ProcessResponse<bool> ValidateContact(UpdateTenantModel model) =>
        ValidateContact(
            model.AddressLine1,
            model.AddressLine2,
            model.City,
            model.State,
            model.PostalCode,
            model.NoticesEmail,
            model.IncidentContact,
            model.IncidentTelephone);

    private static ProcessResponse<bool> ValidateContact(
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? state,
        string? postalCode,
        string? noticesEmail,
        string? incidentContact,
        string? incidentTelephone)
    {
        var addressLine1Validation = ValidateOptionalText(
            addressLine1,
            200,
            "Address line 1 is too long",
            TenantErrorCodes.AddressLine1TooLong);
        if (!addressLine1Validation.IsSuccess)
        {
            return addressLine1Validation;
        }

        var addressLine2Validation = ValidateOptionalText(
            addressLine2,
            200,
            "Address line 2 is too long",
            TenantErrorCodes.AddressLine2TooLong);
        if (!addressLine2Validation.IsSuccess)
        {
            return addressLine2Validation;
        }

        var cityValidation = ValidateOptionalText(
            city,
            100,
            "City is too long",
            TenantErrorCodes.CityTooLong);
        if (!cityValidation.IsSuccess)
        {
            return cityValidation;
        }

        if (!string.IsNullOrWhiteSpace(state) && state.Trim().Length != 2)
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "State must be a 2-character code",
                TenantErrorCodes.StateInvalid);
        }

        var postalCodeValidation = ValidateOptionalText(
            postalCode,
            10,
            "Postal code is too long",
            TenantErrorCodes.PostalCodeTooLong);
        if (!postalCodeValidation.IsSuccess)
        {
            return postalCodeValidation;
        }

        var noticesEmailValidation = ValidateOptionalText(
            noticesEmail,
            320,
            "Notices email is too long",
            TenantErrorCodes.NoticesEmailTooLong);
        if (!noticesEmailValidation.IsSuccess)
        {
            return noticesEmailValidation;
        }

        if (!string.IsNullOrWhiteSpace(noticesEmail) && !IsValidEmail(noticesEmail.Trim()))
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Notices email is invalid",
                TenantErrorCodes.NoticesEmailInvalid);
        }

        var incidentContactValidation = ValidateOptionalText(
            incidentContact,
            200,
            "Incident contact is too long",
            TenantErrorCodes.IncidentContactTooLong);
        if (!incidentContactValidation.IsSuccess)
        {
            return incidentContactValidation;
        }

        return ValidateOptionalText(
            incidentTelephone,
            32,
            "Incident telephone is too long",
            TenantErrorCodes.IncidentTelephoneTooLong);
    }

    private static ProcessResponse<bool> ValidateName(string? name, bool required)
    {
        if (required && string.IsNullOrWhiteSpace(name))
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Organization name is required",
                TenantErrorCodes.NameRequired);
        }

        if (name is { Length: > 200 })
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Organization name is too long",
                TenantErrorCodes.NameTooLong);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }

    private static ProcessResponse<bool> ValidateOptionalText(
        string? value,
        int maxLength,
        string message,
        string errorCode)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            return ProcessResponse<bool>.InvalidRequestResponse(message, errorCode);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var parsed = new MailAddress(email);
            return string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
