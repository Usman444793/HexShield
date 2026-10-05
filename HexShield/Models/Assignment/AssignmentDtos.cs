using HexShield.Models.Academic;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HexShield.Models.DTOs.Assignment;

public record AssignmentListDto
(
    int Id,
    int CourseId,
    int LessonId,
    string Title,
    DateTime DueDate,
    decimal MaxScore,
    bool IsPublished,
    int SubmissionCount
);
public record AssignmentDetailDto
(
    int Id,
    int CourseId,
    int LessonId,
    string Title,
    string Description,
    DateTime DueDate,
    decimal MaxScore,
    bool IsPublished,
    DateTimeOffset CreatedAt
);
public record CreateAssignmentRequestDto
(
    int CourseId,
    int LessonId,
    string Title,
    string Description,
    DateTime DueDate,
    decimal MaxScore
);
public record UpdateAssignmentRequestDto
(
    string Title,
    string Description,
    DateTime DueDate,
    decimal MaxScore,
    bool IsPublished
);
public record SubmissionListDto
(
    int Id,
    int AssignmentId,
    string StudentId,
    string StudentName,
    DateTime SubmittedAt,
    decimal Grade,
    decimal MaxScore,
    SubmissionStatus status,
    string? AttachmentUrl
);
public record CreateSubmissionRequestDto
(
    int AssignmentId,
    string? Content,
    string? AttachmentUrl
);
public record GradeSubmissionRequestDto
(
    decimal Grade,
    string? Feedback
);