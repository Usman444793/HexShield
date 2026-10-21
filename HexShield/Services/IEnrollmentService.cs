using HexShield.Shared.DTOs;
using HexShield.Shared.Enums;

namespace HexShield.Services;

public interface IEnrollmentService
{
    Task<IEnumerable<EnrollmentListDto>> GetEnrollmentsByCourse(int courseId);
    Task<IEnumerable<EnrollmentListDto>> GetEnrollmentsByStudent(string studentId);
    Task<EnrollmentListDto?> GetEnrollmentByIdAsync(int id);
    Task<EnrollmentListDto> EnrollStudentAsync(CreateEnrollmentRequestDto dto, string actionUserId);
    Task<EnrollmentListDto> UpdateStatusAsync(int id, EnrollmentStatus status, string actionUserId);
    Task<bool> UnenrollStudentAsync(int id, string actionUserId);
    Task<bool> IsStudentEnrolledAsync(int courseId, string studentId);
}
