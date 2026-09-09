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
    public void ValidateCreate_WhenSuperAdminRole_ReturnsInvalidRequest()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { Role = OrganizationRoles.SuperAdmin });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(OrganizationUserErrorCodes.RoleInvalid, response.ErrorCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Owner")]
    [InlineData("admin")]
    [InlineData("AGENT")]
    public void ValidateCreate_WhenRoleEmptyUnknownOrWrongCase_ReturnsInvalidRequest(string role)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { Role = role });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(OrganizationUserErrorCodes.RoleInvalid, response.ErrorCode);
    }

    [Theory]
    [InlineData(OrganizationRoles.Admin)]
    [InlineData(OrganizationRoles.Agent)]
    [InlineData(OrganizationRoles.ReadOnly)]
    public void ValidateCreate_WhenAssignableRole_ReturnsSuccess(string role)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { Role = role });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateCreate_WhenDisplayNameNullOrEmpty_ReturnsInvalidRequest(string? displayName)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { DisplayName = displayName });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(OrganizationUserErrorCodes.DisplayNameRequired, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenDisplayNameAtMax_ReturnsSuccess()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { DisplayName = new string('D', 200) });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenDisplayNameJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { DisplayName = new string('D', 201) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(OrganizationUserErrorCodes.DisplayNameTooLong, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenEmailJustOverMax_ReturnsTooLong()
    {
        var email = "user@" + new string('x', 312) + ".com";
        Assert.Equal(321, email.Length);

        var response = _validator.ValidateCreate(ValidCreate() with { EmailAddress = email });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(OrganizationUserErrorCodes.EmailAddressTooLong, response.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateCreate_WhenEmailNullOrEmpty_ReturnsSuccess(string? email)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { EmailAddress = email });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenEmailInvalid_ReturnsInvalidRequest()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { EmailAddress = "not-an-email" });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(OrganizationUserErrorCodes.EmailAddressInvalid, response.ErrorCode);
    }

    [Theory]
    [InlineData("O'Brien")]
    [InlineData("李四")]
    [InlineData("Jane <script>")]
    public void ValidateCreate_WhenDisplayNameContainsSpecialOrUnicodeCharacters_ReturnsSuccess(string displayName)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { DisplayName = displayName });

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
    public void ValidateUpdate_WhenOrganizationUserIdEmpty_ReturnsInvalidRequest()
    {
        var response = _validator.ValidateUpdate(new UpdateOrganizationUserModel
        {
            DisplayName = "Jane",
            Role = OrganizationRoles.Agent,
        });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(OrganizationUserErrorCodes.OrganizationUserIdInvalid, response.ErrorCode);
    }

    [Fact]
    public void ValidateUpdate_WhenRoleInvalid_ReturnsInvalidRequest()
    {
        var response = _validator.ValidateUpdate(ValidUpdate() with { Role = "Owner" });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(OrganizationUserErrorCodes.RoleInvalid, response.ErrorCode);
    }

    [Fact]
    public void ValidateUpdate_WhenSuperAdminRole_ReturnsSuccess()
    {
        var response = _validator.ValidateUpdate(ValidUpdate() with { Role = OrganizationRoles.SuperAdmin });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateUpdate_WhenDisplayNameJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateUpdate(ValidUpdate() with { DisplayName = new string('D', 201) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(OrganizationUserErrorCodes.DisplayNameTooLong, response.ErrorCode);
    }

    private static CreateOrganizationUserModel ValidCreate() => new()
    {
        UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        DisplayName = "Jane",
        EmailAddress = "jane@example.com",
        Role = OrganizationRoles.Admin,
    };

    private static UpdateOrganizationUserModel ValidUpdate() => new()
    {
        OrganizationUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        DisplayName = "Jane",
        Role = OrganizationRoles.Agent,
        IsActive = true,
    };
}
