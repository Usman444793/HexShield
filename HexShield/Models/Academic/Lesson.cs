using System.ComponentModel.DataAnnotations;
using HexShield.Models.Common;
using HexShield.Models.Progress;

namespace HexShield.Models.Academic;

/// <summary>
/// Represents a single lesson within a course.
/// Implements IMultiTenant for per-tenant data isolation.
/// </summary>
public class Lesson : BaseEntity, IMultiTenant
{
    // ── Tenancy ─────────────────────────────────────────────────────────────
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    // ── Course FK ────────────────────────────────────────────────────────────
    [Required]
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    // ── Content ──────────────────────────────────────────────────────────────
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(10000)]
    public string Content { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? VideoUrl { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "SortOrder must be a positive integer.")]
    public int SortOrder { get; set; }

    public bool IsPublished { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public ICollection<LessonProgress> LessonProgress { get; set; } = new List<LessonProgress>();
}
