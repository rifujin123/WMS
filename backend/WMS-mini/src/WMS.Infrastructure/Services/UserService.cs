using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly WmsDbContext _db;
    private readonly IMapper _mapper;
    private readonly IImageService _imageService;

    public UserService(UserManager<User> userManager, WmsDbContext db, IMapper mapper, IImageService imageService)
    {
        _userManager = userManager;
        _db = db;
        _mapper = mapper;
        _imageService = imageService;
    }

    public async Task<PagedResult<UserListItemDto>> GetPagedAsync(
        UserListQuery query,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var users = _userManager.Users.AsNoTracking().AsQueryable();
        var search = query.Search?.Trim();
        var role = query.Role?.Trim();
        var status = query.Status?.Trim().ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(role))
        {
            users = users.Where(u => _db.UserRoles
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Any(x => x.UserId == u.Id && x.Name == role));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            users = users.Where(u => (u.FullName != null && u.FullName.Contains(search))
                || (u.UserName != null && u.UserName.Contains(search)));
        }

        if (status == "locked")
        {
            users = users.Where(u => u.LockoutEnabled && u.LockoutEnd.HasValue && u.LockoutEnd > now);
        }
        else if (status == "active")
        {
            users = users.Where(u => !u.LockoutEnabled || !u.LockoutEnd.HasValue || u.LockoutEnd <= now);
        }

        var totalCount = await users.CountAsync(cancellationToken);
        var pageUsers = await users
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Id)
            .Skip((query.Page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var roleNames = await _db.UserRoles
            .Where(ur => pageUsers.Select(u => u.Id).Contains(ur.UserId))
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name })
            .ToListAsync(cancellationToken);
        var rolesByUserId = roleNames
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName!).ToList());

        var items = pageUsers.Select(user =>
        {
            var isLocked = user.LockoutEnabled && user.LockoutEnd is { } end && end > now;
            var roles = rolesByUserId.TryGetValue(user.Id, out var userRoles)
                ? userRoles
                : [];
            return new UserListItemDto
            {
                Id = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl,
                Role = roles.FirstOrDefault() ?? string.Empty,
                Status = isLocked ? "locked" : "active",
                CreatedAt = user.CreatedAt,
            };
        }).ToList();

        return PagedResult<UserListItemDto>.Create(items, query.Page, pageSize, totalCount);
    }

    public async Task<List<UserListItemDto>> GetAllAsync(string? role = null, string? search = null, string? status = null)
    {
        var users = _userManager.Users.OrderByDescending(u => u.CreatedAt).ToList();

        // 1 query lấy toàn bộ vai trò, tránh N+1 khi gọi GetRolesAsync trong vòng lặp
        var roleNames = await _db.UserRoles
            .Join(_db.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, RoleName = r.Name })
            .ToListAsync();
        var rolesByUserId = roleNames
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName!).ToList());

        var result = new List<UserListItemDto>();

        foreach (var user in users)
        {
            var roles = rolesByUserId.TryGetValue(user.Id, out var userRoles) ? userRoles : new List<string>();
            if (!string.IsNullOrWhiteSpace(role) && !roles.Contains(role)) continue;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.ToLower();
                var match = (user.FullName?.ToLower().Contains(keyword) ?? false)
                    || (user.UserName?.ToLower().Contains(keyword) ?? false);
                if (!match) continue;
            }

            var isLocked = user.LockoutEnabled && user.LockoutEnd is { } end && end > DateTimeOffset.UtcNow;
            var currentStatus = isLocked ? "locked" : "active";
            if (!string.IsNullOrWhiteSpace(status) && status != currentStatus) continue;
            result.Add(new UserListItemDto
            {
                Id = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl,
                Role = roles.FirstOrDefault() ?? string.Empty,
                Status = currentStatus,
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

    public async Task<bool> UpdateUserAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return false;

        user.FullName = dto.FullName;
        if (!string.IsNullOrWhiteSpace(dto.Email))
            user.Email = dto.Email;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new Exception(string.Join(", ", updateResult.Errors.Select(e => e.Description)));

        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, dto.Role);
        }

        return true;
    }

    public async Task<bool> ResetPasswordAsync(Guid id, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return false;

        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
            throw new Exception(string.Join(", ", removeResult.Errors.Select(e => e.Description)));

        var addResult = await _userManager.AddPasswordAsync(user, newPassword);
        if (!addResult.Succeeded)
            throw new Exception(string.Join(", ", addResult.Errors.Select(e => e.Description)));

        return true;
    }

    public async Task<bool> SetLockAsync(Guid id, bool locked)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return false;

        user.LockoutEnabled = true;
        user.LockoutEnd = locked ? DateTimeOffset.MaxValue : null;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        return true;
    }
}