using HexShield.Client.Models.DTOs.Lesson;
using HexShield.Data;
using HexShield.Infrastructure.Tenancy;
using HexShield.Models.Academic;
using HexShield.Models.DTOs.Admin;
using HexShield.Models.DTOs.Courses;
using Microsoft.EntityFrameworkCore;

namespace HexShield.Services;
public class LessonService : ILessonService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<LessonService> _logger;
    public LessonService(ApplicationDbContext context, ITenantContext tenantContext, ILogger<LessonService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }
    public async Task<IEnumerable<LessonListDto>> GetLessonByCourseAsync(int courseId)
    {
         return await _context.Lessons.AsNoTracking().Where(l => l.CourseId == courseId).OrderBy(l => l.SortOrder)
        .Select(l => new LessonListDto(
                l.Id,
                l.CourseId,
                l.Title,
                l.VideoUrl,
                l.SortOrder,
                l.IsPublished
            ))
            .ToListAsync();
    }
    public async Task<LessonDetailDto?> GetLessonByIdAsync(int id)
    {
        return await _context.Lessons.AsNoTracking().Where(l => l.Id == id)
        .Select(l => new LessonDetailDto(
            l.Id,
            l.TenantId,
            l.CourseId,
            l.Title,
            l.Content,
            l.VideoUrl,
            l.SortOrder,
            l.IsPublished,
            l.CreatedAt,
            l.UpdatedAt ?? DateTimeOffset.UtcNow
            )).FirstOrDefaultAsync();
    }
    public async Task<LessonDetailDto> CreateLessonAsync(CreateLessonRequestDto dto, string userId)
    {
        if (!_tenantContext.HasTenant)
        {
            throw new InvalidOperationException("A valid tenant Context is required");
        }
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new ArgumentException("Lesson title is required");
        }
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == dto.CourseId);
        if (!courseExists)
        {
            throw new KeyNotFoundException("Associated course not found");
        }

        // avoid dereferencing nullable DTO properties
        var content = dto.Content?.Trim() ?? string.Empty;
        var videoUrl = string.IsNullOrWhiteSpace(dto.VideoUrl) ? null : dto.VideoUrl!.Trim();

        var lesson = new Lesson
        {
            TenantId = _tenantContext.TenantId,
            CourseId = dto.CourseId,
            Title = dto.Title,
            Content = content,
            VideoUrl = videoUrl,
            SortOrder = dto.SortOrder,
            IsPublished = false,
            CreatedById = userId
        };
        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Lesson {LessonId} created for Course {CourseId} by {UserId}", lesson.Id, dto.CourseId, userId);
        var created = await GetLessonByIdAsync(lesson.Id) ?? throw new InvalidOperationException("Failed to load created lesson");
        return created;
    }
    public async Task<LessonDetailDto> UpdateLessonAsync(int id, UpdateLessonRequestDto dto, string userId)
    {
        var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == id);
        if (lesson == null)
        {
            throw new InvalidOperationException("Lesson not found.");
        }
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new ArgumentException("Lesson title is required");
        }
        lesson.Title = dto.Title.Trim();
        lesson.Content = dto.Content?.Trim() ?? string.Empty;
        lesson.VideoUrl = dto.VideoUrl?.Trim() ?? string.Empty;
        lesson.SortOrder = dto.SortOrder;
        lesson.UpdatedById = userId;
        _context.Lessons.Update(lesson);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Lesson {LessonId} updated by {UserId}", lesson.Id, userId);
        return (await GetLessonByIdAsync(id))!;
    }
    public async Task<bool> DeleteLessonAsync(int id, string userId)
    {
        var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == id);
        if (lesson == null) return false;
        lesson.UpdatedById = userId;
        _context.Lessons.Remove(lesson);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Lesson {LessonId} deleted by {UserId}", lesson.Id, userId);
        return true;
    }
    public async Task<LessonDetailDto> SetPublishedStatusAsync(int id,bool isPublished,string userId)
    {
        var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == id);
        if (lesson == null)
        {
            throw new InvalidOperationException("Lesson not found.");
        }
        lesson.UpdatedById = userId;
        lesson.IsPublished = isPublished;
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Lesson {id} publication status changed to {isPublished} by {userId}");
        return (await GetLessonByIdAsync(id))!;
    }
    public async Task UpdateSortOrderAsync(int courseId,IEnumerable<ReorderLessonDto> reOrderDtos,string userId)
    {
        var lessonIds = reOrderDtos.Select(r => r.LessonId).ToList();
        var lessons = await _context.Lessons.Where(l => l.CourseId == courseId && lessonIds.Contains(l.Id)).ToListAsync();
        foreach (var lesson in lessons) 
        {
            var update = reOrderDtos.FirstOrDefault(r => r.LessonId == lesson.Id);
            lesson.SortOrder = update.NewSortOrder;
            lesson.UpdatedById = userId;
        }
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Re Ordered lesson by {courseId} by {userId}");
    }
}
 