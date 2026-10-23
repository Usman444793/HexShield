using HexShield.Shared.DTOs;

namespace HexShield.Services;

public interface IProgressAndAttendanceService
{
    Task<IEnumerable<AttendanceRecordDto>> GetAttendanceByCourseAsync(int courseId, DateTime? date);
    Task<bool> BulkMarkAttendanceAsync(BulkMarkAttendanceRequestDto dto, string actionUserId);
    Task<bool> MarkLessonCompletedAsync(int lessonId, string studentId);
    Task<IEnumerable<int>> GetCompletedLessonIdsAsync(int courseId, string studentId);
    Task<StudentCourseProgressDto> GetStudentProgressInCourseAsync(int courseId, string studentId);
    Task<IEnumerable<StudentCourseProgressDto>> GetCourseProgressSummaryAsync(int courseId);
}