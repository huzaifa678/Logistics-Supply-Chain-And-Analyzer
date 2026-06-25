namespace Logistics.Api.Contracts;

/// <summary>Body for PUT /api/users/{id}/role — the new role name (Viewer | Operator | Admin).</summary>
public sealed record UpdateUserRoleRequest(string Role);
