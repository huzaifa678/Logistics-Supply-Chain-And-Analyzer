using Logistics.Application.Common.Interfaces;
using Logistics.Application.Routes.Commands.CreateRoute;
using Logistics.Domain.Entities;
using Logistics.Domain.Enums;
using Xunit;

namespace Logistics.Application.UnitTests;

public class CreateRouteCommandHandlerTests
{
    // Hand-rolled fake keeps the test free of a mocking dependency.
    private sealed class FakeRouteRepository : IRouteRepository
    {
        public HashSet<string> ExistingWarehouses { get; } = new();
        public Route? Added { get; private set; }

        public Task<bool> WarehouseExistsAsync(string id, CancellationToken ct = default)
            => Task.FromResult(ExistingWarehouses.Contains(id));

        public Task<string> AddAsync(Route route, CancellationToken ct = default)
        {
            Added = route;
            return Task.FromResult(route.Id);
        }

        public Task<Route?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult<Route?>(Added);

        public Task<IReadOnlyList<Route>> ListAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Route>>(Added is null ? [] : [Added]);
    }

    [Fact]
    public async Task Handle_WhenBothWarehousesExist_CreatesRoute()
    {
        var repo = new FakeRouteRepository();
        repo.ExistingWarehouses.Add("w1");
        repo.ExistingWarehouses.Add("w2");
        var handler = new CreateRouteCommandHandler(repo);

        var result = await handler.Handle(
            new CreateRouteCommand("w1", "w2", 50, 100m, TransportMode.Road), default);

        Assert.True(result.Succeeded);
        Assert.NotNull(repo.Added);
    }

    [Fact]
    public async Task Handle_WhenOriginMissing_Fails()
    {
        var repo = new FakeRouteRepository();
        repo.ExistingWarehouses.Add("w2");
        var handler = new CreateRouteCommandHandler(repo);

        var result = await handler.Handle(
            new CreateRouteCommand("w1", "w2", 50, 100m, TransportMode.Road), default);

        Assert.False(result.Succeeded);
        Assert.Null(repo.Added);
    }
}
