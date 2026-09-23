namespace WMS.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    Guid? WarehouseId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(params string[] roles);
}
