using WMS.Domain.Entities;

namespace WMS.Domain.Common;

public abstract class BaseAuditableEntity
{
    public Guid Id { get; set; }
    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
}
