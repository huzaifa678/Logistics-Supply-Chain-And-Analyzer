using Logistics.Application.Common.Models;
using Logistics.Domain.Identity;
using MediatR;

namespace Logistics.Application.Identity.Commands.Register;

public sealed class RegisterCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher) : IRequestHandler<RegisterCommand, Result<string>>
{
    public async Task<Result<string>> Handle(RegisterCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.EmailExistsAsync(email, ct))
            return Result<string>.Failure("An account with this email already exists.");

        var user = User.Create(
            email,
            passwordHasher.Hash(request.Password),
            request.DisplayName,
            request.Phone.Trim(),
            Role.Viewer);

        var id = await users.AddAsync(user, ct);
        return Result<string>.Success(id);
    }
}
