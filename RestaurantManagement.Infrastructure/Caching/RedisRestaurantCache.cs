
using System.Text.Json;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using StackExchange.Redis;

namespace RestaurantManagement.Infrastructure.Caching;

public class RedisRestaurantCache : IRestaurantCache
{
    private readonly IDatabase _database;

    private static readonly TimeSpan CacheDuration =
        TimeSpan.FromMinutes(5);

    public RedisRestaurantCache(
        IConnectionMultiplexer redis) // redis connection manager shared
    {
        _database = redis.GetDatabase(); // IDatabase object to run commands on
    }

    public async Task<RestaurantResponse?> GetAsync(long id)
    {
        var key = $"restaurant:{id}";

        var value = await _database.StringGetAsync(key);

        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<RestaurantResponse>(
            value.ToString());
    }

    public async Task SetAsync(RestaurantResponse restaurant)
    {
        var key = $"restaurant:{restaurant.Id}";

        var json = JsonSerializer.Serialize(restaurant);

        await _database.StringSetAsync(
            key,
            json,
            CacheDuration);
    }

    public async Task RemoveAsync(long id)
    {
        var key = $"restaurant:{id}";

        await _database.KeyDeleteAsync(key);
    }
}