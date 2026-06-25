using Logistics.Application.Common.Models;
using MediatR;

namespace Logistics.Application.Identity.Queries.ListUsers;

/// <summary>Lists all users for the admin user-management view.</summary>
public sealed record ListUsersQuery : IRequest<Result<IReadOnlyList<UserDto>>>;

public sealed record UserDto(
    string Id,
    string Email,
    string DisplayName,
    string Role,
    DateTimeOffset CreatedAt);

public sealed class ListUsersQueryHandler(IUserRepository users)
    : IRequestHandler<ListUsersQuery, Result<IReadOnlyList<UserDto>>>
{
    public async Task<Result<IReadOnlyList<UserDto>>> Handle(ListUsersQuery request, CancellationToken ct)
    {
        var all = await users.ListAsync(ct);
        var dtos = all
            .OrderBy(u => u.CreatedAt)
            .Select(u => new UserDto(u.Id, u.Email, u.DisplayName, u.Role.ToString(), u.CreatedAt))
            .ToList();

        return Result<IReadOnlyList<UserDto>>.Success(dtos);
    }
}
