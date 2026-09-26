using HexShield.Data;
using HexShield.Infrastructure.Tenancy;
using HexShield.Models.DTOs.Admin;
using HexShield.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace HexShield.Services;

public class AdminService : IAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AdminService> _logger;
    public AdminService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, ApplicationDbContext context, ITenantContext tenantContext, ILogger<AdminService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }
    public async Task<IEnumerable<UserListDto>> GetUsersAsync()
    {
        var users = await _userManager.Users.Include(u => u.OrganizationRoles).AsNoTracking().ToListAsync();
        var userList = new List<UserListDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userList.Add(new UserListDto
            (
                user.Id,
                user.TenantId,
                user.FullName,
                user.Email!,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt,
                roles
            ));
        }
        return userList;
    }
    public async Task<UserDetailsDto?> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.Users.Include(u => u.OrganizationRoles).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return null;
        var roles = await _userManager.GetRolesAsync(user);
        var orgNodeIds = user.OrganizationRoles.Select(r => r.OrganizationNodeId).ToList();
        return new UserDetailsDto(
            user.Id,
            user.TenantId,
            user.FullName,
            user.Email!,
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt,
            roles,
            orgNodeIds
        );
    }
    public async Task<UserDetailsDto> CreateUserAsync(CreateUserRequestDto dto)
    {
        var tenantId = dto.TenantId ?? _tenantContext.TenantId;
        if (tenantId <= 0)
        {
            throw new InvalidOperationException("Valid Tenant ID must be present in this context");
        }
        var NormalizedEmail = dto.Email.ToUpperInvariant();
        var existingUser = await _userManager.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == NormalizedEmail);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this Email already exists");
        }
        var fullName = string.IsNullOrWhiteSpace(dto.LastName) ? dto.FirstName.Trim() : $"{dto.FirstName.Trim()} {dto.LastName.Trim()}";
        var user = new ApplicationUser
        {
            UserName = dto.Email.Trim(),
            Email = dto.Email.Trim(),
            FullName = fullName,
            TenantId = tenantId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"User Creation failed: {errors}");
        }
        if (dto.Roles != null && dto.Roles.Any())
        {
            foreach (var role in dto.Roles)
            {
                if (await _roleManager.RoleExistsAsync(role))
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
            }
        }
        _logger.LogInformation("Admin created User with {Email} and Tenant {tenantId}",user.Email,tenantId);
        return (await GetUserByIdAsync(user.Id))!;
    }
    public async Task<UserDetailsDto> UpdateUserAsync(string userId, UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? throw new KeyNotFoundException($"User with Id {userId} not found");
        var fullName = string.IsNullOrWhiteSpace(dto.LastName) ? dto.FirstName.Trim() : $"{dto.FirstName.Trim()} {dto.LastName.Trim()}";
        user.FullName = fullName;
        user.Email = dto.Email.Trim();
        user.UserName = dto.Email.Trim();
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"User Update failed {errors}");
        }
        return (await GetUserByIdAsync(userId))!;
    }
    public async Task<bool> SetUserActiveStatusAsync(string userId,bool IsActive)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        user.IsActive = IsActive;
        await _userManager.UpdateAsync(user);
        _logger.LogInformation("User {UserId} active status changed to {IsActive}", userId, IsActive);
        return true;
    }
    public async Task<bool> AssignRoleAsync(string userId,string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if(user == null) return false;
        if (! await _roleManager.RoleExistsAsync(roleName))
        {
            throw new InvalidOperationException($"Role {roleName} does not exists");
        }
        var result = await _userManager.AddToRoleAsync(user,roleName);
        return true;
    }
    public async Task<bool> RemoveRoleAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        var result = await _userManager.RemoveFromRoleAsync(user,roleName);
        return result.Succeeded;
    }
}