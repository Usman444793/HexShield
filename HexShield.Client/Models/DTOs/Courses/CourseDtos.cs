using System.ComponentModel.DataAnnotations;

namespace HexShield.Models.DTOs.Courses;

public record CourseListDto(
    int Id,
    string CourseCode,
    string Title,
    string? Description,
    bool IsPublished,
    int PrimaryTeacherProfileId,
    int OrganizationNodeId
);

public record CourseDetailsDto(
    int Id,
    int TenantId,
    string CourseCode,
    string Title,
    string? Description,
    bool IsPublished,
    int PrimaryTeacherProfileId,
    int OrganizationNodeId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

public record CreateCourseRequestDto(
    [Required] string CourseCode,
    [Required] string Title,
    string? Description,
    [Required] int PrimaryTeacherProfileId,
    [Required] int OrganizationNodeId
);

public record UpdateCourseRequestDto(
    [Required] string CourseCode,
    [Required] string Title,
    string? Description,
    [Required] int PrimaryTeacherProfileId,
    [Required] int OrganizationNodeId
);

public record PublishCourseRequestDto(bool IsPublished);
