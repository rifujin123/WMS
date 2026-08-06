using System.ComponentModel.DataAnnotations;
using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class CreateLocationDto
{
    [Required]
    public Guid WarehouseId { get; set; }

    [Required(ErrorMessage = "Code is required")]
    [MaxLength(50, ErrorMessage = "Code must not exceed 50 characters")]
    [RegularExpression(@"^[A-Z0-9\-]+$", ErrorMessage = "Code must contain only uppercase letters, numbers, and hyphens")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Aisle is required")]
    [MaxLength(20, ErrorMessage = "Aisle must not exceed 20 characters")]
    public string Aisle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rack is required")]
    [MaxLength(20, ErrorMessage = "Rack must not exceed 20 characters")]
    public string Rack { get; set; } = string.Empty;

    [Required(ErrorMessage = "Level is required")]
    [MaxLength(20, ErrorMessage = "Level must not exceed 20 characters")]
    public string Level { get; set; } = string.Empty;

    [Required(ErrorMessage = "Location type is required")]
    [EnumDataType(typeof(LocationType), ErrorMessage = "Invalid location type")]
    public LocationType LocationType { get; set; }

    [Required(ErrorMessage = "Max quantity is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Max quantity must be at least 1")]
    public int MaxQuantity { get; set; }
}
