using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class VerifyEmailDto
{
    [Required(ErrorMessage = "Mã xác thực token là bắt buộc.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    public string Email { get; set; } = string.Empty;
}
