
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Application.Services;
using RestaurantManagement.Infrastructure.Persistence;
using RestaurantManagement.Infrastructure.Repositories;
using RestaurantManagement.Infrastructure.Caching;
using RestaurantManagement.Api.Exceptions;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Register API controllers
builder.Services.AddControllers();

// Configure MSSQL with Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Connect to Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException(
            "Redis connection string is missing.");

    return ConnectionMultiplexer.Connect(connectionString);
});

// Configure API rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(
            context =>
            {
                var clientIp = context.Connection.RemoteIpAddress?
                    .ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
});

// Register Redis cache
builder.Services.AddScoped<
    IRestaurantCache,
    RedisRestaurantCache>();

// Register Repository
builder.Services.AddScoped<
    IRestaurantRepository,
    SqlRestaurantRepository>();

// Register Service
builder.Services.AddScoped<
    IRestaurantService,
    RestaurantService>();

// Standardized error responses
builder.Services.AddProblemDetails();

// Register global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// Log every HTTP request and response
app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();

    try
    {
        await next(context);
    }
    finally
    {
        stopwatch.Stop();

        app.Logger.LogInformation(
            "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms. TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.Elapsed.TotalMilliseconds,
            context.TraceIdentifier);
    }
});

// Enable centralized exception handling
app.UseExceptionHandler();

app.UseHttpsRedirection();

// Enable rate limiting
app.UseRateLimiter();

app.MapControllers();

app.Run();
