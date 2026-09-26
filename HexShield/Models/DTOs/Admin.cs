using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace HexShield.Models.DTOs.Admin;

public record UserListDto(
    string Id,
    int TenantID,
    string FullName,
    string Email,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IEnumerable<string> Roles
    );
public record UserDetailsDto(
     string Id,
    int TenantID,
    string FullName,
    string Email,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IEnumerable<string> Roles,
    IEnumerable<int> OrganizationNodeIds
);
public record CreateUserRequestDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    IEnumerable<string> Roles,
    int? TenantId = null
    );
public record UpdateUserDto(
    string FirstName,
    string LastName,
    string Email
    );
public record AssignRoleDto(string RoleName);
public record AssignOrganizationNodeDto(
    int OrganizationNodeId,
    string RoleId
);