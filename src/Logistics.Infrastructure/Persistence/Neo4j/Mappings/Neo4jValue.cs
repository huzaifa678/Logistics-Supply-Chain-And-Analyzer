using Neo4j.Driver;

namespace Logistics.Infrastructure.Persistence.Neo4j.Mappings;

/// <summary>
/// Small conversion helpers shared by the DAO mappers. Keeps null-handling and
/// datetime parsing in one place (DRY) instead of scattered across repositories.
/// </summary>
internal static class Neo4jValue
{
    public static DateTimeOffset? DateTimeOrNull(IRecord record, string key)
    {
        var raw = record[key].As<string?>();
        return string.IsNullOrEmpty(raw) ? null : DateTimeOffset.Parse(raw);
    }

    public static string Iso(DateTimeOffset value) => value.ToString("o");

    public static string? IsoOrNull(DateTimeOffset? value) => value?.ToString("o");
}
