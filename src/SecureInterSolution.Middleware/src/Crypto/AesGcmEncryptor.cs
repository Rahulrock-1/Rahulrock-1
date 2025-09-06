using System;
using System.Security.Cryptography;

namespace SecureInterSolution.Middleware.Crypto
{
  public sealed class AesGcmEncryptor : IAeadEncryptor
  {
    private static byte[] GenerateNonce(int length)
    {
      byte[] nonce = new byte[length];
      RandomNumberGenerator.Fill(nonce);
      return nonce;
    }

    public EncryptedPayload Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> key, int nonceLength, int tagLength)
    {
      byte[] nonce = GenerateNonce(nonceLength);
      byte[] cipher = new byte[plaintext.Length];
      byte[] tag = new byte[tagLength];

      using var aesGcm = new AesGcm(key.ToArray());
      aesGcm.Encrypt(nonce, plaintext, cipher, tag, associatedData);
      return new EncryptedPayload(nonce, cipher, tag);
    }

    public byte[] Decrypt(in EncryptedPayload payload, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> key)
    {
      byte[] plaintext = new byte[payload.CipherText.Length];
      using var aesGcm = new AesGcm(key.ToArray());
      aesGcm.Decrypt(payload.Nonce, payload.CipherText, payload.Tag, plaintext, associatedData);
      return plaintext;
    }
  }
}

