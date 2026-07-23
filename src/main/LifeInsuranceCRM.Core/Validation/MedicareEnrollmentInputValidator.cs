using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Validation;

public interface IMedicareEnrollmentInputValidator
{
    ProcessResponse<CreateMedicareEnrollmentModel> ValidateCreate(CreateMedicareEnrollmentModel model);

    ProcessResponse<UpdateMedicareEnrollmentModel> ValidateUpdate(UpdateMedicareEnrollmentModel model);
}

public sealed class MedicareEnrollmentInputValidator : IMedicareEnrollmentInputValidator
{
    private const int FieldMaxLength = 200;
    private const int NotesMaxLength = 8000;

    public ProcessResponse<CreateMedicareEnrollmentModel> ValidateCreate(CreateMedicareEnrollmentModel model)
    {
        var validation = ValidateFields(
            model.PlanName,
            model.PrescriptionDrugPlan,
            model.HealthReimbursementArrangement,
            model.EnrollmentPlatform,
            model.EnrollmentLocation,
            model.Notes);
        if (validation.IsFailed(out ProcessResponse<CreateMedicareEnrollmentModel> failure))
        {
            return failure;
        }

        return ProcessResponse<CreateMedicareEnrollmentModel>.Succeeded(model);
    }

    public ProcessResponse<UpdateMedicareEnrollmentModel> ValidateUpdate(UpdateMedicareEnrollmentModel model)
    {
        var validation = ValidateFields(
            model.PlanName,
            model.PrescriptionDrugPlan,
            model.HealthReimbursementArrangement,
            model.EnrollmentPlatform,
            model.EnrollmentLocation,
            model.Notes);
        if (validation.IsFailed(out ProcessResponse<UpdateMedicareEnrollmentModel> failure))
        {
            return failure;
        }

        return ProcessResponse<UpdateMedicareEnrollmentModel>.Succeeded(model);
    }

    private ProcessResponse<bool> ValidateFields(
        string? planName,
        string? prescriptionDrugPlan,
        string? healthReimbursementArrangement,
        string? enrollmentPlatform,
        string? enrollmentLocation,
        string? notes)
    {
        if (ExceedsMaxLength(planName, FieldMaxLength))
        {
            return FieldTooLong("Plan name", ClientErrorCodes.PlanNameTooLong, FieldMaxLength);
        }

        if (ExceedsMaxLength(prescriptionDrugPlan, FieldMaxLength))
        {
            return FieldTooLong("Prescription drug plan", ClientErrorCodes.PrescriptionDrugPlanTooLong, FieldMaxLength);
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
