using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Tests.Validation;

public class ClientInputValidatorTests
{
    private readonly IClientInputValidator _validator = new ClientInputValidator();

    [Fact]
    public void ValidateCreate_WhenFirstNameMissing_ReturnsInvalidRequest()
    {
        var model = new CreateClientModel { LastName = "Smith" };

        var response = _validator.ValidateCreate(model);

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.FirstNameRequired, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenEmailInvalid_ReturnsInvalidRequest()
    {
        var model = new CreateClientModel
        {
            FirstName = "Jane",
            LastName = "Smith",
            EmailAddress = "not-an-email",
        };

        var response = _validator.ValidateCreate(model);

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.EmailAddressInvalid, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenStateIsNotTwoCharacters_ReturnsInvalidRequest()
    {
        var model = new CreateClientModel
        {
            FirstName = "Jane",
            LastName = "Smith",
            State = "Florida",
        };

        var response = _validator.ValidateCreate(model);

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.StateInvalid, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenValid_ReturnsSuccess()
    {
        var model = new CreateClientModel
        {
            FirstName = "Jane",
            LastName = "Smith",
            EmailAddress = "jane@example.com",
            State = "FL",
        };

        var response = _validator.ValidateCreate(model);

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal(model, response.Result);
    }
}
