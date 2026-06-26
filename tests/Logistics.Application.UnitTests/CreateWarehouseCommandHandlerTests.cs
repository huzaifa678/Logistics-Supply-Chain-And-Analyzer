using Logistics.Application.Common.Interfaces;
using Logistics.Application.Warehouses.Commands.CreateWarehouse;
using Logistics.Domain.Common;
using Logistics.Domain.Entities;
using Logistics.Domain.Events;
using Xunit;

namespace Logistics.Application.UnitTests;

public class CreateWarehouseCommandHandlerTests
{
    private sealed class FakeWarehouseRepository : IWarehouseRepository
    {
        public Warehouse? Added { get; private set; }

        public Task<string> AddAsync(Warehouse warehouse, CancellationToken ct = default)
        {
            Added = warehouse;
            return Task.FromResult(warehouse.Id);
        }

        public Task<IReadOnlyList<Warehouse>> ListAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Warehouse>>(Added is null ? [] : [Added]);

        public Task<bool> ExistsAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Added?.Id == id);
    }

    private sealed class FakeEventQueue : IDomainEventQueue
    {
        public List<DomainEvent> Enqueued { get; } = new();

        public ValueTask EnqueueAsync(DomainEvent domainEvent, CancellationToken ct = default)
        {
            Enqueued.Add(domainEvent);
            return ValueTask.CompletedTask;
        }

        public IAsyncEnumerable<DomainEvent> DequeueAllAsync(CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    [Fact]
    public async Task Handle_CreatesWarehouse_AndEnqueuesCreatedEvent()
    {
        var repo = new FakeWarehouseRepository();
        var queue = new FakeEventQueue();
        var handler = new CreateWarehouseCommandHandler(repo, queue);

        var result = await handler.Handle(new CreateWarehouseCommand("Hub", 10, 20, 100), default);

        Assert.True(result.Succeeded);
        Assert.NotNull(repo.Added);
        Assert.Contains(queue.Enqueued, e => e is WarehouseCreatedEvent);
    }
}
