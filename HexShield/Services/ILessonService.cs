using HexShield.Shared.DTOs;

namespace HexShield.Services;

public interface ILessonService
{
    Task<IEnumerable<LessonListDto>> GetLessonByCourseAsync(int courseId);
    Task<LessonDetailDto?> GetLessonByIdAsync(int id);
    Task<LessonDetailDto> CreateLessonAsync(CreateLessonRequestDto dto, string userId);
    Task<LessonDetailDto> UpdateLessonAsync(int id, UpdateLessonRequestDto dto, string userId);
    Task<bool> DeleteLessonAsync(int id, string userId);
    Task<LessonDetailDto> SetPublishedStatusAsync(int id, bool isPublished, string userId);
    Task UpdateSortOrderAsync(int courseId, IEnumerable<ReorderLessonDto> reorderDto, string userId);
}
