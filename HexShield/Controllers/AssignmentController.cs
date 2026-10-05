using Microsoft.AspNetCore.Authorization;
using HexShield.Services;
using Microsoft.AspNetCore.Mvc;
using HexShield.Models.Academic;
using System.Security.Claims;
using HexShield.Models.DTOs.Assignment;
namespace HexShield.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentController : ApiControllerBase
{
    private readonly IAssignmentService _assignmentService;
    public AssignmentController(IAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }
    private string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system-user";
    [HttpGet("course/{courseId:int}")]
    public async Task<ActionResult<IEnumerable<AssignmentListDto>>> GetByCourse(int courseId)
    {
        var result = await _assignmentService.GetAssignmentsByCourseAsync(courseId);
        return Ok(result);
    }
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssignmentDetailDto>> GetById(int id)
    {
        var result = await _assignmentService.GetAssignmentByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }
    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<AssignmentDetailDto>> Create([FromBody] CreateAssignmentRequestDto dto)
    {
        try
        {
            var created = await _assignmentService.CreateAssignmentAsync(dto, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { Id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<AssignmentDetailDto>> Update(int id,[FromBody] UpdateAssignmentRequestDto dto)
    {
        try
        {
            var updated = await _assignmentService.UpdateAssignmentAsync(id, dto, CurrentUserId);
            return Ok(updated);
        }
        catch(Exception ex)
        {
            return BadRequest(new {message = ex.Message});
        }
    }
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _assignmentService.DeleteAssignmentAsync(id, CurrentUserId);
        if (!success) return NotFound();
        return NoContent();
    }
    [HttpGet("{assignmentId:int}/submissions")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<IEnumerable<SubmissionListDto>>> GetSubmissions(int assignmentId)
    {
        var submissions = await _assignmentService.GetSubmissionsByAssignmentAsync(assignmentId);
        return Ok(submissions);
    }
    [HttpPost("submit")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<SubmissionListDto>> Submit([FromBody] CreateSubmissionRequestDto dto)
    {
        try
        {
            var submit = await _assignmentService.SubmitAssignmentAsync(dto, CurrentUserId);
            return Ok(submit);
        }
        catch(Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpPost("submissions/{submissionId:int}/grades")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<SubmissionListDto>> Grade(int submissionId, [FromBody] GradeSubmissionRequestDto dto)
    {
        try
        {
            var grade = await _assignmentService.GradeSubmissionAsync(submissionId, dto, CurrentUserId);
            return Ok(grade);
        }
        catch(Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
