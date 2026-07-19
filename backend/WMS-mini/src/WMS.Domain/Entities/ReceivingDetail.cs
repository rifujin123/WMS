using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class ReceivingDetail : BaseAuditableEntity
{
    public Guid ReceivingId { get; set; }

    [Required]
    public Receiving Receiving { get; set; } = null!;

    public Guid ProductId { get; set; }

    [Required]
    public Product Product { get; set; } = null!;

    public int ExpectedQuantity { get; set; }
    public int ActualQuantity { get; set; }
    public ProductCondition Condition { get; set; }

    public ICollection<PutAwayTask> PutAwayTasks { get; set; } = new List<PutAwayTask>();
}
