using HexShield.Models.Academic;
using HexShield.Models.Common;
namespace HexShield.Models.DTOs.Enrollments;
public record EnrollmentListDto
(
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
public record CreateEnrollmentRequestDto
    (
        int CourseId,
        string StudentId
    );
public record UpdateEnrollmentStatusDto
    (
            EnrollmentStatus Status
    );