
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Application.Services;
using RestaurantManagement.Infrastructure.Persistence;
using RestaurantManagement.Infrastructure.Repositories;
using RestaurantManagement.Infrastructure.Caching;
using RestaurantManagement.Api.Exceptions;

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

builder.Services.AddProblemDetails();

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
