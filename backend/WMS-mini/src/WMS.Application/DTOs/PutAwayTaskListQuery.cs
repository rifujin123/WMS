using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class PutAwayTaskListQuery
{
    public int Page { get; init; } = 1;
    public Guid? AssignToId { get; init; }
    public PutAwayTaskStatus? Status { get; init; }
}
