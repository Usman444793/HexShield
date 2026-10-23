using HexShield.Data;
using HexShield.Infrastructure.Tenancy;
using HexShield.Models.Academic;
using HexShield.Models.Progress;
using HexShield.Shared.DTOs;
using HexShield.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace HexShield.Services;

public class ProgressAndAttendanceService : IProgressAndAttendanceService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ProgressAndAttendanceService> _logger;

    public ProgressAndAttendanceService(
        ApplicationDbContext context,
        ITenantContext tenantContext,
        ILogger<ProgressAndAttendanceService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<IEnumerable<AttendanceRecordDto>> GetAttendanceByCourseAsync(int courseId, DateTime? date)
    {
        var query = _context.Attendances
            .AsNoTracking()
            .Where(a => a.CourseId == courseId);

        if (date.HasValue)
        {
            var targetDate = date.Value.Date;
            query = query.Where(a => a.SessionDate.Date == targetDate);
        }

        return await query.Select(a => new AttendanceRecordDto(
            a.Id,
            a.CourseId,
            a.LessonId,
            a.StudentId,
            a.Student.FullName,
            a.SessionDate,
            a.Status,
            a.Remarks
        )).ToListAsync();
    }

    public async Task<bool> BulkMarkAttendanceAsync(BulkMarkAttendanceRequestDto dto, string actionUserId)
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("Tenant context is required.");

        var date = dto.SessionDate.Date;

        var existingRecords = await _context.Attendances
            .Where(a => a.CourseId == dto.CourseId && a.SessionDate.Date == date)
            .ToListAsync();

        foreach (var record in dto.StudentRecords)
        {
            var existing = existingRecords.FirstOrDefault(a => a.StudentId == record.StudentId);
            if (existing != null)
            {
                existing.Status = record.Status;
                existing.Remarks = record.Remarks;
                existing.LessonId = dto.LessonId;
                existing.UpdatedById = actionUserId;
            }
            else
            {
                _context.Attendances.Add(new Attendance
                {
                    TenantId = _tenantContext.TenantId,
                    CourseId = dto.CourseId,
                    LessonId = dto.LessonId,
                    StudentId = record.StudentId,
                    SessionDate = date,
                    Status = record.Status,
                    Remarks = record.Remarks,
                    CreatedById = actionUserId
                });
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkLessonCompletedAsync(int lessonId, string studentId)
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("Tenant context required.");

        var studentProfile = await _context.StudentProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == studentId);

        if (studentProfile == null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == studentId);
            if (user == null) throw new KeyNotFoundException("Student not found.");

            studentProfile = new StudentProfile
            {
                UserId = studentId,
                StudentNumber = $"STU-{DateTimeOffset.UtcNow.Year}-{Random.Shared.Next(10000, 99999)}",
                Department = "General",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedById = studentId
            };
            _context.StudentProfiles.Add(studentProfile);
            await _context.SaveChangesAsync();
        }

        var existing = await _context.LessonProgresses
            .FirstOrDefaultAsync(p => p.LessonId == lessonId && p.StudentProfileId == studentProfile.Id);

        if (existing == null)
        {
            _context.LessonProgresses.Add(new LessonProgress
            {
                TenantId = _tenantContext.TenantId,
                LessonId = lessonId,
                StudentProfileId = studentProfile.Id,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow,
                CreatedById = studentId
            });
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<IEnumerable<int>> GetCompletedLessonIdsAsync(int courseId, string studentId)
    {
        return await _context.LessonProgresses
            .AsNoTracking()
            .Where(lp => lp.Lesson.CourseId == courseId && lp.StudentProfile.UserId == studentId && lp.IsCompleted)
            .Select(lp => lp.LessonId)
            .ToListAsync();
    }

    public async Task<StudentCourseProgressDto> GetStudentProgressInCourseAsync(int courseId, string studentId)
    {
        var student = await _context.Users.FirstOrDefaultAsync(u => u.Id == studentId);
        var studentName = student != null ? student.FullName : studentId;

        var totalLessons = await _context.Lessons.CountAsync(l => l.CourseId == courseId);

        var completedLessons = await _context.LessonProgresses
            .CountAsync(lp => lp.Lesson.CourseId == courseId && lp.StudentProfile.UserId == studentId && lp.IsCompleted);

        var totalAssignments = await _context.Assignments.CountAsync(a => a.CourseId == courseId);

        var submittedAssignments = await _context.Submissions
            .CountAsync(s => s.Assignment.CourseId == courseId && s.StudentId == studentId);

        var attendanceQuery = _context.Attendances
            .Where(a => a.CourseId == courseId && a.StudentId == studentId);

        var totalSessions = await attendanceQuery.CountAsync();
        var presentSessions = await attendanceQuery.CountAsync(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);

        decimal completionPct = totalLessons > 0 ? (decimal)completedLessons / totalLessons * 100 : 0;
        decimal attendancePct = totalSessions > 0 ? (decimal)presentSessions / totalSessions * 100 : 100;

        return new StudentCourseProgressDto(
            studentId,
            studentName,
            totalLessons,
            completedLessons,
            Math.Round(completionPct, 2),
            totalAssignments,
            submittedAssignments,
            Math.Round(attendancePct, 2)
        );
    }

    public async Task<IEnumerable<StudentCourseProgressDto>> GetCourseProgressSummaryAsync(int courseId)
    {
        var enrolledStudentIds = await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId && e.Status == EnrollmentStatus.Active)
            .Select(e => e.StudentProfile.UserId)
            .Distinct()
            .ToListAsync();

        var list = new List<StudentCourseProgressDto>();
        foreach (var studentId in enrolledStudentIds)
        {
            var progress = await GetStudentProgressInCourseAsync(courseId, studentId);
            list.Add(progress);
        }

        return list;
    }
}