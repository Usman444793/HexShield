using HexShield.Shared.DTOs;

namespace HexShield.Services;

public interface IQuizService
{
    Task<IEnumerable<QuizListDto>> GetQuizzesByCourseAsync(int courseId);
    Task<QuizDetailsDto?> GetQuizById(int id);
    Task<QuizDetailsDto> CreateQuizAsync(CreateQuizRequestDto dto, string userId);
    Task<bool> DeleteQuizAsync(int id, string userId);
    Task<QuizDetailsDto> AddQuestionToQuizAsync(int quizId, QuizQuestionDto dto, string userId);
    Task<QuizAttemptResultDto> StartAttemptAsync(int quizId, string studentId);
    Task<QuizAttemptResultDto> SubmitAttemptAsync(SubmitQuizAttemptRequestDto dto, string studentId);
    Task<IEnumerable<QuizAttemptResultDto>> GetStudentAttemptsAsync(int quizId, string studentId);
}