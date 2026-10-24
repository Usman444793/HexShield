using HexShield.Data;
using HexShield.Infrastructure.Tenancy;
using HexShield.Models.Academic;
using HexShield.Shared.DTOs;
using HexShield.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace HexShield.Services;

public class AssignmentService : IAssignmentService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AssignmentService> _logger;

    public AssignmentService(ApplicationDbContext context, ITenantContext tenantContext, ILogger<AssignmentService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<IEnumerable<AssignmentListDto>> GetAssignmentsByCourseAsync(int courseId)
    {
        return await _context.Assignments
            .AsNoTracking()
            .Where(a => a.CourseId == courseId)
            .OrderBy(a => a.DueDate)
            .Select(a => new AssignmentListDto(
                a.Id,
                a.CourseId,
                a.LessonId,
                a.Title,
                a.DueDate,
                a.MaxScore,
                a.IsPublished,
                a.Submissions.Count(s => !s.IsDeleted)
            ))
            .ToListAsync();
    }

    public async Task<AssignmentDetailDto?> GetAssignmentByIdAsync(int id)
    {
        return await _context.Assignments
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AssignmentDetailDto(
                a.Id,
                a.CourseId,
                a.LessonId,
                a.Title,
                a.Description ?? string.Empty,
                a.DueDate,
                a.MaxScore,
                a.IsPublished,
                a.CreatedAt
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<AssignmentDetailDto> CreateAssignmentAsync(CreateAssignmentRequestDto dto, string userId)
    {
        var course = await _context.Courses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == dto.CourseId && !c.IsDeleted)
            ?? throw new KeyNotFoundException($"Course #{dto.CourseId} was not found.");

        var tenantId = _tenantContext.TenantId > 0 ? _tenantContext.TenantId : course.TenantId;
        var lessonId = dto.LessonId.HasValue && dto.LessonId.Value > 0 ? dto.LessonId : null;

        var assignment = new Assignment
        {
            TenantId = tenantId,
            CourseId = dto.CourseId,
            LessonId = lessonId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            DueDate = dto.DueDate,
            MaxScore = dto.MaxScore > 0 ? dto.MaxScore : 100,
            IsPublished = true,
            CreatedById = userId
        };

        _context.Assignments.Add(assignment);
        await _context.SaveChangesAsync();

        return new AssignmentDetailDto(
            assignment.Id,
            assignment.CourseId,
            assignment.LessonId,
            assignment.Title,
            assignment.Description ?? string.Empty,
            assignment.DueDate,
            assignment.MaxScore,
            assignment.IsPublished,
            assignment.CreatedAt
        );
    }

    public async Task<AssignmentDetailDto> UpdateAssignmentAsync(int id, UpdateAssignmentRequestDto dto, string userId)
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
        return await _context.Submissions
            .AsNoTracking()
            .Where(s => s.AssignmentId == assignmentId)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new SubmissionListDto(
                s.Id,
                s.AssignmentId,
                s.StudentId,
                s.Student != null && !string.IsNullOrEmpty(s.Student.FullName) ? s.Student.FullName : "Student",
                s.SubmittedAt,
                s.Grade,
                s.Assignment != null ? s.Assignment.MaxScore : 100m,
                s.Status,
                s.AttachmentUrl,
                s.Content,
                s.Feedback
            ))
            .ToListAsync();
    }

    public async Task<SubmissionListDto?> GetStudentSubmissionAsync(int assignmentId, string studentId)
    {
        return await _context.Submissions
            .AsNoTracking()
            .Where(s => s.StudentId == studentId && s.AssignmentId == assignmentId)
            .Select(s => new SubmissionListDto(
                s.Id,
                s.AssignmentId,
                s.StudentId,
                s.Student != null && !string.IsNullOrEmpty(s.Student.FullName) ? s.Student.FullName : "Student",
                s.SubmittedAt,
                s.Grade,
                s.Assignment != null ? s.Assignment.MaxScore : 100m,
                s.Status,
                s.AttachmentUrl,
                s.Content,
                s.Feedback
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<SubmissionListDto> SubmitAssignmentAsync(CreateSubmissionRequestDto dto, string studentId)
    {
        var assignment = await _context.Assignments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == dto.AssignmentId && !a.IsDeleted)
            ?? throw new KeyNotFoundException($"Assignment #{dto.AssignmentId} was not found.");

        var tenantId = _tenantContext.TenantId > 0 ? _tenantContext.TenantId : assignment.TenantId;

        var existing = await _context.Submissions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.AssignmentId == dto.AssignmentId && s.StudentId == studentId && !s.IsDeleted);

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
                TenantId = tenantId,
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

        var studentName = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Id == studentId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync() ?? "Student";

        return new SubmissionListDto(
            existing.Id,
            existing.AssignmentId,
            existing.StudentId,
            studentName,
            existing.SubmittedAt,
            existing.Grade,
            assignment.MaxScore,
            existing.Status,
            existing.AttachmentUrl,
            existing.Content,
            existing.Feedback
        );
    }

    public async Task<SubmissionListDto> GradeSubmissionAsync(int submissionId, GradeSubmissionRequestDto dto, string teacherId)
    {
        var submission = await _context.Submissions
            .Include(s => s.Assignment)
            .Include(s => s.Student)
            .FirstOrDefaultAsync(s => s.Id == submissionId);

        if (submission == null)
            throw new KeyNotFoundException("Submission not found");

        if (dto.Grade < 0 || dto.Grade > submission.Assignment.MaxScore)
            throw new InvalidOperationException($"Grade must be between 0 and {submission.Assignment.MaxScore}");

        submission.Grade = dto.Grade;
        submission.Feedback = dto.Feedback;
        submission.Status = SubmissionStatus.Graded;
        submission.UpdatedById = teacherId;

        await _context.SaveChangesAsync();

        return new SubmissionListDto(
            submission.Id,
            submission.AssignmentId,
            submission.StudentId,
            submission.Student?.FullName ?? "Student",
            submission.SubmittedAt,
            submission.Grade,
            submission.Assignment.MaxScore,
            submission.Status,
            submission.AttachmentUrl,
            submission.Content,
            submission.Feedback
        );
    }
}
