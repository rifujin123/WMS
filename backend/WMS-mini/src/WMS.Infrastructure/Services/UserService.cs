using AutoMapper;
using Microsoft.AspNetCore.Identity;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;
    private readonly IImageService _imageService;

    public UserService(UserManager<User> userManager, IMapper mapper, IImageService imageService)
    {
        _userManager = userManager;
        _mapper = mapper;
        _imageService = imageService;
    }

    public async Task<List<UserListItemDto>> GetAllAsync()
    {
        var users = _userManager.Users.OrderByDescending(u => u.CreatedAt).ToList();
        var result = new List<UserListItemDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isLocked = user.LockoutEnabled && user.LockoutEnd is { } end && end > DateTimeOffset.UtcNow;
            result.Add(new UserListItemDto
            {
                Id = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? string.Empty,
                Status = isLocked ? "locked" : "active",
                CreatedAt = user.CreatedAt,
            });
        }

        return result;
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        var profile = _mapper.Map<UserProfileDto>(user);
        profile.Roles = (await _userManager.GetRolesAsync(user)).ToList();
        return profile;
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;
        
        if (!string.IsNullOrWhiteSpace(dto.AvatarUrl))
            user.AvatarUrl = dto.AvatarUrl;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        var profile = _mapper.Map<UserProfileDto>(user);
        profile.Roles = (await _userManager.GetRolesAsync(user)).ToList();
        return profile;
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) throw new Exception("User not found.");

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<string?> UploadAvatarAsync(Guid userId, Stream fileStream, string fileName)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        var url = await _imageService.UploadAsync(fileStream, fileName, $"wms/avatars/{userId}", 400, 400);

        user.AvatarUrl = url;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        return user.AvatarUrl;
    }
}