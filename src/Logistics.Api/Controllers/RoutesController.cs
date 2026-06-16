using Logistics.Api.Contracts;
using Logistics.Application.Routes.Commands.CreateRoute;
using Logistics.Application.Routes.Queries.EstimateRoute;
using Logistics.Application.Routes.Queries.GetRouteById;
using Logistics.Application.Routes.Queries.GetShortestPath;
using Logistics.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RoutesController(ISender sender) : ControllerBase
{
    /// <summary>Create a weighted route between two warehouses.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRouteRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateRouteCommand(
                request.OriginWarehouseId,
                request.DestinationWarehouseId,
                request.DistanceKm,
                request.Cost,
                request.Mode),
            ct);

        return result.Succeeded
            ? CreatedAtAction(nameof(Create), new { id = result.Value }, result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>Fetch a single route by id.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await sender.Send(new GetRouteByIdQuery(id), ct);
        return result.Succeeded ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>Weighted shortest path between two warehouses.</summary>
    [HttpGet("shortest-path")]
    public async Task<IActionResult> ShortestPath(
        [FromQuery] string origin, [FromQuery] string destination, CancellationToken ct)
    {
        var result = await sender.Send(new GetShortestPathQuery(origin, destination), ct);
        return result.Succeeded ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>Estimate duration and cost for a mode, via the routing domain service.</summary>
    [HttpGet("estimate")]
    public async Task<IActionResult> Estimate(
        [FromQuery] string origin,
        [FromQuery] string destination,
        [FromQuery] TransportMode mode,
        CancellationToken ct)
    {
        var result = await sender.Send(new EstimateRouteQuery(origin, destination, mode), ct);
        return result.Succeeded ? Ok(result.Value) : NotFound(result.Error);
    }
}
