namespace LifeInsuranceCRM.Core.Security;

public static class MedicareNumberNormalizer
{
    public const int BeneficiaryIdentifierLength = 11;

    /// <summary>
    /// Uppercases and strips non-alphanumeric characters so formatted and unformatted MBIs match.
    /// </summary>
    public static string? Normalize(string? medicareNumber)
    {
        if (string.IsNullOrWhiteSpace(medicareNumber))
        {
            return null;
        }

        Span<char> buffer = stackalloc char[medicareNumber.Length];
        var length = 0;
        foreach (var character in medicareNumber)
        {
            if (!char.IsAsciiLetterOrDigit(character))
            {
                continue;
            }

            buffer[length++] = char.ToUpperInvariant(character);
        }

        return length == 0 ? null : buffer[..length].ToString();
    }

    /// <summary>
    /// True when the search term normalizes to a full 11-character Medicare Beneficiary Identifier.
    /// </summary>
    public static bool IsLookupCandidate(string? searchTerm)
    {
        var normalized = Normalize(searchTerm);
        return normalized is { Length: BeneficiaryIdentifierLength };
    }
}
