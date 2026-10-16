using RestaurantManagement.Domain.Entities;

namespace RestaurantManagement.Application.Interfaces;

public interface IRestaurantRepository
{
    Task<List<Restaurant>> GetAllAsync(
    CancellationToken cancellationToken = default);

    Task<Restaurant?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Restaurant restaurant,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Restaurant restaurant,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Restaurant restaurant,
        CancellationToken cancellationToken = default);
}