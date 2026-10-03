using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace HexShield.Infrastructure.Middleware;

public class ConcurrencyExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ConcurrencyExceptionMiddleware> _logger;

    public ConcurrencyExceptionMiddleware(RequestDelegate next, ILogger<ConcurrencyExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict detected for request {Path}", context.Request.Path);
            await HandleConcurrencyConflictAsync(context, ex);
        }
    }

    private static Task HandleConcurrencyConflictAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.Conflict; // 409 Conflict

        var response = new
        {
            statusCode = 409,
            error = "Conflict",
            message = "The resource you tried to update was modified by another user. Please reload and try again.",
            timestamp = DateTimeOffset.UtcNow
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}