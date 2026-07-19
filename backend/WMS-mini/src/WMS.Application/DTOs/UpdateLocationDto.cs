using System.ComponentModel.DataAnnotations;
using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class UpdateLocationDto
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Aisle { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Rack { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Level { get; set; } = string.Empty;

    public LocationType LocationType { get; set; }
    public int MaxQuantity { get; set; }
}
