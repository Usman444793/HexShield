using HexShield.Models.DTOs.Courses;
using HexShield.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HexShield.Controllers;
[ApiController]
[Route("api/admin/courses")]
//[Authorize(Policy ="AdminOnly")]
public class CoursesController : ApiControllerBase
{
    private readonly ICourseService _courseService;
    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseListDto>>> GetCourses()
    {
        var result = await _courseService.GetCoursesAsync();
        return Ok(result);
    }
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CourseDetailsDto>> GetCourseById(int id)
    {
        var result = await _courseService.GetCourseByIdAsync(id);
        if (result == null)
            return NotFound(new { message = "Course not found" });
        return Ok(result);
    }
    [HttpPost]
    public async Task <ActionResult<CourseDetailsDto>> CreateCourse([FromBody] CreateCourseRequestDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        var course = await _courseService.CreateCourseAsync(dto, userId);
        return CreatedAtAction(nameof(GetCourseById), new { id = course.Id }, course);
    }
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CourseDetailsDto>> UpdateCourse(int id,[FromBody] UpdateCourseRequestDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();
        var course = await _courseService.UpdateCourseAsync(id, dto, userId);
        return Ok(course);
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();
        var deleted = await _courseService.DeleteCourseAsync(id,userId);
        if (!deleted)
            return NotFound();
        return NoContent();
    }
    [HttpPut("{id:int}/publish")]
    public async Task<IActionResult> PublishCourse(int id, [FromBody]PublishCourseRequestDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();
        var course = await _courseService.SetPublishStatusAsync(id, dto.IsPublished, userId);
        return Ok(course);
    }
}
