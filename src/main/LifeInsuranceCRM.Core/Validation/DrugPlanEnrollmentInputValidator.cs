using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Validation;

public interface IDrugPlanEnrollmentInputValidator
{
    ProcessResponse<CreateDrugPlanEnrollmentModel> ValidateCreate(CreateDrugPlanEnrollmentModel model);

    ProcessResponse<UpdateDrugPlanEnrollmentModel> ValidateUpdate(UpdateDrugPlanEnrollmentModel model);
}

public sealed class DrugPlanEnrollmentInputValidator : IDrugPlanEnrollmentInputValidator
{
    private const int FieldMaxLength = 200;
    private const int NotesMaxLength = 8000;

    public ProcessResponse<CreateDrugPlanEnrollmentModel> ValidateCreate(CreateDrugPlanEnrollmentModel model)
    {
        var validation = ValidateFields(
            model.PlanName,
            model.EnrollmentPlatform,
            model.EnrollmentLocation,
            model.Notes);
        if (validation.IsFailed(out ProcessResponse<CreateDrugPlanEnrollmentModel> failure))
        {
            return failure;
        }

        return ProcessResponse<CreateDrugPlanEnrollmentModel>.Succeeded(model);
    }

    public ProcessResponse<UpdateDrugPlanEnrollmentModel> ValidateUpdate(UpdateDrugPlanEnrollmentModel model)
    {
        var validation = ValidateFields(
            model.PlanName,
            model.EnrollmentPlatform,
            model.EnrollmentLocation,
            model.Notes);
        if (validation.IsFailed(out ProcessResponse<UpdateDrugPlanEnrollmentModel> failure))
        {
            return failure;
        }

        return ProcessResponse<UpdateDrugPlanEnrollmentModel>.Succeeded(model);
    }

    private ProcessResponse<bool> ValidateFields(
        string? planName,
        string? enrollmentPlatform,
        string? enrollmentLocation,
        string? notes)
    {
        if (ExceedsMaxLength(planName, FieldMaxLength))
        {
            return FieldTooLong("Plan name", ClientErrorCodes.DrugPlanPlanNameTooLong, FieldMaxLength);
        }

        if (ExceedsMaxLength(enrollmentPlatform, FieldMaxLength))
        {
            return FieldTooLong("Enrollment platform", ClientErrorCodes.DrugPlanEnrollmentPlatformTooLong, FieldMaxLength);
        }

        if (ExceedsMaxLength(enrollmentLocation, FieldMaxLength))
        {
            return FieldTooLong("Enrollment location", ClientErrorCodes.DrugPlanEnrollmentLocationTooLong, FieldMaxLength);
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
}
