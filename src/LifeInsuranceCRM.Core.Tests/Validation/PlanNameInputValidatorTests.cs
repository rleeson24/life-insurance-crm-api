using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Tests.Validation;

public class PlanNameInputValidatorTests
{
    private readonly IPlanNameInputValidator _validator = new PlanNameInputValidator();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateCreate_WhenNameMissing_ReturnsInvalidRequest(string? name)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { Name = name });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(PlanNameErrorCodes.NameRequired, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenNameAtMax_ReturnsSuccess()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { Name = new string('A', PlanNameInputValidator.NameMaxLength) });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenNameJustOverMax_ReturnsInvalidRequest()
    {
        var response = _validator.ValidateCreate(ValidCreate() with
        {
            Name = new string('A', PlanNameInputValidator.NameMaxLength + 1),
        });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(PlanNameErrorCodes.NameTooLong, response.ErrorCode);
    }

    [Theory]
    [InlineData(PlanNameInputValidator.MinPlanYear)]
    [InlineData(PlanNameInputValidator.MinPlanYear + 1)]
    [InlineData(PlanNameInputValidator.MaxPlanYear - 1)]
    [InlineData(PlanNameInputValidator.MaxPlanYear)]
    public void ValidateCreate_WhenYearAtOrJustInsideBounds_ReturnsSuccess(short planYear)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { PlanYear = planYear });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Theory]
    [InlineData(PlanNameInputValidator.MinPlanYear - 1)]
    [InlineData(PlanNameInputValidator.MaxPlanYear + 1)]
    [InlineData(short.MinValue)]
    [InlineData(short.MaxValue)]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateCreate_WhenYearOutsideBounds_ReturnsInvalidRequest(short planYear)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { PlanYear = planYear });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(PlanNameErrorCodes.PlanYearInvalid, response.ErrorCode);
    }

    [Theory]
    [InlineData("Humana Gold Plus")]
    [InlineData("O'Brien Rx")]
    [InlineData("プラン")]
    [InlineData("Plan <A&B>")]
    public void ValidateCreate_WhenNameContainsSpecialOrUnicodeCharacters_ReturnsSuccess(string name)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { Name = name });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenValid_ReturnsSuccess()
    {
        var model = ValidCreate();

        var response = _validator.ValidateCreate(model);

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal(model, response.Result);
    }

    [Fact]
    public void ValidateUpdate_WhenIdEmpty_ReturnsInvalidRequest()
    {
        var response = _validator.ValidateUpdate(new UpdatePlanNameModel
        {
            Kind = PlanNameKind.Medicare,
            Name = "Plan",
        });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(PlanNameErrorCodes.IdInvalid, response.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUpdate_WhenNameMissing_ReturnsInvalidRequest(string? name)
    {
        var response = _validator.ValidateUpdate(new UpdatePlanNameModel
        {
            PlanNameId = Guid.NewGuid(),
            Kind = PlanNameKind.Drug,
            Name = name,
        });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(PlanNameErrorCodes.NameRequired, response.ErrorCode);
    }

    [Fact]
    public void ValidateUpdate_WhenNameJustOverMax_ReturnsInvalidRequest()
    {
        var response = _validator.ValidateUpdate(new UpdatePlanNameModel
        {
            PlanNameId = Guid.NewGuid(),
            Kind = PlanNameKind.Secondary,
            Name = new string('A', PlanNameInputValidator.NameMaxLength + 1),
        });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(PlanNameErrorCodes.NameTooLong, response.ErrorCode);
    }

    [Theory]
    [InlineData(PlanNameInputValidator.MinPlanYear, PlanNameInputValidator.MinPlanYear)]
    [InlineData(PlanNameInputValidator.MaxPlanYear, PlanNameInputValidator.MaxPlanYear)]
    [InlineData(2026, 2026)]
    public void ValidateClone_WhenYearsMatch_ReturnsInvalidRequest(short sourceYear, short targetYear)
    {
        var response = _validator.ValidateClone(new ClonePlanNamesModel
        {
            Kind = PlanNameKind.Medicare,
            SourceYear = sourceYear,
            TargetYear = targetYear,
        });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(PlanNameErrorCodes.CloneYearsInvalid, response.ErrorCode);
    }

    [Theory]
    [InlineData(PlanNameInputValidator.MinPlanYear, PlanNameInputValidator.MinPlanYear + 1)]
    [InlineData(PlanNameInputValidator.MaxPlanYear - 1, PlanNameInputValidator.MaxPlanYear)]
    public void ValidateClone_WhenYearsDifferAndInsideBounds_ReturnsSuccess(short sourceYear, short targetYear)
    {
        var response = _validator.ValidateClone(new ClonePlanNamesModel
        {
            Kind = PlanNameKind.Medicare,
            SourceYear = sourceYear,
            TargetYear = targetYear,
        });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Theory]
    [InlineData(short.MinValue, 2026)]
    [InlineData(2026, short.MaxValue)]
    [InlineData(PlanNameInputValidator.MinPlanYear - 1, 2026)]
    [InlineData(2026, PlanNameInputValidator.MaxPlanYear + 1)]
    public void ValidateClone_WhenEitherYearOutsideBounds_ReturnsInvalidRequest(short sourceYear, short targetYear)
    {
        var response = _validator.ValidateClone(new ClonePlanNamesModel
        {
            Kind = PlanNameKind.Medicare,
            SourceYear = sourceYear,
            TargetYear = targetYear,
        });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(PlanNameErrorCodes.PlanYearInvalid, response.ErrorCode);
    }

    [Theory]
    [InlineData(PlanNameInputValidator.MinPlanYear)]
    [InlineData(PlanNameInputValidator.MaxPlanYear)]
    public void ValidateList_WhenYearAtBounds_ReturnsSuccess(short planYear)
    {
        var response = _validator.ValidateList(new ListPlanNamesRequest
        {
            Kind = PlanNameKind.Drug,
            PlanYear = planYear,
        });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Theory]
    [InlineData(PlanNameInputValidator.MinPlanYear - 1)]
    [InlineData(PlanNameInputValidator.MaxPlanYear + 1)]
    [InlineData(short.MinValue)]
    [InlineData(short.MaxValue)]
    public void ValidateList_WhenYearOutsideBounds_ReturnsInvalidRequest(short planYear)
    {
        var response = _validator.ValidateList(new ListPlanNamesRequest
        {
            Kind = PlanNameKind.Secondary,
            PlanYear = planYear,
        });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(PlanNameErrorCodes.PlanYearInvalid, response.ErrorCode);
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

    [Fact]
    public void ValidateLookup_WhenFromEqualsTo_ReturnsSuccess()
    {
        var response = _validator.ValidateLookup(new LookupPlanNamesRequest
        {
            Kind = PlanNameKind.Medicare,
            FromYear = 2026,
            ToYear = 2026,
        });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateLookup_WhenRangeSpansMinToMax_ReturnsSuccess()
    {
        var response = _validator.ValidateLookup(new LookupPlanNamesRequest
        {
            Kind = PlanNameKind.Medicare,
            FromYear = PlanNameInputValidator.MinPlanYear,
            ToYear = PlanNameInputValidator.MaxPlanYear,
        });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateLookup_WhenFromYearBelowMin_ReturnsInvalidRequest()
    {
        var response = _validator.ValidateLookup(new LookupPlanNamesRequest
        {
            Kind = PlanNameKind.Medicare,
            FromYear = PlanNameInputValidator.MinPlanYear - 1,
            ToYear = 2026,
        });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(PlanNameErrorCodes.PlanYearInvalid, response.ErrorCode);
    }

    private static CreatePlanNameModel ValidCreate() => new()
    {
        Kind = PlanNameKind.Medicare,
        PlanYear = 2026,
        Name = "Humana Gold Plus",
    };
}
