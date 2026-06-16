using Logistics.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Logistics.Infrastructure.Messaging;

/// <summary>
/// The "worker thread": a long-running <see cref="BackgroundService"/> that drains the
/// domain-event channel and dispatches each event to its handlers.
///
/// Prod-grade details:
///  - A fresh DI scope per event, so scoped dependencies (repositories) behave correctly.
///  - Handler exceptions are caught and logged — one bad event never kills the loop.
///  - Cooperative cancellation via the host's stopping token for graceful shutdown.
/// </summary>
public sealed class DomainEventProcessor(
    IDomainEventQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<DomainEventProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Domain event processor started.");

        await foreach (var domainEvent in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var handlers = scope.ServiceProvider.GetServices<IDomainEventHandler>();

                foreach (var handler in handlers.Where(h => h.CanHandle(domainEvent)))
                    await handler.HandleAsync(domainEvent, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break; // graceful shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to dispatch domain event {EventType}",
                    domainEvent.GetType().Name);
            }
        }

        logger.LogInformation("Domain event processor stopping.");
    }
}
