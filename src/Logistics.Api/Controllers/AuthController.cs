using Logistics.Api.Contracts;
using Logistics.Application.Identity.Commands.ClaimAdmin;
using Logistics.Application.Identity.Commands.Login;
using Logistics.Application.Identity.Commands.RefreshToken;
using Logistics.Application.Identity.Commands.Register;
using Logistics.Application.Identity.Commands.RevokeToken;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        // Sends the Register command via MediatR to the Application layer, the dispatcher finds the assigned handler and executes the logic.
        var result = await sender.Send(
            new RegisterCommand(request.Email, request.Password, request.DisplayName), ct);

        return result.Succeeded
            ? CreatedAtAction(nameof(Register), new { id = result.Value }, new { id = result.Value })
            : BadRequest(result.Error);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password), ct);
        return result.Succeeded ? Ok(ToResponse(result.Value!)) : Unauthorized(result.Error);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RefreshTokenCommand(request.RefreshToken), ct);
        return result.Succeeded ? Ok(ToResponse(result.Value!)) : Unauthorized(result.Error);
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RevokeTokenCommand(request.RefreshToken), ct);
        return result.Succeeded ? NoContent() : BadRequest(result.Error);
    }

    /// <summary>
    /// One-time admin bootstrap, gated by the server-configured secret. Promotes the given account
    /// to Admin only while no administrator exists. Re-login afterwards to get an Admin token.
    /// </summary>
    [HttpPost("claim-admin")]
    public async Task<IActionResult> ClaimAdmin([FromBody] ClaimAdminRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ClaimAdminCommand(request.Email, request.Secret), ct);
        return result.Succeeded ? NoContent() : BadRequest(result.Error);
    }

    private static AuthResponse ToResponse(Application.Identity.AuthResult r)
        => new(r.AccessToken, r.AccessTokenExpiresAtUtc, r.RefreshToken);
}
