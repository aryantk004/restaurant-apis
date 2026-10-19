
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace RestaurantManagement.Application.Services;

public class RestaurantService : IRestaurantService
{
    private readonly IRestaurantRepository _repository;
    private readonly IRestaurantCache _cache;
    private readonly ILogger<RestaurantService> _logger;

    public RestaurantService(
        IRestaurantRepository repository,
        IRestaurantCache cache,
        ILogger<RestaurantService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }


    public async Task<PaginatedResponse<RestaurantResponse>> GetAllAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Fetch paginated restaurants and total count from MSSQL
        var (restaurants, totalCount) = await _repository.GetAllAsync(
            page,
            pageSize,
            cancellationToken);

        // Convert database entities to response DTOs
        var items = restaurants
            .Select(r => ToResponse(r))
            .ToList();

        // Return paginated response
        return new PaginatedResponse<RestaurantResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }


    // GET /api/restaurants/{id}
    public async Task<RestaurantResponse?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Check Redis FIRST.
        try
        {
            var cached = await _cache.GetAsync(id);

            if (cached != null)
            {
                _logger.LogInformation(
                    "Cache HIT for restaurant {Id}", id);

                return cached;
            }

            _logger.LogInformation(
                "Cache MISS for restaurant {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Redis read failed for restaurant {Id}", id);
        }

        // Step 2: Cache miss - fetch from MSSQL.
        var restaurant = await _repository.GetByIdAsync(
            id, cancellationToken);

        if (restaurant == null || restaurant.IsDeleted)
        {
            return null;
        }

        var response = ToResponse(restaurant);

        // Step 3: Store the result in Redis.
        try
        {
            await _cache.SetAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Redis write failed for restaurant {Id}", id);
        }

        // Step 4: Return the restaurant.
        return response;
    }

    // POST /api/restaurants
    public async Task<RestaurantResponse> CreateAsync(
        CreateRestaurantRequest request,
        CancellationToken cancellationToken = default)
    {
        var restaurant = new Restaurant
        {
            Name = request.Name.Trim(),
            City = request.City.Trim(),
            Area = request.Area.Trim(),
            Cuisine = string.IsNullOrWhiteSpace(request.Cuisine)
                ? null
                : request.Cuisine.Trim(),
            DeliveryTimeMinutes = request.DeliveryTimeMinutes,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.AddAsync(
            restaurant, cancellationToken);

        // Lazy caching: the first GET populates Redis.
        return ToResponse(restaurant);
    }

    // PUT /api/restaurants/{id}
    public async Task<RestaurantResponse?> UpdateAsync(
        long id,
        UpdateRestaurantRequest request,
        CancellationToken cancellationToken = default)
    {
        var restaurant = await _repository.GetByIdAsync(
            id, cancellationToken);

        if (restaurant == null || restaurant.IsDeleted)
        {
            return null;
        }

        restaurant.Name = request.Name.Trim();
        restaurant.City = request.City.Trim();
        restaurant.Area = request.Area.Trim();

        restaurant.Cuisine = string.IsNullOrWhiteSpace(request.Cuisine)
            ? null
            : request.Cuisine.Trim();

        restaurant.DeliveryTimeMinutes =
            request.DeliveryTimeMinutes;

        restaurant.IsActive = request.IsActive
            ?? throw new ArgumentException(
                "IsActive is required.");

        restaurant.UpdatedAtUtc = DateTime.UtcNow;

        // Step 1: Update MSSQL.
        await _repository.UpdateAsync(
            restaurant, cancellationToken);

        // Step 2: Invalidate Redis.
        try
        {
            await _cache.RemoveAsync(id);

            _logger.LogInformation(
                "Cache invalidated for restaurant {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Redis invalidation failed after updating restaurant {Id}",
                id);
        }

        return ToResponse(restaurant);
    }

    // DELETE /api/restaurants/{id}
    public async Task<bool> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var restaurant = await _repository.GetByIdAsync(
            id, cancellationToken);

        if (restaurant == null || restaurant.IsDeleted)
        {
            return false;
        }

        // Soft delete: preserve the SQL row.
        restaurant.IsDeleted = true;
        restaurant.IsActive = false;
        restaurant.UpdatedAtUtc = DateTime.UtcNow;

        // Step 1: Save soft deletion in MSSQL.
        await _repository.DeleteAsync(
            restaurant, cancellationToken);

        // Step 2: Invalidate Redis.
        try
        {
            await _cache.RemoveAsync(id);

            _logger.LogInformation(
                "Cache invalidated after deleting restaurant {Id}",
                id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Redis invalidation failed after deleting restaurant {Id}",
                id);
        }

        return true;
    }

    // Convert database entity to API response DTO.
    private static RestaurantResponse ToResponse(
        Restaurant restaurant)
    {
        return new RestaurantResponse
        {
            Id = restaurant.Id,
            Name = restaurant.Name,
            City = restaurant.City,
            Area = restaurant.Area,
            Cuisine = restaurant.Cuisine,
            DeliveryTimeMinutes =
                restaurant.DeliveryTimeMinutes,
            IsActive = restaurant.IsActive,
            CreatedAtUtc = restaurant.CreatedAtUtc,
            UpdatedAtUtc = restaurant.UpdatedAtUtc
        };
    }
}
