using Microsoft.AspNetCore.Authorization;
using HexShield.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace HexShield.Infrastructure.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}

public class PermissionHandler : IAuthorizationHandler
{
    private readonly ApplicationDbContext _dbContext;

    public PermissionHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var requirement = context.Requirements.OfType<PermissionRequirement>().FirstOrDefault();
        if (requirement == null) return;

        if (context.User == null) return;

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return;

        var orgNodeClaim = context.User.FindFirst("orgNodeId")?.Value;
        int? currentOrgNodeId = int.TryParse(orgNodeClaim, out var nodeId) ? nodeId : null;

        var globalRoleIds = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var orgRoleIds = await _dbContext.UserOrganizationRoles
            .Where(uor => uor.UserId == userId && !uor.IsDeleted)
            .Where(uor => currentOrgNodeId == null || uor.OrganizationNodeId == currentOrgNodeId)
            .Select(uor => uor.RoleId)
            .ToListAsync();

        var allEffectiveRoles = globalRoleIds.Union(orgRoleIds).ToList();

        if (currentOrgNodeId != null)
        {
            var hierarchyPath = await _dbContext.OrganizationNodes
                .Where(n => n.Id == currentOrgNodeId)
                .Select(n => n.HierarchyPath)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(hierarchyPath))
            {
                var parentNodeIds = await _dbContext.OrganizationNodes
                    .Where(n => hierarchyPath.StartsWith(n.HierarchyPath))
                    .Select(n => n.Id)
                    .ToListAsync();

                var inheritedRoleIds = await _dbContext.UserOrganizationRoles
                    .Where(uor => uor.UserId == userId && !uor.IsDeleted && parentNodeIds.Contains(uor.OrganizationNodeId))
                    .Select(uor => uor.RoleId)
                    .ToListAsync();

                allEffectiveRoles.AddRange(inheritedRoleIds);
            }
        }

        if (!allEffectiveRoles.Any()) return;

        var permissionExists = await (from rp in _dbContext.RolePermissions
                                     join p in _dbContext.Permissions on rp.PermissionId equals p.Id
                                     where allEffectiveRoles.Contains(rp.RoleId) && p.Name == requirement.Permission
                                     select p).AnyAsync();

        if (permissionExists)
        {
            context.Succeed(requirement);
        }
    }
}
