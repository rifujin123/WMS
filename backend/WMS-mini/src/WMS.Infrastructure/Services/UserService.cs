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
            throw new InvalidOperationException(FormatIdentityErrors(result.Errors));

        var profile = _mapper.Map<UserProfileDto>(user);
        profile.Roles = (await _userManager.GetRolesAsync(user)).ToList();
        return profile;
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) throw new InvalidOperationException("Không tìm thấy người dùng.");

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(FormatIdentityErrors(result.Errors));
    }

    public async Task<string?> UploadAvatarAsync(Guid userId, Stream fileStream, string fileName)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        var url = await _imageService.UploadAsync(fileStream, fileName, $"wms/avatars/{userId}", 400, 400);

        user.AvatarUrl = url;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(FormatIdentityErrors(result.Errors));

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
            throw new InvalidOperationException(FormatIdentityErrors(updateResult.Errors));

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
            throw new InvalidOperationException(FormatIdentityErrors(removeResult.Errors));

        var addResult = await _userManager.AddPasswordAsync(user, newPassword);
        if (!addResult.Succeeded)
            throw new InvalidOperationException(FormatIdentityErrors(addResult.Errors));

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
            throw new InvalidOperationException(FormatIdentityErrors(result.Errors));

        return true;
    }

    private static string FormatIdentityErrors(IEnumerable<IdentityError> errors)
    {
        var translated = errors.Select(e => e.Code switch
        {
            "PasswordMismatch" => "Mật khẩu hiện tại không chính xác.",
            "PasswordTooShort" => "Mật khẩu quá ngắn, yêu cầu ít nhất 6 ký tự.",
            "PasswordRequiresNonAlphanumeric" => "Mật khẩu phải chứa ít nhất một ký tự đặc biệt (@, #, $, v.v.).",
            "PasswordRequiresDigit" => "Mật khẩu phải chứa ít nhất một chữ số (0-9).",
            "PasswordRequiresLower" => "Mật khẩu phải chứa ít nhất một chữ cái thường (a-z).",
            "PasswordRequiresUpper" => "Mật khẩu phải chứa ít nhất một chữ cái hoa (A-Z).",
            "DuplicateUserName" => "Tên đăng nhập đã được sử dụng.",
            "DuplicateEmail" => "Địa chỉ email đã được sử dụng.",
            _ => e.Description
        });
        return string.Join(" ", translated);
    }
}