using Logistics.Api.Extensions;
using Logistics.Api.Middleware;
using Logistics.Application;
using Logistics.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// Layer composition — wire each layer's DI from its own module.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Authentication / authorization (JWT bearer).
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandlingMiddleware();
app.UseHttpsRedirection();

app.UseAuthentication();

// Rate limit after auth (so we can partition by user id) but before controllers run.
app.UseDistributedRateLimiting();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed so the functional test project can reference the entry point.
public partial class Program;
