using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class AssignPickingDto
{
    [Required]
    public Guid UserId { get; set; }
}
