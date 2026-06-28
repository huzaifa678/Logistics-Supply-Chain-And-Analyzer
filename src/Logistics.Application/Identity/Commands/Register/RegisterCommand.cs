using FluentValidation;
using Logistics.Application.Common.Models;
using MediatR;

namespace Logistics.Application.Identity.Commands.Register;

public sealed record RegisterCommand(string Email, string Password, string DisplayName, string Phone)
    : IRequest<Result<string>>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(120);
        // E.164 — required so the login OTP can be delivered by SMS as well as email.
        RuleFor(x => x.Phone).NotEmpty()
            .Matches(@"^\+[1-9]\d{1,14}$")
            .WithMessage("Phone must be in E.164 format, e.g. +15551234567.");
    }
}
