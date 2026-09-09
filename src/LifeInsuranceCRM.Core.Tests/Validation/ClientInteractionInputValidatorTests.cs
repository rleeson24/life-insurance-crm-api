using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Tests.Validation;

public class ClientInteractionInputValidatorTests
{
    private readonly IClientInteractionInputValidator _validator = new ClientInteractionInputValidator();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateCreate_WhenSummaryNullOrEmpty_ReturnsInvalidRequest(string? summary)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { Summary = summary });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.InteractionSummaryRequired, response.ErrorCode);
    }

    [Fact]
    public void ValidateCreate_WhenSummaryAtMax_ReturnsSuccess()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { Summary = new string('S', 500) });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateCreate_WhenSummaryJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateCreate(ValidCreate() with { Summary = new string('S', 501) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.InteractionSummaryTooLong, response.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidateCreate_WhenNotesNullOrEmpty_ReturnsSuccess(string? notes)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { Notes = notes });

        Assert.Equal(UseCaseStatus.Success, response.Status);
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
    [InlineData("Called O'Brien")]
    [InlineData("跟进")]
    [InlineData("Notes with <script> & quotes \"'\"")]
    public void ValidateCreate_WhenSummaryContainsSpecialOrUnicodeCharacters_ReturnsSuccess(string summary)
    {
        var response = _validator.ValidateCreate(ValidCreate() with { Summary = summary });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUpdate_WhenSummaryOmitted_ReturnsSuccess(string? summary)
    {
        var response = _validator.ValidateUpdate(ValidUpdate() with { Summary = summary });

        Assert.Equal(UseCaseStatus.Success, response.Status);
    }

    [Fact]
    public void ValidateUpdate_WhenSummaryJustOverMax_ReturnsTooLong()
    {
        var response = _validator.ValidateUpdate(ValidUpdate() with { Summary = new string('S', 501) });

        Assert.Equal(UseCaseStatus.InvalidRequest, response.Status);
        Assert.Equal(ClientErrorCodes.InteractionSummaryTooLong, response.ErrorCode);
    }

    private static CreateClientInteractionModel ValidCreate() => new()
    {
        ClientId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        ContactedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        Summary = "Called client",
    };

    private static UpdateClientInteractionModel ValidUpdate() => new()
    {
        ClientId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        ClientInteractionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        ContactedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        Summary = "Called client",
    };
}
