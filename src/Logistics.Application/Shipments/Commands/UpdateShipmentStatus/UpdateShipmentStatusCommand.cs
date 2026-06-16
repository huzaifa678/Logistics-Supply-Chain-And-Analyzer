using Logistics.Application.Common.Interfaces;
using Logistics.Application.Common.Models;
using Logistics.Domain.Exceptions;
using MediatR;

namespace Logistics.Application.Shipments.Commands.UpdateShipmentStatus;

public enum ShipmentTransition { Dispatch, Delay, Deliver, Cancel }

public sealed record UpdateShipmentStatusCommand(
    string ShipmentId,
    ShipmentTransition Transition,
    DateTimeOffset? EstimatedArrival = null,
    string? Reason = null) : IRequest<Result>;

public sealed class UpdateShipmentStatusCommandHandler(
    IShipmentRepository shipments,
    IDomainEventQueue eventQueue) : IRequestHandler<UpdateShipmentStatusCommand, Result>
{
    public async Task<Result> Handle(UpdateShipmentStatusCommand request, CancellationToken ct)
    {
        var shipment = await shipments.GetByIdAsync(request.ShipmentId, ct)
            ?? throw new DomainException($"Shipment '{request.ShipmentId}' was not found.");

        // The domain entity enforces legal status transitions; invalid ones throw.
        switch (request.Transition)
        {
            case ShipmentTransition.Dispatch:
                shipment.Dispatch(request.EstimatedArrival ?? DateTimeOffset.UtcNow.AddDays(1));
                break;
            case ShipmentTransition.Delay:
                shipment.MarkDelayed(request.Reason ?? "Unspecified delay.");
                break;
            case ShipmentTransition.Deliver:
                shipment.Deliver();
                break;
            case ShipmentTransition.Cancel:
                shipment.Cancel();
                break;
        }

        await shipments.UpdateAsync(shipment, ct);

        // Hand domain events off to the background worker, then clear them from the aggregate.
        foreach (var domainEvent in shipment.DomainEvents)
            await eventQueue.EnqueueAsync(domainEvent, ct);
        shipment.ClearEvents();

        return Result.Success();
    }
}
