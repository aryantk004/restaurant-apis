
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RestaurantManagement.Api.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int statusCode;
        string title;

        switch (exception)
        {
            case ArgumentException:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Invalid request";
                break;

            case DbUpdateConcurrencyException:
                statusCode = StatusCodes.Status409Conflict;
                title = "Concurrency conflict";
                break;

            case TimeoutException:
                statusCode = StatusCodes.Status503ServiceUnavailable;
                title = "Service temporarily unavailable";
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                title = "Internal server error";
                break;
        }

        var method = httpContext.Request.Method;
        var path = httpContext.Request.Path;
        var traceId = httpContext.TraceIdentifier;

        // Log server errors as Error
        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Request {Method} {Path} failed with status {StatusCode}. TraceId: {TraceId}",
                method,
                path,
                statusCode,
                traceId);
        }
        else
        {
            // Log client errors and conflicts as Warning
            _logger.LogWarning(
                exception,
                "Request {Method} {Path} rejected with status {StatusCode}. TraceId: {TraceId}",
                method,
                path,
                statusCode,
                traceId);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode >= 500
                ? "An unexpected error occurred. Please try again later."
                : exception.Message,
            Instance = path.ToString()
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken);

        return true;
    }
}
