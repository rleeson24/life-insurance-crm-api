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

        if (string.IsNullOrWhiteSpace(model.Name) && model.IsActive is null)
        {
            return ProcessResponse<UpdateTenantModel>.InvalidRequestResponse(
                "Provide a name or active flag to update",
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

        return ProcessResponse<UpdateTenantModel>.Succeeded(model);
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
}
