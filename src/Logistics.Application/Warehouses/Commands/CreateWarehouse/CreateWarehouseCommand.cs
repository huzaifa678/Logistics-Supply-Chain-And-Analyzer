using FluentValidation;
using Logistics.Application.Common.Interfaces;
using Logistics.Application.Common.Models;
using Logistics.Domain.Entities;
using Logistics.Domain.ValueObjects;
using MediatR;

namespace Logistics.Application.Warehouses.Commands.CreateWarehouse;

/// <summary>Write operation: create a warehouse node in the supply-chain graph.</summary>
public sealed record CreateWarehouseCommand(
    string Name,
    double Latitude,
    double Longitude,
    int CapacityUnits) : IRequest<Result<string>>;

public sealed class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.CapacityUnits).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateWarehouseCommandHandler(
    IWarehouseRepository warehouses,
    IDomainEventQueue eventQueue) : IRequestHandler<CreateWarehouseCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateWarehouseCommand request, CancellationToken ct)
    {
        var warehouse = Warehouse.Create(
            request.Name,
            new GeoLocation(request.Latitude, request.Longitude),
            request.CapacityUnits);

        var id = await warehouses.AddAsync(warehouse, ct);

        // Hand domain events off to the background worker, then clear them from the aggregate.
        foreach (var domainEvent in warehouse.DomainEvents)
            await eventQueue.EnqueueAsync(domainEvent, ct);
        warehouse.ClearEvents();

        return Result<string>.Success(id);
    }
}
