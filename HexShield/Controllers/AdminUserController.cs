using HexShield.Models.DTOs.Admin;
using HexShield.Models.Identity;
using HexShield.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace HexShield.Controllers;
[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/[controller]")]
public class AdminUserController : ApiControllerBase
{
    private readonly AdminService _adminService;

    public AdminUserController(AdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserListDto>>> GetUsers()
    {
        var users = await _adminService.GetUsersAsync();
        return Ok(users);
    }
    [HttpGet("user/{id}")]
    public async Task<ActionResult<UserDetailsDto>> GetUserById(string id)
    {
        var user = await _adminService.GetUserByIdAsync(id);
        if (user == null) return BadRequest(new {message = $"User with {id} not found"});
        return Ok(user);
    }
    [HttpPost("users")]
    public async Task<ActionResult<UserDetailsDto>> CreateUser([FromBody] CreateUserRequestDto dto)
    {
        var user = await _adminService.CreateUserAsync(dto);
        return Ok(user);
    }
    [HttpPut("user/{id}")]
    public async Task<ActionResult<UserDetailsDto>> UpdateUser(string id, [FromBody] UpdateUserDto dto)
    {
        var user = await _adminService.UpdateUserAsync(id, dto);
        return Ok(user);
    }
    [HttpPut("user/{id}/activate")]
    public async Task<ActionResult> ActivateUser(string id) 
    {
        var activate = await _adminService.SetUserActiveStatusAsync(id,true);
        if (!activate) return NotFound();
        return Ok(activate);
    }
    [HttpPut("user/{id}/deactive")]
    public async Task<ActionResult> DeactivateUser(string id)
    {
        var deactive = await _adminService.SetUserActiveStatusAsync(id, false);
        if (!deactive) return NotFound();
        return Ok(deactive);
    }
    [HttpPost("users/{id}/roles")]
    public async Task<ActionResult> AssignRole(string id, [FromBody] AssignRoleDto dto)
    {
        var role = await _adminService.AssignRoleAsync(id, dto.RoleName);
        if (!role) return BadRequest(new { message = "Failed to assign role." });
        return Ok(new { message = $"Role '{dto.RoleName}' assigned successfully." });
    }
    [HttpDelete("users/{id}/roles/{roleName}")]
    public async Task<ActionResult> RemoveRole(string id,string roleName)
    {
        var success = await _adminService.RemoveRoleAsync(id, roleName);
        if (!success) return BadRequest(new { message = "Failed to remove role." });
        return Ok(new { message = $"Role '{roleName}' removed successfully." });
    }
}