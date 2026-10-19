
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

var builder = WebApplication.CreateBuilder(args); // config logging DI setup

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
    // Return HTTP 429 when the limit is exceeded
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    // Apply the limit to every API request
    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(
            context =>
            {
                // Identify the client by IP address
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

builder.Services.AddProblemDetails(); // Standardised error response (like status codes etc.)

builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // Custom exception handler registered

var app = builder.Build();

app.UseExceptionHandler(); // Enables exception middleware

app.UseHttpsRedirection();

// Enable rate limiting middleware
app.UseRateLimiter();

app.MapControllers();

app.Run();