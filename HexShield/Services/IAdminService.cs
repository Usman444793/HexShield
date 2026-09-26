using HexShield.Models.DTOs.Admin;
using System.Globalization;
namespace HexShield.Services;
public interface IAdminService
{
    Task<IEnumerable<UserListDto>> GetUsersAsync();
    Task<UserDetailsDto?> GetUserByIdAsync(string Id);
    Task<UserDetailsDto> CreateUserAsync(CreateUserRequestDto dto);
    Task<UserDetailsDto> UpdateUserAsync(string userID,UpdateUserDto dto);
    Task<bool> SetUserActiveStatusAsync(string userId, bool IsActive);
    Task<bool> AssignRoleAsync(string userId,string roleName);
    Task<bool> RemoveRoleAsync(string userId, string roleName);
}
