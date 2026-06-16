using FluentValidation;

namespace Logistics.Application.Routes.Commands.CreateRoute;

public sealed class CreateRouteCommandValidator : AbstractValidator<CreateRouteCommand>
{
    public CreateRouteCommandValidator()
    {
        RuleFor(x => x.OriginWarehouseId).NotEmpty();
        RuleFor(x => x.DestinationWarehouseId).NotEmpty();
        RuleFor(x => x.DestinationWarehouseId)
            .NotEqual(x => x.OriginWarehouseId)
            .WithMessage("Origin and destination must differ.");
        RuleFor(x => x.DistanceKm).GreaterThan(0);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0);
    }
}
