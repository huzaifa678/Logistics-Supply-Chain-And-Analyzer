using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Logistics.Application.Common.Models;
using Logistics.Domain.Identity;
using MediatR;
using Microsoft.Extensions.Options;

namespace Logistics.Application.Identity.Commands.ClaimAdmin;

/// <summary>
/// One-time admin bootstrap: promotes the account with the given email to Admin, gated by the
/// server-configured <see cref="AuthSettings.BootstrapSecret"/>. Only valid while no Admin exists.
/// </summary>
public sealed record ClaimAdminCommand(string Email, string Secret) : IRequest<Result>;

public sealed class ClaimAdminCommandValidator : AbstractValidator<ClaimAdminCommand>
{
    public ClaimAdminCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Secret).NotEmpty();
    }
}

public sealed class ClaimAdminCommandHandler(
    IUserRepository users,
    IOptions<AuthSettings> options) : IRequestHandler<ClaimAdminCommand, Result>
{
    private readonly AuthSettings _settings = options.Value;

    public async Task<Result> Handle(ClaimAdminCommand request, CancellationToken ct)
    {
        // Disabled entirely unless a bootstrap secret is configured.
        if (string.IsNullOrWhiteSpace(_settings.BootstrapSecret))
            return Result.Failure("Admin bootstrap is disabled.");

        if (!SecretsMatch(request.Secret, _settings.BootstrapSecret))
            return Result.Failure("Invalid bootstrap secret.");

        var all = await users.ListAsync(ct);

        // One-shot: once any administrator exists, this path is closed for good.
        if (all.Any(u => u.Role == Role.Admin))
            return Result.Failure("An administrator already exists; ask an admin to grant the role.");

        var email = request.Email.Trim().ToLowerInvariant();
        var user = all.FirstOrDefault(u => u.Email == email);
        if (user is null)
            return Result.Failure("No account found for that email.");

        await users.UpdateRoleAsync(user.Id, Role.Admin, ct);
        return Result.Success();
    }

    /// <summary>Constant-time comparison so the secret can't be probed by timing.</summary>
    private static bool SecretsMatch(string provided, string configured) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(configured));
}
