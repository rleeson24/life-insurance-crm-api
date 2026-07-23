using System.Net.Mail;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Validation;

public interface IClientInputValidator
{
    ProcessResponse<CreateClientModel> ValidateCreate(CreateClientModel model);

    ProcessResponse<UpdateClientModel> ValidateUpdate(UpdateClientModel model);
}

public sealed class ClientInputValidator : IClientInputValidator
{
    private const int NotesMaxLength = 8000;

    public ProcessResponse<CreateClientModel> ValidateCreate(CreateClientModel model)
    {
        var validation = ValidateClientFields(
            model.FirstName,
            model.LastName,
            model.LegalName,
            model.HouseholdName,
            model.PrimaryPhone,
            model.State,
            model.PostalCode,
            model.EmailAddress,
            model.MedicareNumber,
            model.Notes,
            requireNames: true);
        if (validation.IsFailed(out ProcessResponse<CreateClientModel> failure))
        {
            return failure;
        }

        return ProcessResponse<CreateClientModel>.Succeeded(model);
    }

    public ProcessResponse<UpdateClientModel> ValidateUpdate(UpdateClientModel model)
    {
        var validation = ValidateClientFields(
            model.FirstName,
            model.LastName,
            model.LegalName,
            model.HouseholdName,
            model.PrimaryPhone,
            model.State,
            model.PostalCode,
            model.EmailAddress,
            model.MedicareNumber,
            model.Notes,
            requireNames: false);
        if (validation.IsFailed(out ProcessResponse<UpdateClientModel> failure))
        {
            return failure;
        }

        return ProcessResponse<UpdateClientModel>.Succeeded(model);
    }

    private ProcessResponse<bool> ValidateClientFields(
        string? firstName,
        string? lastName,
        string? legalName,
        string? householdName,
        string? primaryPhone,
        string? state,
        string? postalCode,
        string? emailAddress,
        string? medicareNumber,
        string? notes,
        bool requireNames)
    {
        if (requireNames && string.IsNullOrWhiteSpace(firstName))
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "First name is required",
                ClientErrorCodes.FirstNameRequired);
        }

        if (requireNames && string.IsNullOrWhiteSpace(lastName))
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Last name is required",
                ClientErrorCodes.LastNameRequired);
        }

        if (ExceedsMaxLength(firstName, 100))
        {
            return FieldTooLong("First name", ClientErrorCodes.FirstNameTooLong, 100);
        }

        if (ExceedsMaxLength(lastName, 100))
        {
            return FieldTooLong("Last name", ClientErrorCodes.LastNameTooLong, 100);
        }

        if (ExceedsMaxLength(legalName, 200))
        {
            return FieldTooLong("Legal name", ClientErrorCodes.LegalNameTooLong, 200);
        }

        if (ExceedsMaxLength(householdName, 200))
        {
            return FieldTooLong("Household name", ClientErrorCodes.HouseholdNameTooLong, 200);
        }

        if (ExceedsMaxLength(primaryPhone, 32))
        {
            return FieldTooLong("Primary phone", ClientErrorCodes.PrimaryPhoneTooLong, 32);
        }

        if (!string.IsNullOrWhiteSpace(state) && state.Trim().Length != 2)
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "State must be a 2-character code",
                ClientErrorCodes.StateInvalid);
        }

        if (ExceedsMaxLength(postalCode, 10))
        {
            return FieldTooLong("Postal code", ClientErrorCodes.PostalCodeTooLong, 10);
        }

        if (ExceedsMaxLength(emailAddress, 320))
        {
            return FieldTooLong("Email address", ClientErrorCodes.EmailAddressTooLong, 320);
        }

        if (!string.IsNullOrWhiteSpace(emailAddress) && !IsValidEmail(emailAddress))
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Email address is invalid",
                ClientErrorCodes.EmailAddressInvalid);
        }

        if (ExceedsMaxLength(medicareNumber, 32))
        {
            return FieldTooLong("Medicare number", ClientErrorCodes.MedicareNumberTooLong, 32);
        }

        if (ExceedsMaxLength(notes, NotesMaxLength))
        {
            return FieldTooLong("Notes", ClientErrorCodes.NotesTooLong, NotesMaxLength);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }

    private bool ExceedsMaxLength(string? value, int maxLength) =>
        !string.IsNullOrEmpty(value) && value.Length > maxLength;

    private ProcessResponse<bool> FieldTooLong(string fieldName, string errorCode, int maxLength) =>
        ProcessResponse<bool>.InvalidRequestResponse(
            $"{fieldName} must be {maxLength} characters or fewer",
            errorCode);

    private bool IsValidEmail(string email)
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
