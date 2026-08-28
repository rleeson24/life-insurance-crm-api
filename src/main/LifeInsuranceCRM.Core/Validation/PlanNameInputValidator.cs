using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Validation;

public interface IPlanNameInputValidator
{
    ProcessResponse<CreatePlanNameModel> ValidateCreate(CreatePlanNameModel model);

    ProcessResponse<UpdatePlanNameModel> ValidateUpdate(UpdatePlanNameModel model);

    ProcessResponse<ClonePlanNamesModel> ValidateClone(ClonePlanNamesModel model);

    ProcessResponse<ListPlanNamesRequest> ValidateList(ListPlanNamesRequest request);

    ProcessResponse<LookupPlanNamesRequest> ValidateLookup(LookupPlanNamesRequest request);
}

public sealed class PlanNameInputValidator : IPlanNameInputValidator
{
    public const short MinPlanYear = 2000;
    public const short MaxPlanYear = 2100;
    public const int NameMaxLength = 200;

    public ProcessResponse<CreatePlanNameModel> ValidateCreate(CreatePlanNameModel model)
    {
        var yearValidation = ValidateYear(model.PlanYear);
        if (yearValidation.IsFailed(out ProcessResponse<CreatePlanNameModel> yearFailure))
        {
            return yearFailure;
        }

        var nameValidation = ValidateName(model.Name, required: true);
        if (nameValidation.IsFailed(out ProcessResponse<CreatePlanNameModel> nameFailure))
        {
            return nameFailure;
        }

        return ProcessResponse<CreatePlanNameModel>.Succeeded(model);
    }

    public ProcessResponse<UpdatePlanNameModel> ValidateUpdate(UpdatePlanNameModel model)
    {
        if (model.PlanNameId == Guid.Empty)
        {
            return ProcessResponse<UpdatePlanNameModel>.InvalidRequestResponse(
                "Plan name id is required",
                PlanNameErrorCodes.IdInvalid);
        }

        var nameValidation = ValidateName(model.Name, required: true);
        if (nameValidation.IsFailed(out ProcessResponse<UpdatePlanNameModel> nameFailure))
        {
            return nameFailure;
        }

        return ProcessResponse<UpdatePlanNameModel>.Succeeded(model);
    }

    public ProcessResponse<ClonePlanNamesModel> ValidateClone(ClonePlanNamesModel model)
    {
        var sourceValidation = ValidateYear(model.SourceYear);
        if (sourceValidation.IsFailed(out ProcessResponse<ClonePlanNamesModel> sourceFailure))
        {
            return sourceFailure;
        }

        var targetValidation = ValidateYear(model.TargetYear);
        if (targetValidation.IsFailed(out ProcessResponse<ClonePlanNamesModel> targetFailure))
        {
            return targetFailure;
        }

        if (model.SourceYear == model.TargetYear)
        {
            return ProcessResponse<ClonePlanNamesModel>.InvalidRequestResponse(
                "Source year and target year must be different",
                PlanNameErrorCodes.CloneYearsInvalid);
        }

        return ProcessResponse<ClonePlanNamesModel>.Succeeded(model);
    }

    public ProcessResponse<ListPlanNamesRequest> ValidateList(ListPlanNamesRequest request)
    {
        var yearValidation = ValidateYear(request.PlanYear);
        if (yearValidation.IsFailed(out ProcessResponse<ListPlanNamesRequest> yearFailure))
        {
            return yearFailure;
        }

        return ProcessResponse<ListPlanNamesRequest>.Succeeded(request);
    }

    public ProcessResponse<LookupPlanNamesRequest> ValidateLookup(LookupPlanNamesRequest request)
    {
        var fromValidation = ValidateYear(request.FromYear);
        if (fromValidation.IsFailed(out ProcessResponse<LookupPlanNamesRequest> fromFailure))
        {
            return fromFailure;
        }

        var toValidation = ValidateYear(request.ToYear);
        if (toValidation.IsFailed(out ProcessResponse<LookupPlanNamesRequest> toFailure))
        {
            return toFailure;
        }

        if (request.FromYear > request.ToYear)
        {
            return ProcessResponse<LookupPlanNamesRequest>.InvalidRequestResponse(
                "From year must be less than or equal to to year",
                PlanNameErrorCodes.YearRangeInvalid);
        }

        return ProcessResponse<LookupPlanNamesRequest>.Succeeded(request);
    }

    private static ProcessResponse<bool> ValidateYear(short planYear)
    {
        if (planYear < MinPlanYear || planYear > MaxPlanYear)
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                $"Plan year must be between {MinPlanYear} and {MaxPlanYear}",
                PlanNameErrorCodes.PlanYearInvalid);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }

    private static ProcessResponse<bool> ValidateName(string? name, bool required)
    {
        if (required && string.IsNullOrWhiteSpace(name))
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Plan name is required",
                PlanNameErrorCodes.NameRequired);
        }

        if (name is { Length: > NameMaxLength })
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                $"Plan name must be {NameMaxLength} characters or fewer",
                PlanNameErrorCodes.NameTooLong);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }
}
