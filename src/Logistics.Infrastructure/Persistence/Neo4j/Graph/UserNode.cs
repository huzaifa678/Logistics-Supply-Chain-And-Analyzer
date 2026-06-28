using Logistics.Domain.Identity;

namespace Logistics.Infrastructure.Persistence.Neo4j.Graph;

/// <summary>
/// ORM node model for (:User). Mutable POCO so Neo4jClient can serialize it as query
/// parameters and deserialize it from returned nodes (mapped camelCase ↔ PascalCase).
/// </summary>
public sealed class UserNode
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;

    public static UserNode FromDomain(User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        PasswordHash = u.PasswordHash,
        DisplayName = u.DisplayName,
        Phone = u.Phone,
        Role = u.Role.ToString(),
        CreatedAt = u.CreatedAt.ToString("o")
    };

    public User ToDomain() => User.Rehydrate(
        Id, Email, PasswordHash, DisplayName, Phone ?? string.Empty,
        Enum.Parse<Role>(Role), DateTimeOffset.Parse(CreatedAt));
}
