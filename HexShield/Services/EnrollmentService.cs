using HexShield.Data;
using HexShield.Infrastructure.Tenancy;
using HexShield.Models.Academic;
using HexShield.Models.DTOs.Enrollments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HexShield.Services;
public class EnrollmentService : IEnrollmentService
{
    private readonly ApplicationDbContext _context;
    private readonly TenantContext _tenantContext;
    private ILogger<EnrollmentService> _logger;
    public EnrollmentService(ApplicationDbContext context,TenantContext tenantContext,ILogger<EnrollmentService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }
    public async Task<IEnumerable<EnrollmentListDto>> GetEnrollmentsByCourse(int courseId)
    {
        return await _context.Enrollments.AsNoTracking().Where(e => e.CourseId == courseId)
       .Select(e => new EnrollmentListDto(
           e.Id,
           e.CourseId,
           e.Course.CourseCode,
           e.Course.Title,
           e.StudentId,
           e.Student.FullName.Trim(),
           e.Student.Email ?? string.Empty,
           e.EnrolledAt,
           e.Status
       ))
       .ToListAsync();
    }
    public async Task<IEnumerable<EnrollmentListDto>> GetEnrollmentsByStudent(string studentId)
    {
        return await _context.Enrollments.AsNoTracking().Where(e => e.StudentId == studentId)
        .Select(e => new EnrollmentListDto(
            e.Id,
            e.CourseId,
            e.Course.CourseCode,
            e.Course.Title,
            e.StudentId,
            e.Student.FullName.Trim(),
            e.Student.Email ?? string.Empty,
            e.EnrolledAt,
            e.Status
            )).ToListAsync();
    }
    public async Task<EnrollmentListDto?> GetEnrollmentByIdAsync(int id)
    {
        return await _context.Enrollments.AsNoTracking().Where(e => e.Id == id)
        .Select(e => new EnrollmentListDto(
            e.Id,
            e.CourseId,
            e.Course.CourseCode,
            e.Course.Title,
            e.StudentId,
            e.Student.FullName.Trim(),
            e.Student.Email ?? string.Empty,
            e.EnrolledAt,
            e.Status
            )).FirstOrDefaultAsync();
    }
    public async Task<EnrollmentListDto> EnrollStudentAsync(CreateEnrollmentRequestDto dto,string actionUserId)
    {
        var exists = await _context.Enrollments.AnyAsync(e => e.CourseId == dto.CourseId && e.StudentId == dto.StudentId);
        if (exists)
            throw new InvalidOperationException("Student is already enrolled in this course");
        var enrollment = new Enrollment
        {
            TenantId = _tenantContext.TenantId,
            CourseId = dto.CourseId,
            StudentId = dto.StudentId,
            EnrolledAt = DateTime.UtcNow,
            Status = EnrollmentStatus.Active,
            CreatedById = actionUserId
        };
        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Student {dto.StudentId} enrolled in Course {dto.CourseId} by {actionUserId}");
        return (await GetEnrollmentByIdAsync(enrollment.Id))!;
    }
    public async Task<EnrollmentListDto> UpdateStatusAsync(int id,EnrollmentStatus status,string actionUserId)
    {
        var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == id);
        if (enrollment == null)
            throw new KeyNotFoundException("Enrollment record not found");
        enrollment.Status = status;
        enrollment.UpdatedById = actionUserId;
        await _context.SaveChangesAsync();
        return (await GetEnrollmentByIdAsync(enrollment.Id))!;
    }
    public async Task<bool> UnenrollStudentAsync(int id,string actionUserId)
    {
        var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == id);
        if (enrollment == null) return false;
        _context.Enrollments.Remove(enrollment);
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<bool> IsStudentEnrolledAsync(int courseId,string studentId)
    {
        return await _context.Enrollments.AnyAsync(e => e.CourseId == courseId && e.StudentId == studentId && e.Status == EnrollmentStatus.Active);
    }
}
