using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Tests.Validation;

public class MajorMedicalEnrollmentInputValidatorTests
{
    private readonly IMajorMedicalEnrollmentInputValidator _validator = new MajorMedicalEnrollmentInputValidator();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidateCreate_WhenOptionalFieldsNullOrEmpty_ReturnsSuccess(string? value)
    {
        var response = _validator.ValidateCreate(ValidCreate() with
        {
            PlanName = value,
            EnrollmentPlatform = value,
            EnrollmentLocation = value,
            Notes = value,
        });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenPlanNameAtMax_ReturnsSuccess()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { PlanName = new string('P', 200) });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenPlanNameJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { PlanName = new string('P', 201) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.PlanNameTooLong, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenEnrollmentPlatformJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { EnrollmentPlatform = new string('E', 201) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.EnrollmentPlatformTooLong, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenEnrollmentLocationJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { EnrollmentLocation = new string('L', 201) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.EnrollmentLocationTooLong, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenNotesJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { Notes = new string('n', 8001) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.NotesTooLong, response.ErrorCode);
    }

    [Theory]
    [InlineData("Humana Gold Plus")]
    [InlineData("プラン")]
    [InlineData("Aetna <MA>")]
    public void ValidateCreate_WhenPlanNameContainsSpecialOrUnicodeCharacters_ReturnsSuccess(string planName)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { PlanName = planName });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateUpdate_WhenNotesAtMax_ReturnsSuccess()
    {
        var response = _validator.ValidateUpdate(ValidUpdate() with { Notes = new string('n', 8000) });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    private static CreateMajorMedicalEnrollmentModel ValidCreate() => new()
    {
        ClientId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        RecordedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        PlanName = "Humana Gold Plus",
    };

    private static UpdateMajorMedicalEnrollmentModel ValidUpdate() => new()
    {
        ClientId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        MajorMedicalEnrollmentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        RecordedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        PlanName = "Humana Gold Plus",
    };
}
