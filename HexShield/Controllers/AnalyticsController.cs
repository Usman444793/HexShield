using HexShield.Shared.DTOs;
using HexShield.Shared.Enums;
using HexShield.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HexShield.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IProgressAndAttendanceService _analyticsService;

    public AnalyticsController(IProgressAndAttendanceService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    private string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system-user";

    [HttpGet("attendance/course/{courseId:int}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<IEnumerable<AttendanceRecordDto>>> GetAttendance(int courseId, [FromQuery] DateTime? date)
    {
        var records = await _analyticsService.GetAttendanceByCourseAsync(courseId, date);
        return Ok(records);
    }

    [HttpPost("attendance/bulk")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> BulkMarkAttendance([FromBody] BulkMarkAttendanceRequestDto dto)
    {
        try
        {
            await _analyticsService.BulkMarkAttendanceAsync(dto, CurrentUserId);
            return Ok(new { message = "Attendance saved successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("lessons/{lessonId:int}/complete")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> CompleteLesson(int lessonId)
    {
        await _analyticsService.MarkLessonCompletedAsync(lessonId, CurrentUserId);
        return Ok(new { message = "Lesson marked as complete." });
    }

    [HttpGet("lessons/course/{courseId:int}/completed-ids")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<IEnumerable<int>>> GetCompletedLessonIds(int courseId)
    {
        var ids = await _analyticsService.GetCompletedLessonIdsAsync(courseId, CurrentUserId);
        return Ok(ids);
    }

    [HttpGet("attendance/course/{courseId:int}/my-attendance")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<IEnumerable<AttendanceRecordDto>>> GetMyAttendance(int courseId)
    {
        var records = await _analyticsService.GetAttendanceByCourseAsync(courseId, null);
        var myRecords = records
            .Where(r => r.StudentId == CurrentUserId)
            .OrderByDescending(r => r.SessionDate)
            .ToList();
        return Ok(myRecords);
    }

    [HttpGet("progress/course/{courseId:int}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<IEnumerable<StudentCourseProgressDto>>> GetCourseProgressSummary(int courseId)
    {
        var summary = await _analyticsService.GetCourseProgressSummaryAsync(courseId);
        return Ok(summary);
    }

    [HttpGet("progress/course/{courseId:int}/my-progress")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<StudentCourseProgressDto>> GetMyProgress(int courseId)
    {
        var progress = await _analyticsService.GetStudentProgressInCourseAsync(courseId, CurrentUserId);
        return Ok(progress);
    }
}