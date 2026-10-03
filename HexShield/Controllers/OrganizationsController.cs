using HexShield.Models.DTOs.Admin;
using HexShield.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HexShield.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/organizations")]
public class OrganizationsController : ApiControllerBase
{
    private readonly IOrganizationService _organizationService;
    public OrganizationsController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrganizationNodeDto>>> GetHierarchy()
    {
        var nodes = await _organizationService.GetOrganizationHierarchyAsync();
        return Ok(nodes);
    }
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrganizationNodeDto>> GetById(int id)
    {
        var node = await _organizationService.GetNodeByIdAsync(id);
        if (node == null) return NotFound(new { message = $"Organization node {id} not found." });
        return Ok(node);
    }
    [HttpPost]
    public async Task<ActionResult<OrganizationNodeDto>> Create([FromBody] CreateOrganizationNodeDto dto)
    {
        var node = await _organizationService.CreateNodeAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = node.Id }, node);
    }
    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrganizationNodeDto>> Update(int id, [FromBody] UpdateOrganizationNodeDto dto)
    {
        var node = await _organizationService.UpdateNodeAsync(id, dto);
        return Ok(node);
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _organizationService.DeleteNodeAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
    [HttpPost("{id:int}/assign-user")]
    public async Task<IActionResult> AssignUser(int id, [FromBody] AssignUserToNodeDto dto)
    {
        var success = await _organizationService.AssignUserToNodeAsync(id, dto);
        if (!success) return BadRequest(new { message = "Failed to assign user to organization node." });
        return Ok(new { message = $"User assigned to organization node successfully." });
    }
}