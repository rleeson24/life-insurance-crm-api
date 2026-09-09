using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Tests.Validation;

public class TenantInputValidatorTests
{
    private readonly ITenantInputValidator _validator = new TenantInputValidator();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateCreate_WhenNameNullOrEmpty_ReturnsInvalidRequest(string? name)
    {
        var response = _validator.ValidateCreate(new CreateTenantModel { Name = name });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(TenantErrorCodes.NameRequired, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenNameAtMax_ReturnsSuccess()
    {
        var response = _validator.ValidateCreate(new CreateTenantModel { Name = new string('N', 200) });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenNameJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateCreate(new CreateTenantModel { Name = new string('N', 201) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(TenantErrorCodes.NameTooLong, response.ErrorCode);
    }

    [Theory]
    [InlineData("North Agency")]
    [InlineData("O'Brien & Sons")]
    [InlineData("北区代理")]
    [InlineData("Acme <script>alert(1)</script>")]
    public void ValidateCreate_WhenNameContainsSpecialOrUnicodeCharacters_ReturnsSuccess(string name)
    {
        var response = _validator.ValidateCreate(new CreateTenantModel { Name = name });

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal(name, response.Result!.Name);
    }

    [Fact]
    public void ValidateCreate_WhenValid_ReturnsSuccess()
    {
        var model = new CreateTenantModel { Name = "North Agency" };

        var response = _validator.ValidateCreate(model);

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal(model, response.Result);
    }

    [Fact]
    public void ValidateUpdate_WhenTenantIdEmpty_ReturnsInvalidRequest()
    {
        var response = _validator.ValidateUpdate(new UpdateTenantModel { Name = "North Agency" });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(TenantErrorCodes.TenantIdInvalid, response.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUpdate_WhenNoChanges_ReturnsInvalidRequest(string? name)
    {
        var response = _validator.ValidateUpdate(new UpdateTenantModel
        {
            TenantId = Guid.NewGuid(),
            Name = name,
        });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(TenantErrorCodes.NoChanges, response.ErrorCode);
    }

    [Fact]
    public void ValidateUpdate_WhenWhitespaceNameAndIsActiveSet_ReturnsSuccess()
    {
        var response = _validator.ValidateUpdate(new UpdateTenantModel
        {
            TenantId = Guid.NewGuid(),
            Name = "   ",
            IsActive = true,
        });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateUpdate_WhenNameJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateUpdate(new UpdateTenantModel
        {
            TenantId = Guid.NewGuid(),
            Name = new string('N', 201),
        });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(TenantErrorCodes.NameTooLong, response.ErrorCode);
    }

    [Fact]
    public void ValidateUpdate_WhenOnlyIsActiveProvided_ReturnsSuccess()
    {
        var response = _validator.ValidateUpdate(new UpdateTenantModel
        {
            TenantId = Guid.NewGuid(),
            IsActive = false,
        });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }
}
