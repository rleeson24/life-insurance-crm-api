using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Tests.Validation;

public class TenantInputValidatorTests
{
    private readonly ITenantInputValidator _validator = new TenantInputValidator();

    [Fact]
    public void ValidateCreate_WhenNameMissing_ReturnsInvalidRequest()
    {
        var response = _validator.ValidateCreate(new CreateTenantModel());

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(TenantErrorCodes.NameRequired, response.ErrorCode);
    }

    [Fact]
    public void ValidateUpdate_WhenNoChanges_ReturnsInvalidRequest()
    {
        var response = _validator.ValidateUpdate(new UpdateTenantModel { TenantId = Guid.NewGuid() });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(TenantErrorCodes.NoChanges, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenValid_ReturnsSuccess()
    {
        var model = new CreateTenantModel { Name = "North Agency" };

        var response = _validator.ValidateCreate(model);

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal(model, response.Result);
    }
}
