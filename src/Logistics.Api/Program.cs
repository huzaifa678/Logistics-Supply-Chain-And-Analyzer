using Logistics.Api.Extensions;
using Logistics.Api.Middleware;
using Logistics.Application;
using Logistics.Infrastructure;
using Logistics.Infrastructure.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// Health checks: liveness = process up; readiness = Neo4j reachable (tagged "ready").
builder.Services.AddHealthChecks()
    .AddCheck<Neo4jHealthCheck>("neo4j", tags: ["ready"]);

// Layer composition — wire each layer's DI from its own module.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Authentication / authorization (JWT bearer).
builder.Services.AddJwtAuthentication(builder.Configuration);

// CORS for the SPA (no-op when the frontend calls through a same-origin proxy/ingress).
builder.Services.AddFrontendCors(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandlingMiddleware();
app.UseHttpsRedirection();

app.UseCors(Logistics.Api.Extensions.CorsExtensions.FrontendPolicy);

app.UseAuthentication();

// Rate limit after auth (so we can partition by user id) but before controllers run.
app.UseDistributedRateLimiting();

app.UseAuthorization();

app.MapControllers();

// Probe endpoints (anonymous). Liveness runs no checks — it only confirms the process is up.
// Readiness runs the "ready"-tagged checks (Neo4j connectivity).
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();

// Exposed so the functional test project can reference the entry point.
public partial class Program;
