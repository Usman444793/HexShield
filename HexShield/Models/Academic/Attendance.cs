using System.ComponentModel.DataAnnotations;
using HexShield.Models.Common;
using HexShield.Models.Identity;
using HexShield.Shared.Enums;

namespace HexShield.Models.Academic;

/// <summary>
/// Records student attendance for a specific session/lesson.
/// </summary>
public class Attendance : BaseEntity, IMultiTenant
{
    // ── Tenancy ──────────────────────────────────────────────────────────────
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    // ── Course/Lesson FKs ────────────────────────────────────────────────────
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int? LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    // ── Student FK ───────────────────────────────────────────────────────────
    [Required]
    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;

    // ── Attendance Data ──────────────────────────────────────────────────────
    public DateTime SessionDate { get; set; } = DateTime.UtcNow.Date;
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    [MaxLength(500)]
    public string? Remarks { get; set; }
}
