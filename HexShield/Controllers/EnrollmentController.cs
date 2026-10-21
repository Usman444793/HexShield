using HexShield.Shared.DTOs;
using HexShield.Shared.Enums;
using HexShield.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HexShield.Controllers;

/// <summary>
/// Enrollment management. Accessible via /api/enrollments.
/// </summary>
[ApiController]
[Route("api/enrollments")]
[Authorize]
public class EnrollmentsController : ApiControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    private string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system-user";

    [HttpGet("course/{courseId:int}")]
    [HttpGet("/api/courses/{courseId:int}/enrollments")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<IEnumerable<EnrollmentListDto>>> GetByCourse(int courseId)
    {
        var result = await _enrollmentService.GetEnrollmentsByCourse(courseId);
        return Ok(result);
    }

    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<IEnumerable<EnrollmentListDto>>> GetByStudent(string studentId)
    {
        var result = await _enrollmentService.GetEnrollmentsByStudent(studentId);
        return Ok(result);
    }

    [HttpPost]
    [HttpPost("/api/courses/{courseId:int}/enrollments")]
    [Authorize(Roles = "Admin,Teacher,Student")]
    public async Task<ActionResult<EnrollmentListDto>> EnrollStudent([FromBody] CreateEnrollmentRequestDto dto, [FromRoute] int? courseId = null)
    {
        if (courseId.HasValue && courseId.Value > 0 && dto.CourseId == 0)
        {
            dto = dto with { CourseId = courseId.Value };
        }
        try
        {
            var enrollment = await _enrollmentService.EnrollStudentAsync(dto, CurrentUserId);
            return CreatedAtAction(nameof(GetByCourse), new { courseId = dto.CourseId }, enrollment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<EnrollmentListDto>> UpdateStatus(int id, [FromBody] UpdateEnrollmentStatusDto dto)
    {
        try
        {
            var updated = await _enrollmentService.UpdateStatusAsync(id, dto.Status, CurrentUserId);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Unenroll(int id)
    {
        var success = await _enrollmentService.UnenrollStudentAsync(id, CurrentUserId);
        if (!success) return NotFound();
        return NoContent();
    }
}