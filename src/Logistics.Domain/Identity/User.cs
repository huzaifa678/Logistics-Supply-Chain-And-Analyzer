using Logistics.Domain.Common;

namespace Logistics.Domain.Identity;

/// <summary>
/// An authenticated principal. Stored as (:User {id, email, ...}) in Neo4j.
/// Password is never stored in plain text — only <see cref="PasswordHash"/>.
/// </summary>
public sealed class User : BaseEntity, IAggregateRoot
{
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string DisplayName { get; private set; }
    /// <summary>E.164 phone for login-OTP delivery via SMS. Optional (empty for older accounts).</summary>
    public string Phone { get; private set; }
    public Role Role { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private User(string email, string passwordHash, string displayName, string phone, Role role)
    {
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        Phone = phone;
        Role = role;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static User Create(string email, string passwordHash, string displayName, string phone = "", Role role = Role.Viewer)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("A valid email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        return new User(email.Trim().ToLowerInvariant(), passwordHash, displayName, phone ?? string.Empty, role);
    }

    public static User Rehydrate(string id, string email, string passwordHash, string displayName, string phone, Role role, DateTimeOffset createdAt)
        => new(email, passwordHash, displayName, phone ?? string.Empty, role) { Id = id, CreatedAt = createdAt };

    /// <summary>Reassigns this user's role (RBAC). Performed by an administrator.</summary>
    public void ChangeRole(Role role) => Role = role;
}
