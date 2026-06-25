using Logistics.Api.Contracts;
using Logistics.Api.Extensions;
using Logistics.Application.Identity.Commands.UpdateUserRole;
using Logistics.Application.Identity.Queries.ListUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.RequireAdmin)] // user administration is Admin-only
public sealed class UsersController(ISender sender) : ControllerBase
{
    /// <summary>All users (admin view).</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await sender.Send(new ListUsersQuery(), ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(result.Error);
    }

    /// <summary>Reassign a user's role.</summary>
    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateRole(
        string id, [FromBody] UpdateUserRoleRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateUserRoleCommand(id, request.Role), ct);
        return result.Succeeded ? NoContent() : BadRequest(result.Error);
    }
}
