
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

        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Request failed with status {StatusCode}",
                statusCode);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Request rejected with status {StatusCode}",
                statusCode);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode >= 500
                ? "An unexpected error occurred. Please try again later."
                : exception.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken);

        return true;
    }
}
