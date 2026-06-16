using FluentValidation;
using Logistics.Application.Common.Models;
using MediatR;

namespace Logistics.Application.Identity.Commands.Register;

public sealed record RegisterCommand(string Email, string Password, string DisplayName)
    : IRequest<Result<string>>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(120);
    }
}
