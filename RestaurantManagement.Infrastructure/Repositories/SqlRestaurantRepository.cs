
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Infrastructure.Persistence;

namespace RestaurantManagement.Infrastructure.Repositories;

public class SqlRestaurantRepository : IRestaurantRepository
{
    private readonly AppDbContext _context;

    public SqlRestaurantRepository(AppDbContext context)
    {
        _context = context;
    }


    public async Task<(List<Restaurant> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Query Restaurants table.
        // The global query filter excludes soft-deleted restaurants.
        var query = _context.Restaurants.AsNoTracking();

        // Count all matching restaurants before pagination.
        var totalCount = await query.CountAsync(cancellationToken);

        // Retrieve only restaurants belonging to the requested page.
        var restaurants = await query
            .OrderBy(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (restaurants, totalCount);
    }


    // GET restaurant by ID
    public async Task<Restaurant?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Restaurants
            .FirstOrDefaultAsync(
                r => r.Id == id,
                cancellationToken);
    }

    // CREATE restaurant
    public async Task AddAsync(
        Restaurant restaurant,
        CancellationToken cancellationToken = default)
    {
        await _context.Restaurants.AddAsync(
            restaurant, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    // UPDATE restaurant
    public async Task UpdateAsync(
        Restaurant restaurant,
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    // SOFT DELETE restaurant
    public async Task DeleteAsync(
        Restaurant restaurant,
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
