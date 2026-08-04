namespace WMS.Application.DTOs;

public class RegisterDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    // "Admin" | "WarehouseManager" | "WarehouseStaff" — mặc định WarehouseStaff nếu bỏ trống
    public string? Role { get; set; }
}
