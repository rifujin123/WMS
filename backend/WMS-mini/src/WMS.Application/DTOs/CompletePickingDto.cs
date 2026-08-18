using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class CompletePickingDto
{
    [Required]
    [MinLength(1)]
    public List<CompletePickingDetailDto> Details { get; set; } = new();
}
