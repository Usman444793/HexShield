namespace HexShield.Models.Quizzes;

public record QuizDtos
(
    int Id,
    int CourseId,
    int LessonId,
    string Title,
    int TimeLimitMinutes,
    int MaxAttempts,
    decimal PassPercentage,
    bool IsPublished,
    int QuestionCount
);
public record QuizOptionDto(int Id,string Text,bool isCorrect);
public record QuizQuestionDto(int Id,string Text,decimal Points,int SortOrder,List<QuizOptionDto> Options);
public record QuizDetailsDto
(
    int Id,
    int CourseId,
    int? LessonId,
    string Title,
    string? Instructions,
    int TimeLimitMinutes,
    int MaxAttempts,
    decimal PassPercentage,
    bool IsPublished,
    List<QuizQuestionDto> Questions
);
public record CreateQuizRequestDto
(

);
