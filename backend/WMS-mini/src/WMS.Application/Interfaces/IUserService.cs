using WMS.Application.DTOs;

namespace WMS.Application.Interfaces;

public interface IUserService
{
    Task<UserProfileDto?> GetProfileAsync(Guid userId);
    Task<UserProfileDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task<string?> UploadAvatarAsync(Guid userId, Stream fileStream, string fileName);
    Task<List<UserListItemDto>> GetAllAsync(string? role = null, string? search = null, string? status = null);
    Task<bool> UpdateUserAsync(Guid id, UpdateUserDto dto);
    Task<bool> ResetPasswordAsync(Guid id, string newPassword);
    Task<bool> SetLockAsync(Guid id, bool locked);
}