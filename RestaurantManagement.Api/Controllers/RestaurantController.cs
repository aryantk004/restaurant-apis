
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;

namespace RestaurantManagement.Api.Controllers;

[ApiController] // attribute = annotations
[Route("api/restaurants")] // Controller Route
public class RestaurantsController : ControllerBase
{
    private readonly IRestaurantService _restaurantService;

    public RestaurantsController(IRestaurantService restaurantService)
    {
        _restaurantService = restaurantService;
    }

    // GET /api/restaurants
    [HttpGet]
    public async Task<ActionResult<List<RestaurantResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var restaurants = await _restaurantService.GetAllAsync(
            cancellationToken);

        return Ok(restaurants);
    }

    // GET /api/restaurants/{id}
    [HttpGet("{id:long}")] // Method Route
    public async Task<ActionResult<RestaurantResponse>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantService.GetByIdAsync(
            id, cancellationToken);

        if (restaurant == null)
        {
            return NotFound();
        }

        return Ok(restaurant);
    }

    // POST /api/restaurants
    [HttpPost]
    public async Task<ActionResult<RestaurantResponse>> Create(
        [FromBody] CreateRestaurantRequest request,
        CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantService.CreateAsync(
            request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = restaurant.Id },
            restaurant);
    }

    // PUT /api/restaurants/1
    [HttpPut("{id:long}")]
    public async Task<ActionResult<RestaurantResponse>> Update(
        long id,
        [FromBody] UpdateRestaurantRequest request,
        CancellationToken cancellationToken)
    {
        var restaurant = await _restaurantService.UpdateAsync(
            id, request, cancellationToken);

        if (restaurant == null)
        {
            return NotFound();
        }

        return Ok(restaurant);
    }

    // DELETE /api/restaurants/1
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        var deleted = await _restaurantService.DeleteAsync(
            id, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
