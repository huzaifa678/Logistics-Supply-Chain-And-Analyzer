namespace Logistics.Api.Contracts;

public sealed record RegisterRequest(string Email, string Password, string DisplayName, string Phone);
public sealed record LoginRequest(string Email, string Password);
public sealed record VerifyOtpRequest(string Email, string Code);
public sealed record RefreshRequest(string RefreshToken);
public sealed record RevokeRequest(string RefreshToken);
public sealed record ClaimAdminRequest(string Email, string Secret);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken);

/// <summary>
/// Login result. When <see cref="OtpRequired"/> is true, <see cref="Tokens"/> is null and the
/// client must POST the emailed/texted code to /api/auth/verify-otp to complete sign-in.
/// </summary>
public sealed record LoginResponse(bool OtpRequired, AuthResponse? Tokens);
