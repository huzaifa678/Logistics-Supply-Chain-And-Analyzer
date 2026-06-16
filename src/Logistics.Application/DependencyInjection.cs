using System.Reflection;
using FluentValidation;
using Logistics.Application.Common.Adapters;
using Logistics.Application.Common.Behaviours;
using Logistics.Application.Common.Interfaces;
using Logistics.Domain.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Logistics.Application;

// Core composition root for loose coupling wiring dependencies (persistence classes, validation classes, domain services) into the Application layer.
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register command/query handlers wiring up the CQRS pipeline
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        // Register FluentValidation validators and the pipeline behaviour that invokes them.
        services.AddValidatorsFromAssembly(assembly);
        // Adds validation to validate commands/queries before hitting their handlers, returning errors if invalid.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        // finally adding domain services and their adapters to the persistence layer
        AddDomainServices(services);

        // Discover every domain-event handler so the dispatcher stays open for extension.
        foreach (var handler in assembly.GetTypes()
                     .Where(t => t is { IsAbstract: false, IsInterface: false }
                                 && typeof(IDomainEventHandler).IsAssignableFrom(t)))
        {
            services.AddTransient(typeof(IDomainEventHandler), handler);
        }

        return services;
    }

    private static void AddDomainServices(IServiceCollection services)
    {
        // Routing domain service + the adapter that connects it to the persistence port.
        services.AddScoped<IShipmentRoutingService, ShipmentRoutingService>();
        services.AddScoped<IRouteGraph, GraphAnalyticsRouteGraphAdapter>();

        // Risk-scoring domain service + its congestion adapter.
        services.AddScoped<IShipmentRiskService, ShipmentRiskService>();
        services.AddScoped<IWarehouseCongestionProvider, WarehouseCongestionAdapter>();

        // Transport-mode strategies (Strategy pattern) + their resolver.
        services.AddSingleton<ITransportModeProfile, RoadProfile>();
        services.AddSingleton<ITransportModeProfile, RailProfile>();
        services.AddSingleton<ITransportModeProfile, SeaProfile>();
        services.AddSingleton<ITransportModeProfile, AirProfile>();
        services.AddSingleton<ITransportModeProfile, IntermodalProfile>();
        services.AddSingleton<ITransportModeProfileResolver, TransportModeProfileResolver>();

        // Risk-factor strategies — add a factor here and the score picks it up, no service change.
        services.AddSingleton<IRiskFactor, DistanceRiskFactor>();
        services.AddSingleton<IRiskFactor, HopCountRiskFactor>();
        services.AddSingleton<IRiskFactor, TransportModeRiskFactor>();
        services.AddSingleton<IRiskFactor, DelayRiskFactor>();
        services.AddSingleton<IRiskFactor, CongestionRiskFactor>();
    }
}
