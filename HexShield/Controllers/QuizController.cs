using Microsoft.AspNetCore.Mvc;
using HexShield.Shared.DTOs;
using HexShield.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HexShield.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/quizzes")]
[Authorize]
public class QuizController : ApiControllerBase
{
    private readonly IQuizService _quizService;

    public QuizController(IQuizService quizService)
    {
        _quizService = quizService;
    }

    private string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system-user";

    [HttpGet("course/{courseId:int}")]
    [HttpGet("/api/courses/{courseId:int}/quizzes")]
    public async Task<ActionResult<IEnumerable<QuizListDto>>> GetByCourse(int courseId)
    {
        var result = await _quizService.GetQuizzesByCourseAsync(courseId);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuizDetailsDto>> GetById(int id)
    {
        var result = await _quizService.GetQuizById(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<QuizDetailsDto>> Create([FromBody] CreateQuizRequestDto dto)
    {
        try
        {
            var created = await _quizService.CreateQuizAsync(dto, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _quizService.DeleteQuizAsync(id, CurrentUserId);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{id:int}/questions")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<QuizDetailsDto>> AddQuestion(int id, [FromBody] QuizQuestionDto dto)
    {
        try
        {
            var addQuestion = await _quizService.AddQuestionToQuizAsync(id, dto, CurrentUserId);
            return Ok(addQuestion);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/start")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<QuizAttemptResultDto>> StartAttempt(int id)
    {
        try
        {
            var attempt = await _quizService.StartAttemptAsync(id, CurrentUserId);
            return Ok(attempt);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("submit")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<QuizAttemptResultDto>> SubmitAttempt([FromBody] SubmitQuizAttemptRequestDto dto)
    {
        try
        {
            var submit = await _quizService.SubmitAttemptAsync(dto, CurrentUserId);
            return Ok(submit);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/my-attempts")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<IEnumerable<QuizAttemptResultDto>>> GetMyAttempts(int id)
    {
        var attempts = await _quizService.GetStudentAttemptsAsync(id, CurrentUserId);
        return Ok(attempts);
    }
}
