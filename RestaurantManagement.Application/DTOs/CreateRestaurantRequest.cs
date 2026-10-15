
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.Application.DTOs;

public class CreateRestaurantRequest
{
    [Required(ErrorMessage = "Restaurant name is required.")]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Area is required.")]
    [MaxLength(100)]
    public string Area { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Cuisine { get; set; }

    [Range(1, 180)]
    public int DeliveryTimeMinutes { get; set; }

    public bool IsActive { get; set; } = true;
}
