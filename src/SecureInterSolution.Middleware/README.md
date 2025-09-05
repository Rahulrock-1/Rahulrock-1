# SecureInterSolution.Middleware

Encrypted inter-solution communication for ASP.NET Core using AES-GCM.

- Request/response body encryption with AEAD (AES-GCM)
- Authenticated Associated Data (AAD) binds messages to sender/receiver and key id
- 30s timeout enforced on both server middleware and client handler (configurable)

## Install & Register

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSecureCommunication(options =>
{
  options.ThisSolutionId = "SolutionA";
  options.DefaultKeyId = "k1";
  options.KeyIdToAesKey["k1"] = Convert.FromBase64String("qUeH3oPKY9l8q8c0g4Y0w3c3T2v3J1x1vGz1wyf3xJ4="); // 32 bytes
  options.RequestTimeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();
app.UseSecureCommunication();
app.MapControllers();
app.Run();
```

## Client Usage

```csharp
builder.Services.AddHttpClient("EncryptedToB")
  .AddEncryptedHandler(targetSolutionId: "SolutionB", keyId: "k1");
```

Now every outgoing request via this client is encrypted. The server middleware will decrypt incoming requests that have header `X-Encrypted: 1` and re-encrypt responses back.

## Headers

- X-Encrypted: "1" when content is encrypted
- X-Key-Id: key id used (maps to AES key)
- X-Solution-From: sender solution id
- X-Solution-To: target solution id
- X-Original-Content-Type: preserved original content type

## Notes

- AES key sizes supported: 16/24/32 bytes. Store keys securely.
- AAD structure: `{keyId}|{from}|{to}` ensures ciphertext is bound to context.
- Timeout returns 504 on server; client throws TimeoutException.

