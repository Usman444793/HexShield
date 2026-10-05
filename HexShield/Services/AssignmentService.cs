using HexShield.Data;
using HexShield.Infrastructure.Tenancy;
using HexShield.Models.Academic;
using HexShield.Models.DTOs.Assignment;
using Microsoft.EntityFrameworkCore;
namespace HexShield.Services;
public class AssignmentService : IAssignmentService
{
    private readonly ApplicationDbContext _context;
    private readonly TenantContext _tenantContext;
    private ILogger<AssignmentService> _logger;
    public AssignmentService(ApplicationDbContext context,TenantContext tenantContext,ILogger<AssignmentService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }
    public async Task<IEnumerable<AssignmentListDto>> GetAssignmentsByCourseAsync(int courseId)
    {
        return await _context.Assignments.AsNoTracking().Where(a => a.CourseId == courseId)
        .Select(a => new AssignmentListDto(
            a.Id,
            a.CourseId,
            a.LessonId,
            a.Title,
            a.DueDate,
            a.MaxScore,
            a.IsPublished,
            a.Submissions.Count
            )).ToListAsync();
    }
    public async Task<AssignmentDetailDto?> GetAssignmentByIdAsync(int id)
    {
        return await _context.Assignments.AsNoTracking().Where(a => a.Id == id)
        .Select(a => new AssignmentDetailDto(
            a.Id,
            a.CourseId,
            a.LessonId,
            a.Title,
            a.Description!,
            a.DueDate,
            a.MaxScore,
            a.IsPublished,
            a.CreatedAt
            )).FirstOrDefaultAsync();
    }
    public async Task<AssignmentDetailDto> CreateAssignmentAsync(CreateAssignmentRequestDto dto,string userId)
    {
        var assignment = new Assignment
        {
            TenantId = _tenantContext.TenantId,
            CourseId = dto.CourseId,
            LessonId = dto.LessonId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            DueDate = dto.DueDate,
            MaxScore = dto.MaxScore,
            IsPublished = false,
            CreatedById = userId
        };
        _context.Assignments.Add(assignment);
        await _context.SaveChangesAsync();
        return (await GetAssignmentByIdAsync(assignment.Id))!;
    }
    public async Task<AssignmentDetailDto> UpdateAssignmentAsync(int id,UpdateAssignmentRequestDto dto,string userId)
    {
        var assignment = await _context.Assignments.FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null)
            throw new KeyNotFoundException("Assignment not found");
        assignment.Title = dto.Title.Trim();
        assignment.Description = dto.Description?.Trim();
        assignment.DueDate = dto.DueDate;
        assignment.MaxScore = dto.MaxScore;
        assignment.IsPublished = dto.IsPublished;
        assignment.UpdatedById = userId;
        await _context.SaveChangesAsync();
        return (await GetAssignmentByIdAsync(assignment.Id))!;
    }
    public async Task<bool> DeleteAssignmentAsync(int id, string userId)
    {
        var assignment = await _context.Assignments.FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return false;
        _context.Assignments.Remove(assignment);
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<IEnumerable<SubmissionListDto>> GetSubmissionsByAssignmentAsync(int assignmentId) 
    {
        return await _context.Submissions.AsNoTracking().Where(s => s.AssignmentId == assignmentId)
        .Select(s => new SubmissionListDto(
            s.Id,
            s.AssignmentId,
            s.StudentId,
            s.Student.FullName.Trim(),
            s.SubmittedAt,
            s.Grade,
            s.Assignment.MaxScore,
            s.Status,
            s.AttachmentUrl
            )).ToListAsync();
    }
    public async Task<SubmissionListDto?> GetStudentSubmissionAsync(int assignmentId,string studentId)
    {
        return await _context.Submissions.Where(s => s.StudentId == studentId && s.AssignmentId == assignmentId)
        .Select(s => new SubmissionListDto(
            s.Id,
            s.AssignmentId,
            s.StudentId,
            s.Student.FullName.Trim(),
            s.SubmittedAt,
            s.Grade,
            s.Assignment.MaxScore,
            s.Status,
            s.AttachmentUrl
            )).FirstOrDefaultAsync();
    }
    public async Task<SubmissionListDto> SubmitAssignmentAsync(CreateSubmissionRequestDto dto,string studentId)
    {
        var existing = await _context.Submissions.FirstOrDefaultAsync(s => s.AssignmentId == dto.AssignmentId && s.StudentId == studentId);
        if (existing != null)
        {
            existing.Content = dto.Content;
            existing.AttachmentUrl = dto.AttachmentUrl;
            existing.SubmittedAt = DateTime.UtcNow;
            existing.Status = SubmissionStatus.Submitted;
            existing.UpdatedById = studentId; 
        }
        else
        {
            existing = new Submission
            {
                TenantId = _tenantContext.TenantId,
                AssignmentId = dto.AssignmentId,
                StudentId = studentId,
                Content = dto.Content,
                AttachmentUrl = dto.AttachmentUrl,
                SubmittedAt = DateTime.UtcNow,
                Status = SubmissionStatus.Submitted,
                CreatedById = studentId
            };
            _context.Submissions.Add(existing);
        }
        await _context.SaveChangesAsync();
        return (await GetSubmissionsByAssignmentAsync(dto.AssignmentId)).First(s => s.StudentId == studentId);
    }
    public async Task<SubmissionListDto> GradeSubmissionAsync(int submissionId,GradeSubmissionRequestDto dto,string teacherId)
    {
        var submission = await _context.Submissions.Include(s => s.Assignment).FirstOrDefaultAsync(s => s.Id == submissionId);
        if (submission == null)
            throw new KeyNotFoundException("Submission not found");
        if (dto.Grade < 0 || dto.Grade > submission.Assignment.MaxScore)
            throw new InvalidOperationException($"Grade must be between 0 and {submission.Assignment.MaxScore}");
        submission.Grade = dto.Grade;
        submission.Feedback = dto.Feedback;
        submission.Status = SubmissionStatus.Graded;
        submission.UpdatedById = teacherId;
            return (await GetSubmissionsByAssignmentAsync(submission.AssignmentId)).First(s => s.Id == submissionId);

    }
}
