using Logistics.Application.Common.Interfaces;
using Logistics.Application.Identity;
using Confluent.SchemaRegistry;
using Logistics.Application.Common.Messaging;
using Logistics.Infrastructure.Identity;
using Logistics.Infrastructure.Messaging;
using Logistics.Infrastructure.Messaging.Kafka;
using Logistics.Infrastructure.Messaging.RabbitMq;
using Logistics.Infrastructure.Persistence.Neo4j;
using Logistics.Infrastructure.Persistence.Neo4j.Migrations;
using Logistics.Infrastructure.Persistence.Neo4j.Repositories;
using Logistics.Infrastructure.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Logistics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<Neo4jSettings>(configuration.GetSection(Neo4jSettings.SectionName));
        services.Configure<AuthSettings>(configuration.GetSection(AuthSettings.SectionName));

        // One driver for the whole app (thread-safe); sessions are created per request.
        services.AddSingleton<Neo4jContext>();

        // Neo4jClient ORM, wrapping the same driver (CRUD repositories use this).
        services.AddSingleton<Neo4jGraphClientProvider>();

        // Persistence
        services.AddScoped<IRouteRepository, RouteRepository>();
        services.AddScoped<IGraphAnalyticsRepository, GraphAnalyticsRepository>();
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Identity primitives
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // Async domain-event pipeline: shared channel (singleton) + background worker.
        services.AddSingleton<IDomainEventQueue, ChannelDomainEventQueue>();
        services.AddHostedService<DomainEventProcessor>();

        AddRateLimiting(services, configuration);
        AddMessaging(services, configuration);

        // Versioned graph migrations (schema + data), applied in order on startup.
        services.AddSingleton<IGraphMigration, M0001_InitialSchema>();
        services.AddSingleton<IGraphMigration, M0002_DefaultWarehouseCapacity>();
        services.AddHostedService<GraphMigrationRunner>();

        return services;
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        // --- Kafka: integration-event backbone ---
        var kafka = configuration.GetSection(KafkaSettings.SectionName);
        services.Configure<KafkaSettings>(kafka);
        var kafkaSettings = kafka.Get<KafkaSettings>() ?? new KafkaSettings();

        if (kafkaSettings.Enabled)
        {
            // Shared Schema Registry client (Avro serde uses it to register/fetch schemas).
            services.AddSingleton<ISchemaRegistryClient>(_ =>
                new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = kafkaSettings.SchemaRegistryUrl }));
            services.AddSingleton<IIntegrationEventPublisher, KafkaEventPublisher>();
            services.AddHostedService<KafkaIntegrationEventConsumer>();
        }
        else
        {
            services.AddSingleton<IIntegrationEventPublisher, NoOpIntegrationEventPublisher>();
        }

        // --- RabbitMQ: notification bus ---
        var rabbit = configuration.GetSection(RabbitMqSettings.SectionName);
        services.Configure<RabbitMqSettings>(rabbit);
        var rabbitSettings = rabbit.Get<RabbitMqSettings>() ?? new RabbitMqSettings();

        if (rabbitSettings.Enabled)
        {
            services.AddSingleton<RabbitMqConnection>();
            services.AddSingleton<INotificationPublisher, RabbitMqNotificationPublisher>();
            services.AddHostedService<RabbitMqNotificationConsumer>();
        }
        else
        {
            services.AddSingleton<INotificationPublisher, NoOpNotificationPublisher>();
        }
    }

    private static void AddRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(RateLimitSettings.SectionName);
        services.Configure<RateLimitSettings>(section);
        var settings = section.Get<RateLimitSettings>() ?? new RateLimitSettings();

        if (settings.Enabled)
        {
            // One multiplexer for the app lifetime — it manages its own connection pool.
            services.AddSingleton<IConnectionMultiplexer>(
                _ => ConnectionMultiplexer.Connect(settings.RedisConnectionString));
            services.AddSingleton<IRateLimiter, RedisTokenBucketRateLimiter>();
        }
        else
        {
            services.AddSingleton<IRateLimiter, NoOpRateLimiter>();
        }
    }
}
