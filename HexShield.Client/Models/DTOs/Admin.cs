namespace HexShield.Models.DTOs.Admin;

public record UserListDto(
    string Id,
    int TenantId,
    string FullName,
    string Email,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IEnumerable<string> Roles
);

public record UserDetailsDto(
    string Id,
    int TenantId,
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
    string Email,
    byte[] RowVersion
);

public record AssignRoleDto(string RoleName);

public record AssignOrganizationNodeDto(
    int OrganizationNodeId,
    string RoleId
);

public record OrganizationNodeDto(
    int Id,
    int TenantId,
    string Name,
    string NodeType,
    int? ParentId,
    IEnumerable<OrganizationNodeDto>? Children
);

public record CreateOrganizationNodeDto(
    string Name,
    string NodeType,
    int? ParentId,
    string? Code = null
);

public record UpdateOrganizationNodeDto(
    string Name,
    string NodeType,
    int? ParentId,
    byte[] RowVersion
);

public record AssignUserToNodeDto(
    string UserId,
    string RoleName
);
