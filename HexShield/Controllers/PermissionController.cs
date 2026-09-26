using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HexShield.Models.Identity;
using HexShield.Data;
using Microsoft.EntityFrameworkCore;

namespace HexShield.Controllers;

[Authorize(Policy = "AdminOnly")]
[Route("api/[controller]")]
public class PermissionController : ApiControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public PermissionController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetPermissions()
    {
        return Ok(await _dbContext.Permissions.ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> CreatePermission([FromBody] ApplicationPermission permission)
    {
        _dbContext.Permissions.Add(permission);
        await _dbContext.SaveChangesAsync();
        return Ok(permission);
    }

    [HttpPost("assign")]
    public async Task<IActionResult> AssignPermissionToRole([FromBody] RolePermission assignment)
    {
        // Check if already assigned
        var exists = await _dbContext.RolePermissions
            .AnyAsync(rp => rp.RoleId == assignment.RoleId && rp.PermissionId == assignment.PermissionId);
        
        if (exists) return BadRequest("Permission already assigned to this role.");

        _dbContext.RolePermissions.Add(assignment);
        await _dbContext.SaveChangesAsync();
        return Ok(new { message = "Permission assigned successfully." });
    }

    [HttpDelete("revoke")]
    public async Task<IActionResult> RevokePermission([FromBody] RolePermission assignment)
    {
        var record = await _dbContext.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == assignment.RoleId && rp.PermissionId == assignment.PermissionId);

        if (record == null) return NotFound("Assignment not found.");

        _dbContext.RolePermissions.Remove(record);
        await _dbContext.SaveChangesAsync();
        return Ok(new { message = "Permission revoked successfully." });
    }
}
