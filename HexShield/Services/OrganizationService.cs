using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HexShield.Models.DTOs.Admin;
using HexShield.Infrastructure.Tenancy;
using HexShield.Data;
using HexShield.Models.Identity;
using HexShield.Models.Common;

namespace HexShield.Services;

public class OrganizationService : IOrganizationService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public OrganizationService(
        ApplicationDbContext context,
        ITenantContext tenantContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _context = context;
        _tenantContext = tenantContext;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IEnumerable<OrganizationNodeDto>> GetOrganizationHierarchyAsync()
    {
        var rootNodes = await _context.OrganizationNodes
            .Include(n => n.Children)
            .Where(n => n.ParentId == null)
            .AsNoTracking()
            .ToListAsync();

        return rootNodes.Select(MapToDto);
    }

    public async Task<OrganizationNodeDto?> GetNodeByIdAsync(int id)
    {
        var node = await _context.OrganizationNodes
            .Include(n => n.Children)
            .FirstOrDefaultAsync(n => n.Id == id);

        return node == null ? null : MapToDto(node);
    }

    public async Task<OrganizationNodeDto> CreateNodeAsync(CreateOrganizationNodeDto dto)
    {
        if (!_tenantContext.HasTenant || _tenantContext.TenantId <= 0)
        {
            throw new InvalidOperationException("A valid tenant context is required.");
        }

        string hierarchyPath = "/";
        if (dto.ParentId.HasValue)
        {
            var parent = await _context.OrganizationNodes.FirstOrDefaultAsync(n => n.Id == dto.ParentId.Value);
            if (parent == null)
            {
                throw new KeyNotFoundException($"Parent Organization Node {dto.ParentId.Value} not found.");
            }

            hierarchyPath = string.IsNullOrEmpty(parent.HierarchyPath)
                ? $"/{parent.Id}/"
                : $"{parent.HierarchyPath}{parent.Id}/";
        }

        var code = !string.IsNullOrWhiteSpace(dto.Code)
            ? dto.Code.Trim().ToUpperInvariant()
            : dto.Name.Trim().Replace(" ", "").ToUpperInvariant();

        if (code.Length > 20)
        {
            code = code[..20];
        }

        // Check if code already exists in this tenant
        var codeExists = await _context.OrganizationNodes
            .AnyAsync(n => n.TenantId == _tenantContext.TenantId && n.Code == code);
        if (codeExists)
        {
            code = $"{code[..Math.Min(code.Length, 15)]}_{Random.Shared.Next(100, 999)}";
        }

        var node = new OrganizationNode
        {
            Name = dto.Name.Trim(),
            Code = code,
            NodeType = dto.NodeType.Trim(),
            ParentId = dto.ParentId,
            HierarchyPath = hierarchyPath,
            TenantId = _tenantContext.TenantId
        };

        _context.OrganizationNodes.Add(node);
        await _context.SaveChangesAsync();

        return MapToDto(node);
    }

    public async Task<OrganizationNodeDto> UpdateNodeAsync(int id, UpdateOrganizationNodeDto dto)
    {
        var node = await _context.OrganizationNodes.FindAsync(id) 
            ?? throw new KeyNotFoundException($"Organization node {id} not found.");

        if (dto.ParentId.HasValue && dto.ParentId.Value == id)
        {
            throw new InvalidOperationException("An Organization node cannot be its own parent.");
        }

        if (dto.RowVersion != null && dto.RowVersion.Length > 0)
        {
            _context.Entry(node).Property(u => u.RowVersion).OriginalValue = dto.RowVersion;
        }

        node.Name = dto.Name.Trim();
        node.NodeType = dto.NodeType.Trim();
        node.ParentId = dto.ParentId;

        await _context.SaveChangesAsync();
        return MapToDto(node);
    }

    public async Task<bool> DeleteNodeAsync(int id)
    {
        var node = await _context.OrganizationNodes
            .Include(n => n.Children)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (node == null) return false;

        if (node.Children.Any())
        {
            throw new InvalidOperationException("Cannot delete an organization node that has child departments.");
        }

        _context.OrganizationNodes.Remove(node);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignUserToNodeAsync(int id, AssignUserToNodeDto dto)
    {
        var nodeExists = await _context.OrganizationNodes.AnyAsync(n => n.Id == id);
        if (!nodeExists) 
            throw new KeyNotFoundException($"Organization node {id} was not found.");

        var user = await _userManager.FindByIdAsync(dto.UserId) 
            ?? throw new KeyNotFoundException($"User with ID '{dto.UserId}' was not found.");

        var role = await _roleManager.FindByNameAsync(dto.RoleName) 
            ?? await _roleManager.FindByIdAsync(dto.RoleName)
            ?? throw new KeyNotFoundException($"Role '{dto.RoleName}' was not found.");

        var existingMapping = await _context.UserOrganizationRoles
            .FirstOrDefaultAsync(ur => ur.UserId == dto.UserId && ur.OrganizationNodeId == id && ur.RoleId == role.Id);

        if (existingMapping != null) return true;

        var userOrgRole = new UserOrganizationRole
        {
            UserId = dto.UserId,
            OrganizationNodeId = id,
            RoleId = role.Id
        };

        _context.UserOrganizationRoles.Add(userOrgRole);
        await _context.SaveChangesAsync();
        return true;
    }

    private static OrganizationNodeDto MapToDto(OrganizationNode node) => new(
        node.Id,
        node.TenantId,
        node.Name,
        node.NodeType,
        node.ParentId,
        node.Children?.Select(MapToDto)
    );
}
