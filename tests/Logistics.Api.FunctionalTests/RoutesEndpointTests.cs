using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Logistics.Api.FunctionalTests;

/// <summary>
/// Boots the API in-memory. These exercise the HTTP pipeline (routing, model binding,
/// exception middleware). Endpoints that hit Neo4j need a running database or a fake
/// registered via WebApplicationFactory's ConfigureServices — see ShortestPath note.
/// </summary>
public class RoutesEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CreateRoute_WithInvalidBody_ReturnsBadRequest()
    {
        var client = factory.CreateClient();

        // Origin == destination violates the validator -> 400 via exception middleware.
        var response = await client.PostAsJsonAsync("/api/routes", new
        {
            originWarehouseId = "w1",
            destinationWarehouseId = "w1",
            distanceKm = 10.0,
            cost = 5.0,
            mode = 0
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}
