using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Tests.Validation;

public class PlanNameInputValidatorTests
{
    private readonly IPlanNameInputValidator _validator = new PlanNameInputValidator();

    [Fact]
    public void ValidateCreate_WhenNameMissing_ReturnsInvalidRequest()
    {
        var response = _validator.ValidateCreate(new CreatePlanNameModel
        {
            Kind = PlanNameKind.Medicare,
            PlanYear = 2026,
        });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(PlanNameErrorCodes.NameRequired, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenNameTooLong_ReturnsInvalidRequest()
    {
        var response = _validator.ValidateCreate(new CreatePlanNameModel
        {
            Kind = PlanNameKind.Drug,
            PlanYear = 2026,
            Name = new string('A', 201),
        });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(PlanNameErrorCodes.NameTooLong, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenYearOutOfRange_ReturnsInvalidRequest()
    {
        var response = _validator.ValidateCreate(new CreatePlanNameModel
        {
            Kind = PlanNameKind.Secondary,
            PlanYear = 1999,
            Name = "Plan",
        });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(PlanNameErrorCodes.PlanYearInvalid, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenValid_ReturnsSuccess()
    {
        var model = new CreatePlanNameModel
        {
            Kind = PlanNameKind.Medicare,
            PlanYear = 2026,
            Name = "Humana Gold Plus",
        };

        var response = _validator.ValidateCreate(model);

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal(model, response.Result);
    }

    [Fact]
    public void ValidateClone_WhenYearsMatch_ReturnsInvalidRequest()
    {
        var response = _validator.ValidateClone(new ClonePlanNamesModel
        {
            Kind = PlanNameKind.Medicare,
            SourceYear = 2026,
            TargetYear = 2026,
        });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(PlanNameErrorCodes.CloneYearsInvalid, response.ErrorCode);
    }

    [Fact]
    public void ValidateLookup_WhenFromYearAfterToYear_ReturnsInvalidRequest()
    {
        var response = _validator.ValidateLookup(new LookupPlanNamesRequest
        {
            Kind = PlanNameKind.Medicare,
            FromYear = 2026,
            ToYear = 2025,
        });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(PlanNameErrorCodes.YearRangeInvalid, response.ErrorCode);
    }
}
