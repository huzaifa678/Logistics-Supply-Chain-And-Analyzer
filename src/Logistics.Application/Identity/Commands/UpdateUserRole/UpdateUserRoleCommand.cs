using FluentValidation;
using Logistics.Application.Common.Models;
using Logistics.Domain.Identity;
using MediatR;

namespace Logistics.Application.Identity.Commands.UpdateUserRole;

/// <summary>Reassigns a user's RBAC role. Authorized to administrators only (enforced at the API).</summary>
public sealed record UpdateUserRoleCommand(string UserId, string Role) : IRequest<Result>;

public sealed class UpdateUserRoleCommandValidator : AbstractValidator<UpdateUserRoleCommand>
{
    public UpdateUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => Enum.TryParse<Role>(r, ignoreCase: true, out _))
            .WithMessage("Role must be one of: Viewer, Operator, Admin.");
    }
}

public sealed class UpdateUserRoleCommandHandler(IUserRepository users)
    : IRequestHandler<UpdateUserRoleCommand, Result>
{
    public async Task<Result> Handle(UpdateUserRoleCommand request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return Result.Failure($"No user found with id '{request.UserId}'.");

        var role = Enum.Parse<Role>(request.Role, ignoreCase: true);
        user.ChangeRole(role);
        await users.UpdateRoleAsync(user.Id, role, ct);

        return Result.Success();
    }
}
