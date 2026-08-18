using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class CompletePickingDetailDto
{
    [Required]
    public Guid DetailId { get; set; }

    [Range(1, int.MaxValue)]
    public int QtyPicked { get; set; }
}
