using System.Buffers.Binary;
using System.Security.Cryptography;

namespace LifeInsuranceCRM.Core.Security;

/// <summary>
/// AES-256-GCM envelope payload: format version, key version, nonce, tag, ciphertext.
/// </summary>
public static class EncryptedValue
{
    public const byte CurrentFormatVersion = 1;
    public const int NonceSize = 12;
    public const int TagSize = 16;
    public const int HeaderSize = 1 + sizeof(ushort) + NonceSize + TagSize;

    public static byte[] Encrypt(byte[] key, ushort keyVersion, ReadOnlySpan<byte> plaintext)
    {
        ArgumentNullException.ThrowIfNull(key);

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var payload = new byte[HeaderSize + ciphertext.Length];
        payload[0] = CurrentFormatVersion;
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(1, 2), keyVersion);
        nonce.CopyTo(payload.AsSpan(3, NonceSize));
        tag.CopyTo(payload.AsSpan(3 + NonceSize, TagSize));
        ciphertext.CopyTo(payload.AsSpan(HeaderSize));
        return payload;
    }

    public static byte[] Decrypt(byte[] key, byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(payload);
        ValidateHeader(payload);

        var nonce = payload.AsSpan(3, NonceSize);
        var tag = payload.AsSpan(3 + NonceSize, TagSize);
        var ciphertext = payload.AsSpan(HeaderSize);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    public static ushort ReadKeyVersion(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ValidateHeader(payload);
        return BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(1, 2));
    }

    private static void ValidateHeader(byte[] payload)
    {
        if (payload.Length < HeaderSize)
        {
            throw new CryptographicException("Encrypted field payload is truncated.");
        }

        if (payload[0] != CurrentFormatVersion)
        {
            throw new CryptographicException($"Unsupported field encryption format version {payload[0]}.");
        }
    }
}
