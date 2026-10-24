using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HexShield.Shared.Enums;

namespace HexShield.Shared.DTOs;

#region Auth DTOs

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiration,
    string UserId,
    string Email,
    IEnumerable<string> Roles,
    int TenantId = 0
)
{
    // Alias property so client or server code reading ExpiresAt succeeds seamlessly
    public DateTimeOffset ExpiresAt => AccessTokenExpiration;
}

public class LoginRequestDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;

    public LoginRequestDto() { }
    public LoginRequestDto(string email, string password)
    {
        Email = email;
        Password = password;
    }
}

public class RegisterRequestDto
{
    [Required(ErrorMessage = "First name is required.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(12, ErrorMessage = "Password must be at least 12 characters with uppercase, lowercase, digit, and special character.")]
    public string Password { get; set; } = string.Empty;

    public int? TenantId { get; set; }
    public string? Department { get; set; }

    public RegisterRequestDto() { }
    public RegisterRequestDto(string firstName, string lastName, string email, string password, int? tenantId = null, string? department = null)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Password = password;
        TenantId = tenantId;
        Department = department;
    }
}

public record RefreshTokenRequestDto(
    string AccessToken,
    string RefreshToken
);

public record RevokeTokenRequestDto(
    string? RefreshToken = null
);

#endregion

#region Course DTOs

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

#endregion

#region Lesson DTOs

public record LessonListDto(
    int Id,
    int CourseId,
    string Title,
    string? VideoUrl,
    int sortOrder,
    bool isPublished
);

public record LessonDetailDto(
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

#endregion

#region Enrollment DTOs

public record EnrollmentListDto(
    int Id,
    int CourseId,
    string CourseCode,
    string CourseTitle,
    string StudentId,
    string StudentName,
    string StudentEmail,
    DateTime EnrolledAt,
    EnrollmentStatus Status
);

public record CreateEnrollmentRequestDto(
    int CourseId,
    string StudentId
);

public record UpdateEnrollmentStatusDto(
    EnrollmentStatus Status
);

#endregion

#region Assignment DTOs

public record AssignmentListDto(
    int Id,
    int CourseId,
    int? LessonId,
    string Title,
    DateTime DueDate,
    decimal MaxScore,
    bool IsPublished,
    int SubmissionCount
);

public record AssignmentDetailDto(
    int Id,
    int CourseId,
    int? LessonId,
    string Title,
    string? Description,
    DateTime DueDate,
    decimal MaxScore,
    bool IsPublished,
    DateTimeOffset CreatedAt
);

public record CreateAssignmentRequestDto(
    int CourseId,
    int? LessonId,
    string Title,
    string? Description,
    DateTime DueDate,
    decimal MaxScore
);

public record UpdateAssignmentRequestDto(
    string Title,
    string? Description,
    DateTime DueDate,
    decimal MaxScore,
    bool IsPublished
);

public record SubmissionListDto(
    int Id,
    int AssignmentId,
    string StudentId,
    string StudentName,
    DateTime SubmittedAt,
    decimal Grade,
    decimal MaxScore,
    SubmissionStatus Status,
    string? AttachmentUrl,
    string? Content = null,
    string? Feedback = null
)
{
    [JsonIgnore]
    public SubmissionStatus status => Status;
}

public record CreateSubmissionRequestDto(
    int AssignmentId,
    string? Content,
    string? AttachmentUrl
);

public record GradeSubmissionRequestDto(
    decimal Grade,
    string? Feedback
);

#endregion

#region Quiz DTOs

public record QuizListDto(
    int Id,
    int CourseId,
    int? LessonId,
    string Title,
    int TimeLimitMinutes,
    int MaxAttempts,
    decimal PassPercentage,
    bool IsPublished,
    int QuestionCount
);

// Alias for backwards compatibility
public record QuizListDtos(
    int Id,
    int CourseId,
    int? LessonId,
    string Title,
    int TimeLimitMinutes,
    int MaxAttempts,
    decimal PassPercentage,
    bool IsPublished,
    int QuestionCount
) : QuizListDto(Id, CourseId, LessonId, Title, TimeLimitMinutes, MaxAttempts, PassPercentage, IsPublished, QuestionCount);

public record QuizOptionDto(int Id, string Text, bool IsCorrect)
{
    [JsonConstructor]
    public QuizOptionDto(string Text, bool IsCorrect, int Id = 0)
        : this(Id, Text, IsCorrect) { }

    public string OptionText => Text;
}

public record QuizQuestionDto(int Id, int QuizId, string Text, decimal Points, int SortOrder, List<QuizOptionDto> Options)
{
    [JsonConstructor]
    public QuizQuestionDto(string Text, decimal Points, int SortOrder, List<QuizOptionDto> Options, int Id = 0, int QuizId = 0)
        : this(Id, QuizId, Text, Points, SortOrder, Options) { }

    public string QuestionText => Text;
}

public record QuizDetailsDto(
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
)
{
    public string? Description => Instructions;
}

public record CreateQuizRequestDto(
    int CourseId,
    int? LessonId,
    string Title,
    string? Instructions,
    int TimeLimitMinutes,
    int MaxAttempts,
    decimal PassPercentage
);

public record SubmitQuizAnswerDto(int QuestionId, int SelectedOptionId);

public record SubmitQuizAttemptRequestDto(int QuizId, List<SubmitQuizAnswerDto> Answers);

public record QuizAttemptResultDto(
    int Id,
    int QuizId,
    string StudentId,
    DateTime StartedAt,
    DateTime? CompletedAt,
    decimal ScoreAcheived,
    decimal TotalPossibleScore,
    decimal Percentage,
    bool Passed
)
{
    public int AttemptId => Id;
    public decimal ScorePercentage => Math.Round(Percentage, 2);
    public bool IsPassed => Passed;
}

#endregion

#region Analytics & Progress DTOs

public record AttendanceRecordDto(
    int Id,
    int CourseId,
    int? LessonId,
    string StudentId,
    string StudentName,
    DateTime SessionDate,
    AttendanceStatus Status,
    string? Remarks
);

public record BulkMarkAttendanceRequestDto(
    int CourseId,
    int? LessonId,
    DateTime SessionDate,
    List<StudentAttendanceItemDto> StudentRecords
);

public record StudentAttendanceItemDto(
    string StudentId,
    AttendanceStatus Status,
    string? Remarks
);

public record StudentCourseProgressDto(
    string StudentId,
    string StudentName,
    int TotalLessons,
    int CompletedLessons,
    decimal CompletionPercentage,
    int TotalAssignments,
    int SubmittedAssignments,
    decimal AttendancePercentage
);

#endregion

#region Admin & Organization DTOs

public record UserListDto(
    string Id,
    int TenantId,
    string FullName,
    string Email,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IEnumerable<string> Roles
);

public record UserDetailsDto(
    string Id,
    int TenantId,
    string FullName,
    string Email,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IEnumerable<string> Roles,
    IEnumerable<int> OrganizationNodeIds
);

public record CreateUserRequestDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    IEnumerable<string> Roles,
    int? TenantId = null
);

public record UpdateUserDto(
    string FirstName,
    string LastName,
    string Email,
    byte[] RowVersion
);

public record AssignRoleDto(string RoleName);

public record AssignOrganizationNodeDto(
    int OrganizationNodeId,
    string RoleId
);

public record OrganizationNodeDto(
    int Id,
    int TenantId,
    string Name,
    string NodeType,
    int? ParentId,
    IEnumerable<OrganizationNodeDto>? Children
);

public record CreateOrganizationNodeDto(
    string Name,
    string NodeType,
    int? ParentId,
    string? Code = null
);

public record UpdateOrganizationNodeDto(
    string Name,
    string NodeType,
    int? ParentId,
    byte[] RowVersion
);

public record AssignUserToNodeDto(
    string UserId,
    string RoleName
);

#endregion

#region Tenant DTOs

public record TenantDto(
    int Id,
    string Name,
    string Identifier,
    string? CustomDomain,
    bool IsActive,
    DateTimeOffset CreatedAt
);

public record CreateTenantDto(
    string Name,
    string Identifier,
    string? CustomDomain = null
);

public record UpdateTenantDto(
    string Name,
    string Identifier,
    string? CustomDomain = null,
    bool IsActive = true
);

#endregion
