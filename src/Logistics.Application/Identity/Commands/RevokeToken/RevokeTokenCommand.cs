using FluentValidation;
using Logistics.Application.Common.Models;
using MediatR;

namespace Logistics.Application.Identity.Commands.RevokeToken;

/// <summary>Logout: revoke a refresh token so it can no longer be exchanged.</summary>
public sealed record RevokeTokenCommand(string RefreshToken) : IRequest<Result>;

public sealed class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}
