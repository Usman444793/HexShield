using System.ComponentModel.DataAnnotations;
using HexShield.Models.Common;
using HexShield.Shared.Enums;

namespace HexShield.Models.Academic;

/// <summary>
/// Links a StudentProfile to a Course, tracking enrollment lifecycle.
/// Uses int FK to StudentProfile.
/// </summary>
public class Enrollment : BaseEntity, IMultiTenant
{
    // ── Tenancy ──────────────────────────────────────────────────────────────
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    // ── Course FK ────────────────────────────────────────────────────────────
    [Required]
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    // ── Student FK — int FK to StudentProfile ────────────────────────────────
    [Required]
    public int StudentProfileId { get; set; }
    public StudentProfile StudentProfile { get; set; } = null!;

    // ── Enrollment Data ──────────────────────────────────────────────────────
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
}