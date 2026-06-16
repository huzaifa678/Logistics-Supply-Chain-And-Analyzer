using System.Net;
using FluentValidation;
using Logistics.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Api.Middleware;

/// <summary>
/// Translates uncaught exceptions into RFC 7807 ProblemDetails responses so
/// controllers never have to write try/catch boilerplate.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await WriteProblem(context, HttpStatusCode.BadRequest, "Validation failed",
                string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
        }
        catch (DomainException ex)
        {
            await WriteProblem(context, HttpStatusCode.UnprocessableEntity, "Domain rule violated", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteProblem(context, HttpStatusCode.InternalServerError, "Unexpected error",
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblem(HttpContext context, HttpStatusCode status, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = (int)status,
            Title = title,
            Detail = detail
        };
        context.Response.StatusCode = problem.Status!.Value;
        await context.Response.WriteAsJsonAsync(problem);
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}
