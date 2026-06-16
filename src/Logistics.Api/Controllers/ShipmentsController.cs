using Logistics.Api.Contracts;
using Logistics.Application.Shipments.Commands.CreateShipment;
using Logistics.Application.Shipments.Commands.UpdateShipmentStatus;
using Logistics.Application.Shipments.Queries.AssessShipmentRisk;
using Logistics.Application.Shipments.Queries.GetShipmentByTracking;
using Logistics.Application.Shipments.Queries.StreamShipmentsByStatus;
using Logistics.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // all shipment endpoints require a valid access token
public sealed class ShipmentsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShipmentRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateShipmentCommand(
            request.TrackingNumber, request.OriginWarehouseId, request.DestinationWarehouseId,
            request.WeightKg, request.Mode), ct);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetByTracking), new { trackingNumber = request.TrackingNumber }, new { id = result.Value })
            : BadRequest(result.Error);
    }

    [HttpGet("{trackingNumber}")]
    public async Task<IActionResult> GetByTracking(string trackingNumber, CancellationToken ct)
    {
        var result = await sender.Send(new GetShipmentByTrackingQuery(trackingNumber), ct);
        return result.Succeeded ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>
    /// Streams all shipments with the given status as a JSON array. The response is produced
    /// incrementally (IAsyncEnumerable) so neither the server nor the DB buffers the full set.
    /// </summary>
    [HttpGet("by-status/{status}")]
    public async Task<IAsyncEnumerable<Application.Shipments.Queries.GetShipmentByTracking.ShipmentDto>>
        StreamByStatus(ShipmentStatus status, CancellationToken ct)
        => await sender.Send(new StreamShipmentsByStatusQuery(status), ct);

    /// <summary>Delivery-risk assessment for a shipment, via the risk domain service.</summary>
    [HttpGet("{id}/risk")]
    public async Task<IActionResult> Risk(string id, CancellationToken ct)
    {
        var result = await sender.Send(new AssessShipmentRiskQuery(id), ct);
        return result.Succeeded ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost("{id}/status")]
    [Authorize(Roles = "Operator,Admin")] // status changes need elevated privileges
    public async Task<IActionResult> UpdateStatus(
        string id, [FromBody] UpdateShipmentStatusRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateShipmentStatusCommand(
            id, request.Transition, request.EstimatedArrival, request.Reason), ct);

        return result.Succeeded ? NoContent() : BadRequest(result.Error);
    }
}
