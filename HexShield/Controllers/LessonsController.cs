using HexShield.Client.Models.DTOs.Lesson;
using HexShield.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace HexShield.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class LessonsController : ApiControllerBase
{
    private readonly ILessonService _lessonService;
    private readonly ILogger<LessonsController> _logger;
    public LessonsController(ILessonService lessonService, ILogger<LessonsController> logger)
    {
        _lessonService = lessonService;
        _logger = logger;
    }
    private string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system-user";
    [HttpGet("course/{courseId:int}")]
    public async Task<ActionResult<IEnumerable<LessonListDto>>> GetLessonsByCourse(int courseId)
    {
        var lessons = await _lessonService.GetLessonByCourseAsync(courseId);
        return Ok(lessons);
    }
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LessonDetailDto>> GetLessonById(int id)
    {
        var lesson = await _lessonService.GetLessonByIdAsync(id);
        if (lesson == null) return NotFound(new { message = "Lesson not found" });
        return Ok(lesson);
    }
    [HttpPost]
    //[Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<LessonDetailDto>> CreateLesson([FromBody] CreateLessonRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        try
        {
            var createdLesson = await _lessonService.CreateLessonAsync(dto, CurrentUserId);
            return CreatedAtAction(nameof(GetLessonById), new { id = createdLesson.id }, createdLesson);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
    [HttpPut("{id:int}")]
    //[Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<LessonDetailDto>> UpdateLesson(int id,[FromBody]UpdateLessonRequestDto dto)
    {
        if(!ModelState.IsValid)
           return BadRequest(ModelState); 
        try
        {
            var result = await _lessonService.UpdateLessonAsync(id, dto, CurrentUserId);
            return Ok(result);
        }
        catch(KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch(ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
    [HttpDelete("{id:int}")]
    //[Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<LessonDetailDto>> DeleteLesson(int id)
    {
        var deleted = await _lessonService.DeleteLessonAsync(id,CurrentUserId);
        if (!deleted) 
        {
            return NotFound(new { message = "Lesson Could not be deleted" });
        }
        return NoContent();
    }
    [HttpPatch("{id:int}/publish")]
    public async Task<ActionResult> SetPublishStatus(int id, [FromBody] bool isPublished)
    {
        try
        {
            var publish = await _lessonService.SetPublishedStatusAsync(id, isPublished, CurrentUserId);
            return Ok(publish);
        }
        catch (KeyNotFoundException ex) 
        {
            return NotFound(new { message = ex.Message });
        }
    }
    [HttpPost("course/{courseId:int}/reorder")]
    public async Task<IActionResult> ReorderLessons(int courseId, [FromBody]IEnumerable<ReorderLessonDto> reorderDtos)
    {
        if (reorderDtos == null || !reorderDtos.Any())
            return BadRequest(new { message = "Reorder list cannot be empty." });
        try
        {
            await _lessonService.UpdateSortOrderAsync(courseId, reorderDtos, CurrentUserId);
            return Ok(new { message = "Lesson sort orders updated successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorder lessons for course {CourseId}", courseId);
            return BadRequest(new { message = ex.Message });
        }
    }
}