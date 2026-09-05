namespace LifeInsuranceCRM.Core.Abstractions.Security;

public interface IFieldEncryptionService
{
    byte[]? Encrypt(string? plaintext);

    string? Decrypt(byte[]? ciphertext);

    byte[]? EncryptDateOnly(DateOnly? value);

    DateOnly? DecryptDateOnly(byte[]? ciphertext);

    /// <summary>
    /// Keyed HMAC of the normalized Medicare number for equality lookup without storing plaintext.
    /// </summary>
    byte[]? ComputeMedicareNumberBlindIndex(string? medicareNumber);
}
