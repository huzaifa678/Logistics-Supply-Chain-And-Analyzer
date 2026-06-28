namespace Logistics.Application.Identity;

/// <summary>
/// Bound from the "Auth:Otp" configuration section. Controls the login one-time-password step.
/// Disable (Enabled=false) in dev/test to log in with just email + password.
/// </summary>
public sealed class OtpSettings
{
    public const string SectionName = "Auth:Otp";

    /// <summary>When true, every password login must be followed by an OTP verification.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Number of digits in the generated code.</summary>
    public int Length { get; set; } = 6;

    /// <summary>How long a code stays valid after issue.</summary>
    public int TtlSeconds { get; set; } = 300;

    public TimeSpan Ttl => TimeSpan.FromSeconds(TtlSeconds);
}
