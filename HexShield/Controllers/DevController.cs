using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using HexShield.Data;

namespace HexShield.Controllers;

[ApiController]
[Route("api/dev")]
public class DevController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IHostEnvironment _env;

    public DevController(ApplicationDbContext db, IHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet("users")]
    public IActionResult GetUsers()
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var users = _db.Users.Select(u => new { u.Id, u.Email, u.UserName, u.TenantId, u.IsActive }).ToList();
        return Ok(users);
    }

    [HttpGet("user-by-email")]
    public IActionResult GetUserByEmail([FromQuery] string email)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var user = _db.Users.FirstOrDefault(u => u.Email == email);
        if (user == null) return NotFound(new { message = "Not found" });
        return Ok(new { user.Id, user.Email, user.UserName, user.TenantId, user.IsActive });
    }
}
