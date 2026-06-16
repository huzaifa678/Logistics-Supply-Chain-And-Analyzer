using System.Security.Cryptography;
using System.Text;
using Logistics.Application.Identity;

namespace Logistics.Infrastructure.Identity;

/// <summary>
/// Generates 256-bit URL-safe random refresh tokens and stores only their SHA-256 hash,
/// so a database leak does not expose usable tokens.
/// </summary>
public sealed class SecureTokenGenerator : ISecureTokenGenerator
{
    public string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
