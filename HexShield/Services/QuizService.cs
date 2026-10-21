using HexShield.Data;
using HexShield.Infrastructure.Tenancy;
using HexShield.Models.Academic;
using HexShield.Models.Quizzes;
using HexShield.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HexShield.Services;

public class QuizService : IQuizService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<QuizService> _logger;

    public QuizService(ApplicationDbContext context, ITenantContext tenantContext, ILogger<QuizService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<IEnumerable<QuizListDto>> GetQuizzesByCourseAsync(int courseId)
    {
        return await _context.Quizzes
            .AsNoTracking()
            .Where(q => q.CourseId == courseId)
            .Select(q => new QuizListDto(
                q.Id,
                q.CourseId,
                q.LessonId,
                q.Title,
                q.TimeLimitMinutes,
                q.MaxAttempts,
                q.PassPercentage,
                q.IsPublished,
                q.Questions.Count
            ))
            .ToListAsync();
    }

    public async Task<QuizDetailsDto?> GetQuizById(int id)
    {
        return await _context.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
                .ThenInclude(qn => qn.Options)
            .Where(q => q.Id == id)
            .Select(q => new QuizDetailsDto(
                q.Id,
                q.CourseId,
                q.LessonId,
                q.Title,
                q.Instructions,
                q.TimeLimitMinutes,
                q.MaxAttempts,
                q.PassPercentage,
                q.IsPublished,
                q.Questions.OrderBy(qn => qn.SortOrder).Select(qn => new QuizQuestionDto(
                    qn.Id,
                    qn.QuizId,
                    qn.Text,
                    qn.Marks,
                    qn.SortOrder,
                    qn.Options.OrderBy(o => o.SortOrder).Select(o => new QuizOptionDto(o.Id, o.Text, o.IsCorrect)).ToList()
                )).ToList()
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<QuizDetailsDto> CreateQuizAsync(CreateQuizRequestDto dto, string userId)
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("Tenant Context Required");

        var quiz = new Quiz
        {
            TenantId = _tenantContext.TenantId,
            CourseId = dto.CourseId,
            LessonId = dto.LessonId,
            Title = dto.Title.Trim(),
            Instructions = dto.Instructions?.Trim() ?? string.Empty,
            TimeLimitMinutes = dto.TimeLimitMinutes,
            MaxAttempts = dto.MaxAttempts,
            PassPercentage = dto.PassPercentage,
            CreatedById = userId
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();
        return (await GetQuizById(quiz.Id))!;
    }

    public async Task<bool> DeleteQuizAsync(int id, string userId)
    {
        var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.Id == id);
        if (quiz == null) return false;

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<QuizDetailsDto> AddQuestionToQuizAsync(int quizId, QuizQuestionDto dto, string userId)
    {
        var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz == null) throw new KeyNotFoundException("Quiz not found.");

        var question = new Question
        {
            QuizId = quizId,
            Text = dto.Text.Trim(),
            Marks = (int)dto.Points,
            SortOrder = dto.SortOrder,
            CreatedById = userId,
            Options = dto.Options.Select((o, index) => new QuestionOption
            {
                Text = o.Text.Trim(),
                IsCorrect = o.IsCorrect,
                SortOrder = index + 1,
                CreatedById = userId
            }).ToList()
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();
        return (await GetQuizById(quizId))!;
    }

    public async Task<QuizAttemptResultDto> StartAttemptAsync(int quizId, string studentId)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Attempts)
            .FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz == null) throw new KeyNotFoundException("Quiz not found.");

        var completedAttempts = quiz.Attempts.Count(a => a.StudentId == studentId && a.CompletedAt != null);
        if (completedAttempts >= quiz.MaxAttempts)
            throw new InvalidOperationException("Maximum quiz attempts reached.");

        var studentProfile = await _context.StudentProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == studentId || sp.Id.ToString() == studentId);

        if (studentProfile == null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == studentId);
            if (user != null)
            {
                studentProfile = new StudentProfile
                {
                    UserId = user.Id,
                    StudentNumber = $"STU-{DateTimeOffset.UtcNow.Year}-{Random.Shared.Next(10000, 99999)}",
                    Department = "General",
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedById = studentId
                };
                _context.StudentProfiles.Add(studentProfile);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new KeyNotFoundException("Student user not found.");
            }
        }

        var attempt = new QuizAttempt
        {
            TenantId = _tenantContext.TenantId,
            QuizId = quizId,
            StudentId = studentId,
            StudentProfileId = studentProfile.Id,
            StartedAt = DateTime.UtcNow,
            CreatedById = studentId
        };

        _context.QuizAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        return new QuizAttemptResultDto(
            attempt.Id,
            quizId,
            studentId,
            attempt.StartedAt,
            null,
            0m,
            0m,
            0m,
            false
        );
    }

    public async Task<QuizAttemptResultDto> SubmitAttemptAsync(SubmitQuizAttemptRequestDto dto, string studentId)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(qn => qn.Options)
            .FirstOrDefaultAsync(q => q.Id == dto.QuizId);
        if (quiz == null) throw new KeyNotFoundException("Quiz not found.");

        var attempt = await _context.QuizAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.QuizId == dto.QuizId && a.StudentId == studentId && a.CompletedAt == null);
        if (attempt == null)
            throw new InvalidOperationException("No active attempt was found for this quiz.");

        decimal scoreAchieved = 0m;
        decimal totalPossibleScore = quiz.Questions.Sum(q => (decimal)q.Marks);

        foreach (var answerDto in dto.Answers)
        {
            var question = quiz.Questions.FirstOrDefault(q => q.Id == answerDto.QuestionId);
            if (question == null) continue;

            var selectedOption = question.Options.FirstOrDefault(o => o.Id == answerDto.SelectedOptionId);
            bool isCorrect = selectedOption != null && selectedOption.IsCorrect;
            if (isCorrect) scoreAchieved += question.Marks;

            attempt.Answers.Add(new QuizAnswer
            {
                QuestionId = question.Id,
                SelectedOptionId = answerDto.SelectedOptionId,
                IsCorrect = isCorrect,
                AwardedMarks = isCorrect ? question.Marks : 0,
                IsGraded = true
            });
        }

        decimal percentage = totalPossibleScore > 0 ? (scoreAchieved / totalPossibleScore) * 100 : 0;
        attempt.CompletedAt = DateTime.UtcNow;
        attempt.Score = scoreAchieved;
        attempt.TotalPossibleScore = totalPossibleScore;
        attempt.IsCompleted = true;
        attempt.Passed = percentage >= quiz.PassPercentage;
        attempt.UpdatedById = studentId;

        await _context.SaveChangesAsync();

        return new QuizAttemptResultDto(
            attempt.Id,
            attempt.QuizId,
            attempt.StudentId,
            attempt.StartedAt,
            attempt.CompletedAt,
            scoreAchieved,
            totalPossibleScore,
            percentage,
            attempt.Passed
        );
    }

    public async Task<IEnumerable<QuizAttemptResultDto>> GetStudentAttemptsAsync(int quizId, string studentId)
    {
        return await _context.QuizAttempts
            .AsNoTracking()
            .Where(q => q.QuizId == quizId && q.StudentId == studentId)
            .Select(a => new QuizAttemptResultDto(
                a.Id,
                a.QuizId,
                a.StudentId,
                a.StartedAt,
                a.CompletedAt,
                a.Score,
                a.TotalPossibleScore,
                a.TotalPossibleScore > 0 ? (a.Score / a.TotalPossibleScore) * 100 : 0,
                a.Passed
            ))
            .ToListAsync();
    }
}