using HexShield.Data;
using HexShield.Infrastructure.Tenancy;
using HexShield.Models.Academic;
using HexShield.Models.DTOs.Courses;
using Microsoft.EntityFrameworkCore;

namespace HexShield.Services;

public class CourseService : ICourseService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CourseService> _logger;

    public CourseService(ApplicationDbContext context, ITenantContext tenantContext, ILogger<CourseService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<IEnumerable<CourseListDto>> GetCoursesAsync()
    {
        return await _context.Courses
            .AsNoTracking()
            .OrderBy(c => c.CourseCode)
            .Select(c => new CourseListDto(
                c.Id,
                c.CourseCode,
                c.Title,
                c.Description,
                c.IsPublished,
                c.PrimaryTeacherProfileId,
                c.OrganizationNodeId
            ))
            .ToListAsync();
    }

    public async Task<CourseDetailsDto?> GetCourseByIdAsync(int id)
    {
        return await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseDetailsDto(
                c.Id,
                c.TenantId,
                c.CourseCode,
                c.Title,
                c.Description,
                c.IsPublished,
                c.PrimaryTeacherProfileId,
                c.OrganizationNodeId,
                c.CreatedAt,
                c.UpdatedAt
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<CourseDetailsDto> CreateCourseAsync(CreateCourseRequestDto dto, string userId)
    {
        if (!_tenantContext.HasTenant)
        {
            throw new InvalidOperationException("A valid tenant context is required");
        }

        if (string.IsNullOrWhiteSpace(dto.CourseCode))
            throw new ArgumentException("Course code is required.");
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("Course title is required.");

        var courseCode = dto.CourseCode.Trim().ToUpperInvariant();
        var codeExists = await _context.Courses.AnyAsync(c => c.CourseCode == courseCode);
        if (codeExists)
        {
            throw new InvalidOperationException($"Course Code '{courseCode}' already exists.");
        }

        var organizationExists = await _context.OrganizationNodes.AnyAsync(o => o.Id == dto.OrganizationNodeId);
        if (!organizationExists)
        {
            throw new KeyNotFoundException("The specified organization node does not exist.");
        }

        var teacherExists = await _context.TeacherProfiles.AnyAsync(t => t.Id == dto.PrimaryTeacherProfileId);
        if (!teacherExists)
        {
            throw new KeyNotFoundException("The specified teacher was not found.");
        }

        var course = new Course
        {
            TenantId = _tenantContext.TenantId,
            CourseCode = courseCode,
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            IsPublished = false,
            PrimaryTeacherProfileId = dto.PrimaryTeacherProfileId,
            OrganizationNodeId = dto.OrganizationNodeId,
            CreatedById = userId
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Course {CourseCode} created by {UserId} in tenant {TenantId}", course.CourseCode, userId, course.TenantId);

        return (await GetCourseByIdAsync(course.Id))!;
    }

    public async Task<CourseDetailsDto> UpdateCourseAsync(int id, UpdateCourseRequestDto dto, string userId)
    {
        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
        if (course == null)
            throw new KeyNotFoundException("Course not found.");

        if (string.IsNullOrWhiteSpace(dto.CourseCode))
            throw new ArgumentException("Course code is required.");
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("Course title is required.");

        var courseCode = dto.CourseCode.Trim().ToUpperInvariant();
        var duplicateExists = await _context.Courses.AnyAsync(c => c.Id != id && c.CourseCode == courseCode);
        if (duplicateExists)
        {
            throw new InvalidOperationException($"Course Code '{courseCode}' already exists.");
        }

        var teacherExists = await _context.TeacherProfiles.AnyAsync(t => t.Id == dto.PrimaryTeacherProfileId);
        if (!teacherExists)
        {
            throw new KeyNotFoundException("The specified teacher was not found.");
        }

        var organizationExists = await _context.OrganizationNodes.AnyAsync(o => o.Id == dto.OrganizationNodeId);
        if (!organizationExists)
        {
            throw new KeyNotFoundException("The specified organization node does not exist.");
        }

        course.CourseCode = courseCode;
        course.Title = dto.Title.Trim();
        course.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        course.PrimaryTeacherProfileId = dto.PrimaryTeacherProfileId;
        course.OrganizationNodeId = dto.OrganizationNodeId;
        course.UpdatedById = userId;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Course {Id} updated by {UserId}", id, userId);

        return (await GetCourseByIdAsync(id))!;
    }

    public async Task<bool> DeleteCourseAsync(int id, string userId)
    {
        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
        if (course == null) return false;

        course.UpdatedById = userId;
        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Course {Id} deleted by {UserId}", id, userId);

        return true;
    }

    public async Task<CourseDetailsDto> SetPublishStatusAsync(int id, bool isPublished, string userId)
    {
        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
        if (course == null) throw new KeyNotFoundException("Course not found.");

        course.IsPublished = isPublished;
        course.UpdatedById = userId;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Course {Id} publication status changed to {IsPublished} by {UserId}", id, isPublished, userId);

        return (await GetCourseByIdAsync(id))!;
    }
}