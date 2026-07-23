using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Validation;

public interface IClientInteractionInputValidator
{
    ProcessResponse<CreateClientInteractionModel> ValidateCreate(CreateClientInteractionModel model);

    ProcessResponse<UpdateClientInteractionModel> ValidateUpdate(UpdateClientInteractionModel model);
}

public sealed class ClientInteractionInputValidator : IClientInteractionInputValidator
{
    private const int SummaryMaxLength = 500;
    private const int NotesMaxLength = 8000;

    public ProcessResponse<CreateClientInteractionModel> ValidateCreate(CreateClientInteractionModel model)
    {
        var validation = ValidateFields(model.Summary, model.Notes, requireSummary: true);
        if (validation.IsFailed(out ProcessResponse<CreateClientInteractionModel> failure))
        {
            return failure;
        }

        return ProcessResponse<CreateClientInteractionModel>.Succeeded(model);
    }

    public ProcessResponse<UpdateClientInteractionModel> ValidateUpdate(UpdateClientInteractionModel model)
    {
        var validation = ValidateFields(model.Summary, model.Notes, requireSummary: false);
        if (validation.IsFailed(out ProcessResponse<UpdateClientInteractionModel> failure))
        {
            return failure;
        }

        return ProcessResponse<UpdateClientInteractionModel>.Succeeded(model);
    }

    private ProcessResponse<bool> ValidateFields(string? summary, string? notes, bool requireSummary)
    {
        if (requireSummary && string.IsNullOrWhiteSpace(summary))
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Summary is required",
                ClientErrorCodes.InteractionSummaryRequired);
        }

        if (!string.IsNullOrEmpty(summary) && summary.Length > SummaryMaxLength)
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                $"Summary must be {SummaryMaxLength} characters or fewer",
                ClientErrorCodes.InteractionSummaryTooLong);
        }

        if (!string.IsNullOrEmpty(notes) && notes.Length > NotesMaxLength)
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                $"Notes must be {NotesMaxLength} characters or fewer",
                ClientErrorCodes.NotesTooLong);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }
}
