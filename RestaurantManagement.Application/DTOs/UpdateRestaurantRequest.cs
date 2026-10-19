
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.Application.DTOs;

public class UpdateRestaurantRequest
{
    [Required(ErrorMessage = "Restaurant name is required.")]
    [RegularExpression(@".*\S.*",
        ErrorMessage = "Restaurant name cannot be blank.")]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    [RegularExpression(@".*\S.*",
        ErrorMessage = "City cannot be blank.")]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Area is required.")]
    [RegularExpression(@".*\S.*",
        ErrorMessage = "Area cannot be blank.")]
    [MaxLength(100)]
    public string Area { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Cuisine { get; set; }

    [Range(1, 180,
        ErrorMessage = "Delivery time must be between 1 and 180 minutes.")]
    public int DeliveryTimeMinutes { get; set; }

    [Required(ErrorMessage = "Active status is required.")]
    public bool? IsActive { get; set; }
}
