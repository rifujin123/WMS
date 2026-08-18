using WMS.Domain.Entities;

namespace WMS.Domain.Common;

public abstract class BaseAuditableEntity
{
    public Guid Id { get; set; }
    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? UpdatedById { get; set; }
    public User? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
    public Guid? DeletedById { get; set; }
    public User? DeletedBy { get; set; }
    public DateTime? DeletedDate { get; set; }
}
