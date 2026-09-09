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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateCreate_WhenLastNameNullOrEmpty_ReturnsInvalidRequest(string? lastName)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { LastName = lastName });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.LastNameRequired, response.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateCreate_WhenFirstNameNullOrEmpty_ReturnsInvalidRequest(string? firstName)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { FirstName = firstName });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.FirstNameRequired, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenEmailInvalid_ReturnsInvalidRequest()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { EmailAddress = "not-an-email" });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.EmailAddressInvalid, response.ErrorCode);
    }

    [Theory]
    [InlineData("F")]
    [InlineData("FLA")]
    [InlineData("F ")]
    public void ValidateCreate_WhenStateIsNotTwoCharacters_ReturnsInvalidRequest(string state)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { State = state });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.StateInvalid, response.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("FL")]
    [InlineData(" FL ")]
    public void ValidateCreate_WhenStateIsOmittedOrExactlyTwoCharacters_ReturnsSuccess(string? state)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { State = state });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenValid_ReturnsSuccess()
    {
        var model = ValidCreate() with
        {
            EmailAddress = "jane@example.com",
            State = "FL",
        };

        var response = _validator.ValidateCreate(model);

        Assert.Equal(UseCaseStatus.Success, response.Status);
        Assert.Equal(model, response.Result);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1)]
    public void ValidateCreate_WhenFirstNameAtOrJustInsideMax_ReturnsSuccess(int length)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { FirstName = new string('A', length) });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenFirstNameJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { FirstName = new string('A', 101) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.FirstNameTooLong, response.ErrorCode);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1)]
    public void ValidateCreate_WhenLastNameAtOrJustInsideMax_ReturnsSuccess(int length)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { LastName = new string('B', length) });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenLastNameJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { LastName = new string('B', 101) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.LastNameTooLong, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenLegalNameAtMax_ReturnsSuccess()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { LegalName = new string('L', 200) });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenLegalNameJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { LegalName = new string('L', 201) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.LegalNameTooLong, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenHouseholdNameAtMax_ReturnsSuccess()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { HouseholdName = new string('H', 200) });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenHouseholdNameJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { HouseholdName = new string('H', 201) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.HouseholdNameTooLong, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenPrimaryPhoneAtMax_ReturnsSuccess()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { PrimaryPhone = new string('5', 32) });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenPrimaryPhoneJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { PrimaryPhone = new string('5', 33) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.PrimaryPhoneTooLong, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenPostalCodeAtMax_ReturnsSuccess()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { PostalCode = "12345-6789" });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenPostalCodeJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { PostalCode = new string('9', 11) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.PostalCodeTooLong, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenEmailAtMaxLength_DoesNotReturnTooLong()
    {
        var email = "user@" + new string('x', 311) + ".com";
        Assert.Equal(320, email.Length);

        var response = _validator.ValidateCreate(ValidCreate() with { EmailAddress = email });

        Assert.NotEqual(ClientErrorCodes.EmailAddressTooLong, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenEmailJustOverMax_ReturnsTooLong()
    {
        var email = "user@" + new string('x', 312) + ".com";
        Assert.Equal(321, email.Length);

        var response = _validator.ValidateCreate(ValidCreate() with { EmailAddress = email });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.EmailAddressTooLong, response.ErrorCode);
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
    public void ValidateCreate_WhenMedicareNumberAtMax_ReturnsSuccess()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { MedicareNumber = new string('1', 32) });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenMedicareNumberJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { MedicareNumber = new string('1', 33) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.MedicareNumberTooLong, response.ErrorCode);
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
    [InlineData("O'Brien")]
    [InlineData("李")]
    [InlineData("José")]
    [InlineData("Robert'); DROP TABLE Clients;--")]
    [InlineData("Jane\tAnn")]
    public void ValidateCreate_WhenNamesContainSpecialOrUnicodeCharacters_ReturnsSuccess(string firstName)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { FirstName = firstName });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Theory]
    [InlineData("1EG4-TE5-MK72")]
    [InlineData(" 1EG4 TE5 MK72 ")]
    public void ValidateCreate_WhenMedicareNumberContainsFormatting_ReturnsSuccess(string medicareNumber)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { MedicareNumber = medicareNumber });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Theory]
    [InlineData("jane@example.com")]
    [InlineData("Jane.O'Brien+tag@example.com")]
    public void ValidateCreate_WhenEmailHasAllowedSpecialCharacters_ReturnsSuccess(string email)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { EmailAddress = email });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Theory]
    [InlineData("jane example.com")]
    [InlineData("jane@")]
    [InlineData("@example.com")]
    public void ValidateCreate_WhenEmailHasInvalidSpecialCharacters_ReturnsInvalid(string email)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { EmailAddress = email });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.EmailAddressInvalid, response.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUpdate_WhenNamesOmitted_ReturnsSuccess(string? name)
    {
        var response = _validator.ValidateUpdate(ValidUpdate() with { FirstName = name, LastName = name });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateUpdate_WhenFirstNameJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateUpdate(ValidUpdate() with { FirstName = new string('A', 101) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.FirstNameTooLong, response.ErrorCode);
    }

    [Fact]
    public void ValidateUpdate_WhenNotesJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateUpdate(ValidUpdate() with { Notes = new string('n', 8001) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.NotesTooLong, response.ErrorCode);
    }

    private static CreateClientModel ValidCreate() => new()
    {
        FirstName = "Jane",
        LastName = "Smith",
    };

    private static UpdateClientModel ValidUpdate() => new()
    {
        ClientId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        FirstName = "Jane",
        LastName = "Smith",
    };
}
