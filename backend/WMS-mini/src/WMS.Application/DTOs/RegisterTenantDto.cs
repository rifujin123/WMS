using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class RegisterTenantDto
{
    [Required(ErrorMessage = "Tên doanh nghiệp là bắt buộc.")]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã định danh doanh nghiệp là bắt buộc.")]
    [MaxLength(50)]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Mã doanh nghiệp chỉ gồm chữ thường không dấu, số và dấu gạch ngang.")]
    public string CompanyCode { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Address { get; set; }

    [MaxLength(20)]
    public string? ContactPhone { get; set; }

    public bool HasExpiryManagement { get; set; }

    [Required(ErrorMessage = "Họ và tên người đại diện là bắt buộc.")]
    [MaxLength(100)]
    public string AdminFullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email đại diện là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [MaxLength(100)]
    public string AdminEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
    public string Password { get; set; } = string.Empty;
}
