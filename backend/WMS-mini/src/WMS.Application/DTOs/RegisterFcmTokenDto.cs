namespace WMS.Application.DTOs;

public class RegisterFcmTokenDto
{
    public string FcmToken { get; set; } = string.Empty;
    public string? DeviceType { get; set; } = "Web";
}
