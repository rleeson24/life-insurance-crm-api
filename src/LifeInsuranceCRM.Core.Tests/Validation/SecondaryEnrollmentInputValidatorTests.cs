using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Tests.Validation;

public class SecondaryEnrollmentInputValidatorTests
{
    private readonly ISecondaryEnrollmentInputValidator _validator = new SecondaryEnrollmentInputValidator();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidateCreate_WhenOptionalFieldsNullOrEmpty_ReturnsSuccess(string? value)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { PlanOrCarrierName = value, Notes = value });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenPlanOrCarrierNameAtMax_ReturnsSuccess()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { PlanOrCarrierName = new string('P', 200) });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenPlanOrCarrierNameJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { PlanOrCarrierName = new string('P', 201) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.PlanOrCarrierNameTooLong, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenNotesAtMax_ReturnsSuccess()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { Notes = new string('n', 8000) });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenNotesJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { Notes = new string('n', 8001) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.NotesTooLong, response.ErrorCode);
    }

    [Theory]
    [InlineData("Aflac")]
    [InlineData("O'Brien Mutual")]
    [InlineData("互惠")]
    [InlineData("Carrier \"A&B\"")]
    public void ValidateCreate_WhenPlanOrCarrierNameContainsSpecialOrUnicodeCharacters_ReturnsSuccess(string name)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { PlanOrCarrierName = name });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateUpdate_WhenPlanOrCarrierNameJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateUpdate(ValidUpdate() with { PlanOrCarrierName = new string('P', 201) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.PlanOrCarrierNameTooLong, response.ErrorCode);
    }

    private static CreateSecondaryEnrollmentModel ValidCreate() => new()
    {
        ClientId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        RecordedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        PlanOrCarrierName = "Aflac",
    };

    private static UpdateSecondaryEnrollmentModel ValidUpdate() => new()
    {
        ClientId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        SecondaryEnrollmentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        RecordedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        PlanOrCarrierName = "Aflac",
    };
}
