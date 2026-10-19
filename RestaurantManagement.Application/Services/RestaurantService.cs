
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

    // GET /api/restaurants?page=1&pageSize=10
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

        // Log pagination result
        _logger.LogDebug(
            "Retrieved restaurant page {Page}, page size {PageSize}, returned {ReturnedCount} of {TotalCount} restaurants",
            page,
            pageSize,
            items.Count,
            totalCount);

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
        // Step 1: Check Redis first
        try
        {
            var cached = await _cache.GetAsync(id);

            if (cached != null)
            {
                _logger.LogDebug(
                    "Redis cache HIT for restaurant {RestaurantId}",
                    id);

                return cached;
            }

            _logger.LogDebug(
                "Redis cache MISS for restaurant {RestaurantId}",
                id);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            // Redis failure should not block MSSQL reads
            _logger.LogWarning(
                ex,
                "Redis read failed for restaurant {RestaurantId}. Falling back to MSSQL",
                id);
        }

        // Step 2: Fetch from MSSQL
        var restaurant = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (restaurant == null || restaurant.IsDeleted)
        {
            _logger.LogDebug(
                "Restaurant {RestaurantId} not found in MSSQL",
                id);

            return null;
        }

        _logger.LogDebug(
            "Restaurant {RestaurantId} retrieved from MSSQL",
            id);

        var response = ToResponse(restaurant);

        // Step 3: Populate Redis cache
        try
        {
            await _cache.SetAsync(response);

            _logger.LogDebug(
                "Restaurant {RestaurantId} stored in Redis cache",
                id);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Redis write failed for restaurant {RestaurantId}",
                id);
        }

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

        // Save to MSSQL
        await _repository.AddAsync(
            restaurant,
            cancellationToken);

        // Log only after successful database save
        _logger.LogInformation(
            "Restaurant {RestaurantId} created successfully",
            restaurant.Id);

        // Lazy caching: first GET populates Redis
        return ToResponse(restaurant);
    }

    // PUT /api/restaurants/{id}
    public async Task<RestaurantResponse?> UpdateAsync(
        long id,
        UpdateRestaurantRequest request,
        CancellationToken cancellationToken = default)
    {
        var restaurant = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (restaurant == null || restaurant.IsDeleted)
        {
            _logger.LogDebug(
                "Update requested for nonexistent restaurant {RestaurantId}",
                id);

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

        // Step 1: Update MSSQL
        await _repository.UpdateAsync(
            restaurant,
            cancellationToken);

        _logger.LogInformation(
            "Restaurant {RestaurantId} updated successfully in MSSQL",
            id);

        // Step 2: Invalidate Redis
        try
        {
            await _cache.RemoveAsync(id);

            _logger.LogDebug(
                "Redis cache invalidated for restaurant {RestaurantId}",
                id);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Redis invalidation failed after updating restaurant {RestaurantId}",
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
            id,
            cancellationToken);

        if (restaurant == null || restaurant.IsDeleted)
        {
            _logger.LogDebug(
                "Delete requested for nonexistent restaurant {RestaurantId}",
                id);

            return false;
        }

        // Soft delete: preserve the SQL row
        restaurant.IsDeleted = true;
        restaurant.IsActive = false;
        restaurant.UpdatedAtUtc = DateTime.UtcNow;

        // Step 1: Save soft deletion in MSSQL
        await _repository.DeleteAsync(
            restaurant,
            cancellationToken);

        _logger.LogInformation(
            "Restaurant {RestaurantId} soft deleted successfully",
            id);

        // Step 2: Invalidate Redis
        try
        {
            await _cache.RemoveAsync(id);

            _logger.LogDebug(
                "Redis cache invalidated after deleting restaurant {RestaurantId}",
                id);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Redis invalidation failed after deleting restaurant {RestaurantId}",
                id);
        }

        return true;
    }

    // Convert database entity to API response DTO
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
