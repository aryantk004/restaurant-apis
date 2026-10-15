
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
