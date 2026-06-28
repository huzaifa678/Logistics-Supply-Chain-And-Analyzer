using FluentValidation;
using Logistics.Application.Common.Models;
using MediatR;

namespace Logistics.Application.Identity.Commands.VerifyOtp;

public sealed record VerifyOtpCommand(string Email, string Code) : IRequest<Result<AuthResult>>;

public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Matches(@"^\d{4,10}$");
    }
}
