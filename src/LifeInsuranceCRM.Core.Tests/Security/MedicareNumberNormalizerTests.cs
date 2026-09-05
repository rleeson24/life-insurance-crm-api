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
    [InlineData("1EG4-TE5-MK72", true)]
    [InlineData("1EG4TE5MK72", true)]
    [InlineData("1EG4", false)]
    [InlineData("John Smith", false)]
    public void IsLookupCandidate_RequiresElevenNormalizedCharacters(string input, bool expected)
    {
        Assert.Equal(expected, MedicareNumberNormalizer.IsLookupCandidate(input));
    }
}
