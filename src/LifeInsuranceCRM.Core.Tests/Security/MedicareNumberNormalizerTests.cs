using LifeInsuranceCRM.Core.Security;

namespace LifeInsuranceCRM.Core.Tests.Security;

public class MedicareNumberNormalizerTests
{
    [Theory]
    [InlineData("1EG4-TE5-MK72", "1EG4TE5MK72")]
    [InlineData("1eg4te5mk72", "1EG4TE5MK72")]
    [InlineData(" 1EG4 TE5 MK72 ", "1EG4TE5MK72")]
    public void Normalize_StripsFormattingAndUppercases(string input, string expected)
    {
        Assert.Equal(expected, MedicareNumberNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("---")]
    public void Normalize_WhenMissing_ReturnsNull(string? input)
    {
        Assert.Null(MedicareNumberNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("1EG4TE5MK7")]
    [InlineData("1EG4TE5MK72X")]
    public void IsLookupCandidate_WhenNormalizedLengthIsNotEleven_ReturnsFalse(string input)
    {
        Assert.False(MedicareNumberNormalizer.IsLookupCandidate(input));
    }

    [Fact]
    public void IsLookupCandidate_WhenNormalizedLengthIsEleven_ReturnsTrue()
    {
        Assert.True(MedicareNumberNormalizer.IsLookupCandidate("1EG4-TE5-MK72"));
    }

    [Theory]
    [InlineData("***")]
    [InlineData("李李李")]
    [InlineData("!!!@@@")]
    public void Normalize_WhenOnlyNonAlphanumeric_ReturnsNull(string input)
    {
        Assert.Null(MedicareNumberNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_WhenUnicodeNoiseAroundMbi_KeepsAlphanumeric()
    {
        Assert.Equal("1EG4TE5MK72", MedicareNumberNormalizer.Normalize("李1EG4-TE5-MK72李"));
    }

    [Fact]
    public void IsLookupCandidate_WhenWhitespaceAroundElevenCharacters_ReturnsTrue()
    {
        Assert.True(MedicareNumberNormalizer.IsLookupCandidate("  1EG4TE5MK72  "));
    }
}
