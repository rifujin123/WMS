namespace WMS.Application.DTOs;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string? WarehouseCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}