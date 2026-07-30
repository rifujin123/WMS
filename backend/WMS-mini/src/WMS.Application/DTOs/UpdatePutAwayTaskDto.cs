using System.ComponentModel.DataAnnotations;
using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class UpdatePutAwayTaskDto
{
    [Required]
    public Guid ReceivingDetailId { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }
    public PutAwayTaskStatus Status { get; set; }
    public Guid? AssignToId { get; set; }
}