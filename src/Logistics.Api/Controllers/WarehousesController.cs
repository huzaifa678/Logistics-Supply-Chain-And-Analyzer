using Logistics.Api.Contracts;
using Logistics.Api.Extensions;
using Logistics.Application.Warehouses.Commands.CreateWarehouse;
using Logistics.Application.Warehouses.Queries.ListWarehouses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // any authenticated user can read; creation is restricted below
public sealed class WarehousesController(ISender sender) : ControllerBase
{
    /// <summary>All warehouses.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await sender.Send(new ListWarehousesQuery(), ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(result.Error);
    }

    /// <summary>Create a warehouse. Operator/Admin only.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.RequireOperator)]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateWarehouseCommand(request.Name, request.Latitude, request.Longitude, request.CapacityUnits),
            ct);

        return result.Succeeded
            ? CreatedAtAction(nameof(List), new { id = result.Value }, new { id = result.Value })
            : BadRequest(result.Error);
    }
}
