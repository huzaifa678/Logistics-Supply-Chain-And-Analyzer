using FluentValidation;
using Logistics.Application.Common.Models;
using Logistics.Domain.Enums;
using MediatR;

namespace Logistics.Application.Shipments.Commands.CreateShipment;

public sealed record CreateShipmentCommand(
    string TrackingNumber,
    string OriginWarehouseId,
    string DestinationWarehouseId,
    double WeightKg,
    TransportMode Mode) : IRequest<Result<string>>;

public sealed class CreateShipmentCommandValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentCommandValidator()
    {
        RuleFor(x => x.TrackingNumber).NotEmpty().MaximumLength(64);
        RuleFor(x => x.OriginWarehouseId).NotEmpty();
        RuleFor(x => x.DestinationWarehouseId).NotEmpty()
            .NotEqual(x => x.OriginWarehouseId).WithMessage("Origin and destination must differ.");
        RuleFor(x => x.WeightKg).GreaterThan(0);
    }
}
