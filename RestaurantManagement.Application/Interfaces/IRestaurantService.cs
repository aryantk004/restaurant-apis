
using RestaurantManagement.Application.DTOs;

namespace RestaurantManagement.Application.Interfaces;

public interface IRestaurantService
{
    Task<PaginatedResponse<RestaurantResponse>> GetAllAsync(
    int page,
    int pageSize,
    CancellationToken cancellationToken = default);

    Task<RestaurantResponse?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<RestaurantResponse> CreateAsync(
        CreateRestaurantRequest request,
        CancellationToken cancellationToken = default);

    Task<RestaurantResponse?> UpdateAsync(
        long id,
        UpdateRestaurantRequest request,
        CancellationToken cancellationToken = default); // default -> optional

    Task<bool> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default);
}
