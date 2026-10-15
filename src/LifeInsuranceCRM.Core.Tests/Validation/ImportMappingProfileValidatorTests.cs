using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Validation;

namespace LifeInsuranceCRM.Core.Tests.Validation;

public class ImportMappingProfileValidatorTests
{
    [Fact]
    public void Validate_AcceptsColumnNamesWithoutClientValues()
    {
        var response = ImportMappingProfileValidator.Validate(new SaveImportMappingProfileModel
        {
            Name = "Acme",
            Shape = "flat",
            Tables = new Dictionary<string, string?> { ["rows"] = "Sheet1" },
            Columns = new Dictionary<string, Dictionary<string, string?>?>
            {
                ["rows"] = new Dictionary<string, string?>
                {
                    ["firstName"] = "First Name",
                    ["lastName"] = "Last Name",
                    ["phone"] = " ",
                },
            },
        });

        Assert.True(response.IsSuccess);
        var profile = response.Result!;
        Assert.Equal("Acme", profile.Name);
        Assert.Equal("First Name", profile.Dto.Columns["rows"]["firstName"]);
        Assert.False(profile.Dto.Columns["rows"].ContainsKey("phone"));
        Assert.DoesNotContain("Jane", profile.DefinitionJson, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RejectsAnUnknownShape()
    {
        var response = ImportMappingProfileValidator.Validate(new SaveImportMappingProfileModel
        {
            Name = "Acme",
            Shape = "freeform",
        });

        Assert.False(response.IsSuccess);
        Assert.Equal(LifeInsuranceCRM.Core.Constants.ImportErrorCodes.ProfileInvalid, response.ErrorCode);
    }

    [Fact]
    public void ReadDefinition_RoundTripsTheSavedLayout()
    {
        var saved = ImportMappingProfileValidator.Validate(new SaveImportMappingProfileModel
        {
            Name = "Acme",
            Shape = "ledger",
            Tables = new Dictionary<string, string?> { ["rows"] = "Coverage" },
            Columns = new Dictionary<string, Dictionary<string, string?>?>
            {
                ["rows"] = new Dictionary<string, string?>
                {
                    ["firstName"] = "First Name",
                    ["coverageKind"] = "Coverage Type",
                },
            },
        });

        var dto = ImportMappingProfileValidator.ReadDefinition(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "Acme",
            saved.Result!.DefinitionJson);

        Assert.Equal("ledger", dto.Shape);
        Assert.Equal("Coverage", dto.Tables["rows"]);
        Assert.Equal("Coverage Type", dto.Columns["rows"]["coverageKind"]);
        Assert.Empty(dto.Columns["clients"]);
    }
}
