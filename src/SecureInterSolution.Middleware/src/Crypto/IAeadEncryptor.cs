using System;

namespace SecureInterSolution.Middleware.Crypto
{
  public sealed class EncryptedPayload
  {
    public byte[] Nonce { get; }
    public byte[] CipherText { get; }
    public byte[] Tag { get; }

    public EncryptedPayload(byte[] nonce, byte[] cipherText, byte[] tag)
    {
      Nonce = nonce;
      CipherText = cipherText;
      Tag = tag;
    }

    public byte[] ToPackedBytes()
    {
      var buffer = new byte[Nonce.Length + CipherText.Length + Tag.Length];
      Buffer.BlockCopy(Nonce, 0, buffer, 0, Nonce.Length);
      Buffer.BlockCopy(CipherText, 0, buffer, Nonce.Length, CipherText.Length);
      Buffer.BlockCopy(Tag, 0, buffer, Nonce.Length + CipherText.Length, Tag.Length);
      return buffer;
    }

    public static EncryptedPayload FromPackedBytes(ReadOnlySpan<byte> packed, int nonceLength, int tagLength)
    {
      if (packed.Length < nonceLength + tagLength)
      {
        throw new ArgumentException("Packed payload too short for given nonce/tag lengths.");
      }
      int cipherLength = packed.Length - nonceLength - tagLength;
      var nonce = packed.Slice(0, nonceLength).ToArray();
      var cipher = packed.Slice(nonceLength, cipherLength).ToArray();
      var tag = packed.Slice(nonceLength + cipherLength, tagLength).ToArray();
      return new EncryptedPayload(nonce, cipher, tag);
    }
  }

  public interface IAeadEncryptor
  {
    EncryptedPayload Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> key, int nonceLength, int tagLength);
    byte[] Decrypt(in EncryptedPayload payload, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> key);
  }
}

