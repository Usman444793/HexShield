using HexShield.Models.DTOs.Admin;
using HexShield.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HexShield.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/users")]
public class AdminUserController : ApiControllerBase
{
    private readonly IAdminService _adminService;

    public AdminUserController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    [HttpGet("/api/adminuser/users")]
    public async Task<ActionResult<IEnumerable<UserListDto>>> GetUsers()
    {
        var users = await _adminService.GetUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    [HttpGet("/api/adminuser/user/{id}")]
    public async Task<ActionResult<UserDetailsDto>> GetUserById(string id)
    {
        var user = await _adminService.GetUserByIdAsync(id);
        if (user == null) return NotFound(new { message = $"User with ID '{id}' was not found." });
        return Ok(user);
    }

    [HttpPost]
    [HttpPost("/api/adminuser/users")]
    public async Task<ActionResult<UserDetailsDto>> CreateUser([FromBody] CreateUserRequestDto dto)
    {
        var user = await _adminService.CreateUserAsync(dto);
        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    [HttpPut("/api/adminuser/user/{id}")]
    public async Task<ActionResult<UserDetailsDto>> UpdateUser(string id, [FromBody] UpdateUserDto dto)
    {
        var user = await _adminService.UpdateUserAsync(id, dto);
        return Ok(user);
    }

    [HttpPut("{id}/activate")]
    [HttpPut("/api/adminuser/user/{id}/activate")]
    public async Task<ActionResult> ActivateUser(string id) 
    {
        var activate = await _adminService.SetUserActiveStatusAsync(id, true);
        if (!activate) return NotFound(new { message = $"User with ID '{id}' was not found." });
        return Ok(new { message = "User activated successfully." });
    }

    [HttpPut("{id}/deactivate")]
    [HttpPut("{id}/deactive")]
    [HttpPut("/api/adminuser/user/{id}/deactive")]
    public async Task<ActionResult> DeactivateUser(string id)
    {
        var deactive = await _adminService.SetUserActiveStatusAsync(id, false);
        if (!deactive) return NotFound(new { message = $"User with ID '{id}' was not found." });
        return Ok(new { message = "User deactivated successfully." });
    }

    [HttpPost("{id}/roles")]
    [HttpPost("/api/adminuser/users/{id}/roles")]
    public async Task<ActionResult> AssignRole(string id, [FromBody] AssignRoleDto dto)
    {
        var role = await _adminService.AssignRoleAsync(id, dto.RoleName);
        if (!role) return BadRequest(new { message = "Failed to assign role." });
        return Ok(new { message = $"Role '{dto.RoleName}' assigned successfully." });
    }

    [HttpDelete("{id}/roles/{roleName}")]
    [HttpDelete("/api/adminuser/users/{id}/roles/{roleName}")]
    public async Task<ActionResult> RemoveRole(string id, string roleName)
    {
        var success = await _adminService.RemoveRoleAsync(id, roleName);
        if (!success) return BadRequest(new { message = "Failed to remove role." });
        return Ok(new { message = $"Role '{roleName}' removed successfully." });
    }
}