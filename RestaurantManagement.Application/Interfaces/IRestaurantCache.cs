
using RestaurantManagement.Application.DTOs;

namespace RestaurantManagement.Application.Interfaces;

public interface IRestaurantCache
{
    Task<RestaurantResponse?> GetAsync(long id);
    Task SetAsync(RestaurantResponse restaurant);
    Task RemoveAsync(long id);
}
