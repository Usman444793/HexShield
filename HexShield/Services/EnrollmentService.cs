using HexShield.Data;
using HexShield.Infrastructure.Tenancy;
using HexShield.Models.Academic;
using HexShield.Shared.DTOs;
using HexShield.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace HexShield.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(ApplicationDbContext context, ITenantContext tenantContext, ILogger<EnrollmentService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<IEnumerable<EnrollmentListDto>> GetEnrollmentsByCourse(int courseId)
    {
        return await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Select(e => new EnrollmentListDto(
                e.Id,
                e.CourseId,
                e.Course.CourseCode,
                e.Course.Title,
                e.StudentProfile.UserId,
                e.StudentProfile.User.FullName,
                e.StudentProfile.User.Email ?? string.Empty,
                e.EnrolledAt,
                e.Status
            ))
            .ToListAsync();
    }

    public async Task<IEnumerable<EnrollmentListDto>> GetEnrollmentsByStudent(string studentId)
    {
        return await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentProfile.UserId == studentId || e.StudentProfileId.ToString() == studentId)
            .Select(e => new EnrollmentListDto(
                e.Id,
                e.CourseId,
                e.Course.CourseCode,
                e.Course.Title,
                e.StudentProfile.UserId,
                e.StudentProfile.User.FullName,
                e.StudentProfile.User.Email ?? string.Empty,
                e.EnrolledAt,
                e.Status
            ))
            .ToListAsync();
    }

    public async Task<EnrollmentListDto?> GetEnrollmentByIdAsync(int id)
    {
        return await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EnrollmentListDto(
                e.Id,
                e.CourseId,
                e.Course.CourseCode,
                e.Course.Title,
                e.StudentProfile.UserId,
                e.StudentProfile.User.FullName,
                e.StudentProfile.User.Email ?? string.Empty,
                e.EnrolledAt,
                e.Status
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<EnrollmentListDto> EnrollStudentAsync(CreateEnrollmentRequestDto dto, string actionUserId)
    {
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == dto.CourseId);
        if (!courseExists)
            throw new KeyNotFoundException("Course not found.");

        var studentProfile = await _context.StudentProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == dto.StudentId || sp.Id.ToString() == dto.StudentId);

        if (studentProfile == null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.StudentId);
            if (user == null)
                throw new KeyNotFoundException("Student user not found.");

            studentProfile = new StudentProfile
            {
                UserId = user.Id,
                StudentNumber = $"STU-{DateTimeOffset.UtcNow.Year}-{Random.Shared.Next(10000, 99999)}",
                Department = "General",
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedById = actionUserId
            };
            _context.StudentProfiles.Add(studentProfile);
            await _context.SaveChangesAsync();
        }

        var exists = await _context.Enrollments
            .AnyAsync(e => e.CourseId == dto.CourseId && e.StudentProfileId == studentProfile.Id);
        if (exists)
            throw new InvalidOperationException("Student is already enrolled in this course.");

        var enrollment = new Enrollment
        {
            TenantId = _tenantContext.TenantId,
            CourseId = dto.CourseId,
            StudentProfileId = studentProfile.Id,
            EnrolledAt = DateTime.UtcNow,
            Status = EnrollmentStatus.Active,
            CreatedById = actionUserId
        };

        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Student {StudentId} enrolled in Course {CourseId} by {ActionUserId}", dto.StudentId, dto.CourseId, actionUserId);
        return (await GetEnrollmentByIdAsync(enrollment.Id))!;
    }

    public async Task<EnrollmentListDto> UpdateStatusAsync(int id, EnrollmentStatus status, string actionUserId)
    {
        var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == id);
        if (enrollment == null)
            throw new KeyNotFoundException("Enrollment record not found.");

        enrollment.Status = status;
        enrollment.UpdatedById = actionUserId;
        await _context.SaveChangesAsync();

        return (await GetEnrollmentByIdAsync(enrollment.Id))!;
    }

    public async Task<bool> UnenrollStudentAsync(int id, string actionUserId)
    {
        var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == id);
        if (enrollment == null) return false;

        enrollment.UpdatedById = actionUserId;
        _context.Enrollments.Remove(enrollment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsStudentEnrolledAsync(int courseId, string studentId)
    {
        return await _context.Enrollments.AnyAsync(e =>
            e.CourseId == courseId &&
            (e.StudentProfile.UserId == studentId || e.StudentProfileId.ToString() == studentId) &&
            e.Status == EnrollmentStatus.Active);
    }
}
