using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Validation;

public interface ISupplementalEnrollmentInputValidator
{
    ProcessResponse<CreateSupplementalEnrollmentModel> ValidateCreate(CreateSupplementalEnrollmentModel model);

    ProcessResponse<UpdateSupplementalEnrollmentModel> ValidateUpdate(UpdateSupplementalEnrollmentModel model);
}

public sealed class SupplementalEnrollmentInputValidator : ISupplementalEnrollmentInputValidator
{
    private const int PlanOrCarrierNameMaxLength = 200;
    private const int NotesMaxLength = 8000;

    public ProcessResponse<CreateSupplementalEnrollmentModel> ValidateCreate(CreateSupplementalEnrollmentModel model)
    {
        var validation = ValidateFields(model.PlanOrCarrierName, model.Notes);
        if (validation.IsFailed(out ProcessResponse<CreateSupplementalEnrollmentModel> failure))
        {
            return failure;
        }

        return ProcessResponse<CreateSupplementalEnrollmentModel>.Succeeded(model);
    }

    public ProcessResponse<UpdateSupplementalEnrollmentModel> ValidateUpdate(UpdateSupplementalEnrollmentModel model)
    {
        var validation = ValidateFields(model.PlanOrCarrierName, model.Notes);
        if (validation.IsFailed(out ProcessResponse<UpdateSupplementalEnrollmentModel> failure))
        {
            return failure;
        }

        return ProcessResponse<UpdateSupplementalEnrollmentModel>.Succeeded(model);
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
