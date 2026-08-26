using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Tests.Validation;

public class OrganizationUserInputValidatorTests
{
    private readonly IOrganizationUserInputValidator _validator = new OrganizationUserInputValidator();

    [Fact]
    public void ValidateCreate_WhenUserIdMissing_ReturnsInvalidRequest()
    {
        var model = new CreateOrganizationUserModel
        {
            DisplayName = "Jane",
            Role = OrganizationRoles.Agent,
        };

        var response = _validator.ValidateCreate(model);

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(OrganizationUserErrorCodes.UserIdRequired, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenNewTenantNameMissing_ReturnsInvalidRequest()
    {
        var model = new CreateOrganizationUserModel
        {
            UserId = Guid.NewGuid(),
            DisplayName = "Jane",
            Role = OrganizationRoles.Agent,
            CreateNewTenant = true,
        };

        var response = _validator.ValidateCreate(model);

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(OrganizationUserErrorCodes.TenantNameRequired, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenValid_ReturnsSuccess()
    {
        var model = new CreateOrganizationUserModel
        {
            UserId = Guid.NewGuid(),
            DisplayName = "Jane",
            EmailAddress = "jane@example.com",
            Role = OrganizationRoles.Admin,
        };

        var response = _validator.ValidateCreate(model);

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal(model, response.Result);
    }

    [Fact]
    public void ValidateUpdate_WhenRoleInvalid_ReturnsInvalidRequest()
    {
        var model = new UpdateOrganizationUserModel
        {
            OrganizationUserId = Guid.NewGuid(),
            DisplayName = "Jane",
            Role = "Owner",
            IsActive = true,
        };

        var response = _validator.ValidateUpdate(model);

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(OrganizationUserErrorCodes.RoleInvalid, response.ErrorCode);
    }
}
