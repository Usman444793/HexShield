namespace HexShield.Client.Models.DTOs.Lesson;

public record LessonListDto
(
    int Id,
    int CourseId,
    string Title,
    string? VideoUrl,
    int sortOrder,
    bool isPublished
);
public record LessonDetailDto
(
    int id,
    int TenantId,
    int CourseId,
    string Title,
    string Content,
    string? VideoUrl,
    int SortOrder,
    bool IsPublished,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
public record CreateLessonRequestDto(
    int CourseId,
    string Title,
    string? Content,
    string? VideoUrl,
    int SortOrder
);
public record UpdateLessonRequestDto(
    string Title,
    string? Content,
    string? VideoUrl,
    int SortOrder
);
public record ReorderLessonDto(
    int LessonId,
    int NewSortOrder
);