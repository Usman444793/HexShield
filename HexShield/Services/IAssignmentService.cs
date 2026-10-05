using HexShield.Models.DTOs.Assignment;
namespace HexShield.Services;
public interface IAssignmentService
{
    Task<IEnumerable<AssignmentListDto>> GetAssignmentsByCourseAsync(int courseId);
    Task<AssignmentDetailDto?> GetAssignmentByIdAsync(int id);
    Task<AssignmentDetailDto> CreateAssignmentAsync(CreateAssignmentRequestDto dto, string userId);
    Task<AssignmentDetailDto> UpdateAssignmentAsync(int id, UpdateAssignmentRequestDto dto, string userId);
    Task<bool> DeleteAssignmentAsync(int id, string userId);
    // Submissions
    Task<IEnumerable<SubmissionListDto>> GetSubmissionsByAssignmentAsync(int assignmentId);
    Task<SubmissionListDto?> GetStudentSubmissionAsync(int assignmentId, string studentId);
    Task<SubmissionListDto> SubmitAssignmentAsync(CreateSubmissionRequestDto dto, string studentId);
    Task<SubmissionListDto> GradeSubmissionAsync(int submissionId, GradeSubmissionRequestDto dto, string teacherId);
}