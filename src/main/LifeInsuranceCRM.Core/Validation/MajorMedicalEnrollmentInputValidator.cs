using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Validation;

public interface IMajorMedicalEnrollmentInputValidator
{
    ProcessResponse<CreateMajorMedicalEnrollmentModel> ValidateCreate(CreateMajorMedicalEnrollmentModel model);

    ProcessResponse<UpdateMajorMedicalEnrollmentModel> ValidateUpdate(UpdateMajorMedicalEnrollmentModel model);
}

public sealed class MajorMedicalEnrollmentInputValidator : IMajorMedicalEnrollmentInputValidator
{
    private const int FieldMaxLength = 200;
    private const int NotesMaxLength = 8000;

    public ProcessResponse<CreateMajorMedicalEnrollmentModel> ValidateCreate(CreateMajorMedicalEnrollmentModel model)
    {
        var validation = ValidateFields(
            model.PlanName,
            model.HealthReimbursementArrangement,
            model.EnrollmentPlatform,
            model.EnrollmentLocation,
            model.Notes);
        if (validation.IsFailed(out ProcessResponse<CreateMajorMedicalEnrollmentModel> failure))
        {
            return failure;
        }

        return ProcessResponse<CreateMajorMedicalEnrollmentModel>.Succeeded(model);
    }

    public ProcessResponse<UpdateMajorMedicalEnrollmentModel> ValidateUpdate(UpdateMajorMedicalEnrollmentModel model)
    {
        var validation = ValidateFields(
            model.PlanName,
            model.HealthReimbursementArrangement,
            model.EnrollmentPlatform,
            model.EnrollmentLocation,
            model.Notes);
        if (validation.IsFailed(out ProcessResponse<UpdateMajorMedicalEnrollmentModel> failure))
        {
            return failure;
        }

        return ProcessResponse<UpdateMajorMedicalEnrollmentModel>.Succeeded(model);
    }

    private ProcessResponse<bool> ValidateFields(
        string? planName,
        string? healthReimbursementArrangement,
        string? enrollmentPlatform,
        string? enrollmentLocation,
        string? notes)
    {
        if (ExceedsMaxLength(planName, FieldMaxLength))
        {
            return FieldTooLong("Plan name", ClientErrorCodes.PlanNameTooLong, FieldMaxLength);
        }

        if (ExceedsMaxLength(healthReimbursementArrangement, FieldMaxLength))
        {
            return FieldTooLong(
                "Health reimbursement arrangement",
                ClientErrorCodes.HealthReimbursementArrangementTooLong,
                FieldMaxLength);
        }

        if (ExceedsMaxLength(enrollmentPlatform, FieldMaxLength))
        {
            return FieldTooLong("Enrollment platform", ClientErrorCodes.EnrollmentPlatformTooLong, FieldMaxLength);
        }

        if (ExceedsMaxLength(enrollmentLocation, FieldMaxLength))
        {
            return FieldTooLong("Enrollment location", ClientErrorCodes.EnrollmentLocationTooLong, FieldMaxLength);
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
