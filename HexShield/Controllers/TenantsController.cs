using HexShield.Data;
using HexShield.Models.Common;
using HexShield.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HexShield.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/tenants")]
public class TenantsController : ApiControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public TenantsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TenantDto>>> GetTenants()
    {
        var tenants = await _dbContext.Tenants
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Select(t => new TenantDto(
                t.Id,
                t.Name,
                t.Identifier,
                t.CustomDomain,
                t.IsActive,
                t.CreatedAt
            ))
            .ToListAsync();

        return Ok(tenants);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TenantDto>> GetTenantById(int id)
    {
        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TenantDto(
                t.Id,
                t.Name,
                t.Identifier,
                t.CustomDomain,
                t.IsActive,
                t.CreatedAt
            ))
            .FirstOrDefaultAsync();

        if (tenant == null)
            return NotFound(new { message = $"Tenant {id} not found." });

        return Ok(tenant);
    }

    [HttpPost]
    public async Task<ActionResult<TenantDto>> CreateTenant([FromBody] CreateTenantDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Identifier))
            return BadRequest(new { message = "Name and Identifier are required." });

        var normalizedIdentifier = dto.Identifier.Trim().ToLowerInvariant();

        var exists = await _dbContext.Tenants
            .AnyAsync(t => t.Identifier.ToLower() == normalizedIdentifier);

        if (exists)
            return BadRequest(new { message = $"Tenant with identifier '{dto.Identifier}' already exists." });

        var tenant = new Tenant
        {
            Name = dto.Name.Trim(),
            Identifier = normalizedIdentifier,
            CustomDomain = string.IsNullOrWhiteSpace(dto.CustomDomain) ? null : dto.CustomDomain.Trim(),
            IsActive = true
        };

        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync();

        var result = new TenantDto(
            tenant.Id,
            tenant.Name,
            tenant.Identifier,
            tenant.CustomDomain,
            tenant.IsActive,
            tenant.CreatedAt
        );

        return CreatedAtAction(nameof(GetTenantById), new { id = tenant.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TenantDto>> UpdateTenant(int id, [FromBody] UpdateTenantDto dto)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == id);
        if (tenant == null)
            return NotFound(new { message = $"Tenant {id} not found." });

        var normalizedIdentifier = dto.Identifier.Trim().ToLowerInvariant();

        var exists = await _dbContext.Tenants
            .AnyAsync(t => t.Id != id && t.Identifier.ToLower() == normalizedIdentifier);

        if (exists)
            return BadRequest(new { message = $"Tenant with identifier '{dto.Identifier}' already exists." });

        tenant.Name = dto.Name.Trim();
        tenant.Identifier = normalizedIdentifier;
        tenant.CustomDomain = string.IsNullOrWhiteSpace(dto.CustomDomain) ? null : dto.CustomDomain.Trim();
        tenant.IsActive = dto.IsActive;

        await _dbContext.SaveChangesAsync();

        return Ok(new TenantDto(
            tenant.Id,
            tenant.Name,
            tenant.Identifier,
            tenant.CustomDomain,
            tenant.IsActive,
            tenant.CreatedAt
        ));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> ToggleTenantActive(int id)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == id);
        if (tenant == null)
            return NotFound(new { message = $"Tenant {id} not found." });

        tenant.IsActive = !tenant.IsActive;
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = $"Tenant status updated to {(tenant.IsActive ? "Active" : "Inactive")}." });
    }
}
