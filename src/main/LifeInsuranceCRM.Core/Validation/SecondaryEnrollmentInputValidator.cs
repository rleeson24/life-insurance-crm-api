using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Validation;

public interface ISecondaryEnrollmentInputValidator
{
    ProcessResponse<CreateSecondaryEnrollmentModel> ValidateCreate(CreateSecondaryEnrollmentModel model);

    ProcessResponse<UpdateSecondaryEnrollmentModel> ValidateUpdate(UpdateSecondaryEnrollmentModel model);
}

public sealed class SecondaryEnrollmentInputValidator : ISecondaryEnrollmentInputValidator
{
    private const int PlanOrCarrierNameMaxLength = 200;
    private const int NotesMaxLength = 8000;

    public ProcessResponse<CreateSecondaryEnrollmentModel> ValidateCreate(CreateSecondaryEnrollmentModel model)
    {
        var validation = ValidateFields(model.PlanOrCarrierName, model.Notes);
        if (validation.IsFailed(out ProcessResponse<CreateSecondaryEnrollmentModel> failure))
        {
            return failure;
        }

        return ProcessResponse<CreateSecondaryEnrollmentModel>.Succeeded(model);
    }

    public ProcessResponse<UpdateSecondaryEnrollmentModel> ValidateUpdate(UpdateSecondaryEnrollmentModel model)
    {
        var validation = ValidateFields(model.PlanOrCarrierName, model.Notes);
        if (validation.IsFailed(out ProcessResponse<UpdateSecondaryEnrollmentModel> failure))
        {
            return failure;
        }

        return ProcessResponse<UpdateSecondaryEnrollmentModel>.Succeeded(model);
    }

    private ProcessResponse<bool> ValidateFields(string? planOrCarrierName, string? notes)
    {
        if (ExceedsMaxLength(planOrCarrierName, PlanOrCarrierNameMaxLength))
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                $"Plan or carrier name must be {PlanOrCarrierNameMaxLength} characters or fewer",
                ClientErrorCodes.PlanOrCarrierNameTooLong);
        }

        if (ExceedsMaxLength(notes, NotesMaxLength))
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                $"Notes must be {NotesMaxLength} characters or fewer",
                ClientErrorCodes.NotesTooLong);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }

    private bool ExceedsMaxLength(string? value, int maxLength) =>
        !string.IsNullOrEmpty(value) && value.Length > maxLength;
}
