using HexShield.Shared.DTOs;

namespace HexShield.Services;

public interface IAdminService
{
    Task<IEnumerable<UserListDto>> GetUsersAsync();
    Task<UserDetailsDto?> GetUserByIdAsync(string id);
    Task<UserDetailsDto> CreateUserAsync(CreateUserRequestDto dto);
    Task<UserDetailsDto> UpdateUserAsync(string userId, UpdateUserDto dto);
    Task<bool> SetUserActiveStatusAsync(string userId, bool isActive);
    Task<bool> AssignRoleAsync(string userId, string roleName);
    Task<bool> RemoveRoleAsync(string userId, string roleName);
}
