using System;
using System.Collections.Generic;

namespace SecureInterSolution.Middleware.Options
{
  /// <summary>
  /// Options for configuring encrypted inter-solution communication.
  /// </summary>
  public sealed class SecureCommunicationOptions
  {
    /// <summary>
    /// Mapping of key identifier to raw AES key bytes (16, 24, or 32 bytes for AES-128/192/256).
    /// </summary>
    public IDictionary<string, byte[]> KeyIdToAesKey { get; } = new Dictionary<string, byte[]>(StringComparer.Ordinal);

    /// <summary>
    /// Default key identifier to use when none is explicitly provided.
    /// </summary>
    public string DefaultKeyId { get; set; } = "default";

    /// <summary>
    /// Identifier of this solution (used for headers like X-Solution-From).
    /// </summary>
    public string ThisSolutionId { get; set; } = "unknown";

    /// <summary>
    /// Header name that signals encryption is applied.
    /// </summary>
    public string EncryptedFlagHeader { get; set; } = "X-Encrypted";

    /// <summary>
    /// Header that carries the key identifier.
    /// </summary>
    public string KeyIdHeader { get; set; } = "X-Key-Id";

    /// <summary>
    /// Header with requester solution id.
    /// </summary>
    public string FromSolutionHeader { get; set; } = "X-Solution-From";

    /// <summary>
    /// Header with target solution id.
    /// </summary>
    public string ToSolutionHeader { get; set; } = "X-Solution-To";

    /// <summary>
    /// Header to preserve the original content type.
    /// </summary>
    public string OriginalContentTypeHeader { get; set; } = "X-Original-Content-Type";

    /// <summary>
    /// Whether responses should be encrypted when requests are encrypted.
    /// </summary>
    public bool MirrorEncryptResponse { get; set; } = true;

    /// <summary>
    /// AES-GCM nonce length in bytes. 12 is the recommended size.
    /// </summary>
    public int NonceLengthBytes { get; set; } = 12;

    /// <summary>
    /// AES-GCM authentication tag length in bytes. 16 is the recommended size.
    /// </summary>
    public int TagLengthBytes { get; set; } = 16;

    /// <summary>
    /// Maximum allowed processing duration for a single request before returning 504.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
  }
}

