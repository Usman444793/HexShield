using HexShield.Models.DTOs.Courses;
namespace HexShield.Services;
public interface ICourseService
{
    Task<IEnumerable<CourseListDto>> GetCoursesAsync();
    Task<CourseDetailsDto?> GetCourseByIdAsync(int id);
    Task<CourseDetailsDto> CreateCourseAsync(CreateCourseRequestDto dto, string userId);
    Task<CourseDetailsDto> UpdateCourseAsync(int id,UpdateCourseRequestDto dto, string userId);
    Task<bool> DeleteCourseAsync(int id,string userId);
    Task<CourseDetailsDto> SetPublishStatusAsync(int id, bool IsPublished, string UserId);
}