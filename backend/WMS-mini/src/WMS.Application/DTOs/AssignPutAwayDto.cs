using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class AssignPutAwayDto
{
    [Required]
    public Guid UserId { get; set; }
}