using HexShield.Infrastructure.Tenancy;
using HexShield.Models.Academic;
using HexShield.Models.Common;
using HexShield.Models.Identity;
using HexShield.Models.Progress;
using HexShield.Models.Quizzes;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HexShield.Data;

/// <summary>
/// Central EF Core DbContext for HexShield.
/// Inherits IdentityDbContext for ASP.NET Core Identity.
/// Implements IDataProtectionKeyContext for key persistence.
/// Supports multi-tenancy via ITenantContext global query filters.
/// Supports soft-delete and full audit trail via BaseEntity.
/// Concurrency is enforced with RowVersion (timestamp) on all BaseEntity types.
/// </summary>
public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>,
      IDataProtectionKeyContext
{
    private readonly ITenantContext? _tenantContext;
    public int CurrentTenantId => _tenantContext?.TenantId ?? 0;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // ── Data Protection ──────────────────────────────────────────────────────
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;

    // ── Identity ─────────────────────────────────────────────────────────────
    public DbSet<RefreshToken> RefreshTokens { get; set; } = default!;
    public DbSet<ApplicationPermission> Permissions { get; set; } = default!;
    public DbSet<RolePermission> RolePermissions { get; set; } = default!;

    // ── Organisation ─────────────────────────────────────────────────────────
    public DbSet<Tenant> Tenants { get; set; } = default!;
    public DbSet<OrganizationNode> OrganizationNodes { get; set; } = default!;
    public DbSet<UserOrganizationRole> UserOrganizationRoles { get; set; } = default!;

    // ── Academics ────────────────────────────────────────────────────────────
    public DbSet<Course> Courses { get; set; } = default!;
    public DbSet<Enrollment> Enrollments { get; set; } = default!;
    public DbSet<Lesson> Lessons { get; set; } = default!;
    public DbSet<Attendance> Attendances { get; set; } = default!;    // Added — was missing
    public DbSet<StudentProfile> StudentProfiles { get; set; } = default!;
    public DbSet<TeacherProfile> TeacherProfiles { get; set; } = default!;

    // ── Assignments ──────────────────────────────────────────────────────────
    public DbSet<Assignment> Assignments { get; set; } = default!;
    public DbSet<Submission> Submissions { get; set; } = default!;

    // ── Quizzes ──────────────────────────────────────────────────────────────
    public DbSet<Quiz> Quizzes { get; set; } = default!;
    public DbSet<Question> Questions { get; set; } = default!;
    public DbSet<QuestionOption> QuestionOptions { get; set; } = default!;
    public DbSet<QuizAttempt> QuizAttempts { get; set; } = default!;
    public DbSet<QuizAnswer> QuizAnswers { get; set; } = default!;
    public DbSet<QuizAnswerOption> QuizAnswerOptions { get; set; } = default!;

    // ── Progress ─────────────────────────────────────────────────────────────
    public DbSet<LessonProgress> LessonProgresses { get; set; } = default!;

    // ── SaveChanges overrides (Auditing + Soft-Delete + Concurrency) ─────────
    public override int SaveChanges()
    {
        ProcessAuditingAndSoftDelete();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ProcessAuditingAndSoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Automatically stamps CreatedAt/UpdatedAt/DeletedAt and enforces soft-delete
    /// for all BaseEntity derivatives. Also stamps TenantId on multi-tenant entities.
    /// </summary>
    private void ProcessAuditingAndSoftDelete()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            // ── Soft-delete + audit ──────────────────────────────────────────
            if (entry.Entity is BaseEntity baseEntity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        baseEntity.CreatedAt = now;
                        baseEntity.IsDeleted = false;
                        break;

                    case EntityState.Modified:
                        baseEntity.UpdatedAt = now;
                        break;

                    case EntityState.Deleted:
                        entry.State          = EntityState.Modified;
                        baseEntity.IsDeleted = true;
                        baseEntity.DeletedAt = now;
                        baseEntity.UpdatedAt = now;
                        break;
                }
            }

            // ── Auto-stamp TenantId on new multi-tenant entities ─────────────
            if (entry.State == EntityState.Added
                && entry.Entity is IMultiTenant multiTenant
                && multiTenant.TenantId == 0
                && CurrentTenantId != 0)
            {
                multiTenant.TenantId = CurrentTenantId;
            }
        }
    }

    // ── Model Building ───────────────────────────────────────────────────────
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureIdentity(builder);
        ConfigureTenantRelationships(builder);
        ConfigureOrganizationRelationships(builder);
        ConfigureAcademicRelationships(builder);
        ConfigureAttendanceRelationships(builder);
        ConfigureAssignmentRelationships(builder);
        ConfigureQuizRelationships(builder);
        ConfigureProgressRelationships(builder);
        ConfigureIndexes(builder);
        ConfigureGlobalFilters(builder);

        // ── RowVersion (concurrency token) for every BaseEntity ──────────────
        // Applied here rather than per-entity so it's automatically inherited
        // by all future entities without manual configuration.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                builder.Entity(entityType.ClrType)
                       .Property(nameof(BaseEntity.RowVersion))
                       .IsRowVersion()
                       .IsConcurrencyToken();
            }
        }

        // ── Default Precision for Decimals ────────────────────────────────────
        foreach (var property in builder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }
    }

    // ── Identity ─────────────────────────────────────────────────────────────
    private static void ConfigureIdentity(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            // RowVersion concurrency token — explicitly configured for ApplicationUser
            // (ApplicationUser is NOT a BaseEntity so must be done here separately)
            entity.Property(u => u.RowVersion).IsRowVersion().IsConcurrencyToken();

            entity.HasOne(u => u.Tenant)
                  .WithMany(t => t.Users)
                  .HasForeignKey(u => u.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Override Identity's default unique indexes — uniqueness is enforced
            // at the tenant level via the composite indexes below.
            entity.HasIndex(u => u.NormalizedUserName)
                  .HasDatabaseName("UserNameIndex")
                  .IsUnique(false);

            entity.HasIndex(u => u.NormalizedEmail)
                  .HasDatabaseName("EmailIndex")
                  .IsUnique(false);
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(r => r.Description).HasMaxLength(250);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            entity.HasOne(rp => rp.Role)
                  .WithMany(r => r.RolePermissions)
                  .HasForeignKey(rp => rp.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Permission)
                  .WithMany()
                  .HasForeignKey(rp => rp.PermissionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApplicationPermission>(entity =>
        {
            entity.HasIndex(p => p.Name).IsUnique();
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);
            entity.HasIndex(rt => rt.TokenHash).IsUnique();

            entity.HasOne(rt => rt.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(rt => rt.UserId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // ── Tenant ───────────────────────────────────────────────────────────────
    private static void ConfigureTenantRelationships(ModelBuilder builder)
    {
        builder.Entity<Tenant>(entity =>
        {
            entity.Property(t => t.Name).HasMaxLength(100).IsRequired();
            entity.Property(t => t.Identifier).HasMaxLength(50).IsRequired();
            entity.Property(t => t.CustomDomain).HasMaxLength(200);

            // Soft-delete aware unique index — deleted tenants don't block re-use of identifier
            entity.HasIndex(t => t.Identifier)
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");

            // Concurrency for Tenant (extends BaseEntity — handled by loop above)
        });
    }

    // ── Organisation ─────────────────────────────────────────────────────────
    private static void ConfigureOrganizationRelationships(ModelBuilder builder)
    {
        builder.Entity<OrganizationNode>(entity =>
        {
            entity.HasOne(o => o.Tenant)
                  .WithMany(t => t.OrganizationNodes)
                  .HasForeignKey(o => o.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Parent)
                  .WithMany(o => o.Children)
                  .HasForeignKey(o => o.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(o => new { o.TenantId, o.Code })
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");

            entity.HasIndex(o => o.HierarchyPath);
        });

        builder.Entity<UserOrganizationRole>(entity =>
        {
            entity.HasOne(x => x.User)
                  .WithMany(u => u.OrganizationRoles)
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Role)
                  .WithMany(r => r.OrganizationRoles)
                  .HasForeignKey(x => x.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.OrganizationNode)
                  .WithMany(o => o.UserOrganizationRoles)
                  .HasForeignKey(x => x.OrganizationNodeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.UserId, x.RoleId, x.OrganizationNodeId })
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");
        });
    }

    // ── Academics ────────────────────────────────────────────────────────────
    private static void ConfigureAcademicRelationships(ModelBuilder builder)
    {
        // TeacherProfile → ApplicationUser (1-to-1)
        builder.Entity<TeacherProfile>(entity =>
        {
            entity.HasOne(t => t.User)
                  .WithOne(u => u.TeacherProfile)
                  .HasForeignKey<TeacherProfile>(t => t.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(t => t.UserId)
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");

            entity.HasIndex(t => t.EmployeeNumber)
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");
        });

        // StudentProfile → ApplicationUser (1-to-1)
        builder.Entity<StudentProfile>(entity =>
        {
            entity.HasOne(s => s.User)
                  .WithOne(u => u.StudentProfile)
                  .HasForeignKey<StudentProfile>(s => s.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => s.UserId)
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");

            entity.HasIndex(s => s.StudentNumber)
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");
        });

        // Course
        builder.Entity<Course>(entity =>
        {
            entity.HasOne(c => c.PrimaryTeacherProfile)
                  .WithMany(t => t.PrimaryCourses)
                  .HasForeignKey(c => c.PrimaryTeacherProfileId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.OrganizationNode)
                  .WithMany(o => o.Courses)
                  .HasForeignKey(c => c.OrganizationNodeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Tenant)
                  .WithMany(t => t.Courses)
                  .HasForeignKey(c => c.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => new { c.TenantId, c.CourseCode })
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");
        });

        // Lesson → Course
        builder.Entity<Lesson>(entity =>
        {
            entity.HasOne(l => l.Course)
                  .WithMany(c => c.Lessons)
                  .HasForeignKey(l => l.CourseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.Tenant)
                  .WithMany()
                  .HasForeignKey(l => l.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(l => new { l.CourseId, l.SortOrder })
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");
        });

        // Enrollment → StudentProfile + Course  (FIXED: was HasOne on int FK)
        builder.Entity<Enrollment>(entity =>
        {
            entity.HasOne(e => e.StudentProfile)          // Fixed: was e.StudentProfileId
                  .WithMany(sp => sp.Enrollments)
                  .HasForeignKey(e => e.StudentProfileId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Course)
                  .WithMany(c => c.Enrollments)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.StudentProfileId, e.CourseId })
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");
        });
    }

    // ── Attendance ───────────────────────────────────────────────────────────
    private static void ConfigureAttendanceRelationships(ModelBuilder builder)
    {
        builder.Entity<Attendance>(entity =>
        {
            entity.HasOne(a => a.Tenant)
                  .WithMany()
                  .HasForeignKey(a => a.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Course)
                  .WithMany()
                  .HasForeignKey(a => a.CourseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Lesson)
                  .WithMany()
                  .HasForeignKey(a => a.LessonId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Student)
                  .WithMany()
                  .HasForeignKey(a => a.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Unique: one attendance record per student per lesson
            entity.HasIndex(a => new { a.StudentId, a.LessonId, a.TenantId })
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");
        });
    }

    // ── Assignments ──────────────────────────────────────────────────────────
    private static void ConfigureAssignmentRelationships(ModelBuilder builder)
    {
        builder.Entity<Assignment>(entity =>
        {
            entity.HasOne(a => a.Course)
                  .WithMany(c => c.Assignments)
                  .HasForeignKey(a => a.CourseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Lesson)
                  .WithMany()
                  .HasForeignKey(a => a.LessonId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.Tenant)
                  .WithMany()
                  .HasForeignKey(a => a.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Submission>(entity =>
        {
            entity.HasOne(s => s.Assignment)
                  .WithMany(a => a.Submissions)
                  .HasForeignKey(s => s.AssignmentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Student)
                  .WithMany()
                  .HasForeignKey(s => s.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Tenant)
                  .WithMany()
                  .HasForeignKey(s => s.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => new { s.AssignmentId, s.StudentId })
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");
        });
    }

    // ── Quizzes ──────────────────────────────────────────────────────────────
    private static void ConfigureQuizRelationships(ModelBuilder builder)
    {
        builder.Entity<Quiz>(entity =>
        {
            entity.HasOne(q => q.Course)
                  .WithMany(c => c.Quizzes)
                  .HasForeignKey(q => q.CourseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(q => q.Lesson)
                  .WithMany()
                  .HasForeignKey(q => q.LessonId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(q => q.Tenant)
                  .WithMany()
                  .HasForeignKey(q => q.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Question>(entity =>
        {
            entity.HasOne(q => q.Quiz)
                  .WithMany(q => q.Questions)
                  .HasForeignKey(q => q.QuizId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(q => new { q.QuizId, q.SortOrder })
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");
        });

        builder.Entity<QuestionOption>(entity =>
        {
            entity.HasOne(o => o.Question)
                  .WithMany(q => q.Options)
                  .HasForeignKey(o => o.QuestionId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(o => new { o.QuestionId, o.SortOrder })
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");
        });

        builder.Entity<QuizAttempt>(entity =>
        {
            entity.HasOne(a => a.Quiz)
                  .WithMany(q => q.Attempts)
                  .HasForeignKey(a => a.QuizId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.StudentProfile)
                  .WithMany(sp => sp.QuizAttempts)
                  .HasForeignKey(a => a.StudentProfileId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Tenant)
                  .WithMany()
                  .HasForeignKey(a => a.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<QuizAnswer>(entity =>
        {
            entity.HasOne(a => a.QuizAttempt)
                  .WithMany(qa => qa.Answers)
                  .HasForeignKey(a => a.QuizAttemptId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Question)
                  .WithMany()
                  .HasForeignKey(a => a.QuestionId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => new { a.QuizAttemptId, a.QuestionId })
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");
        });

        builder.Entity<QuizAnswerOption>(entity =>
        {
            entity.HasKey(x => new { x.QuizAnswerId, x.QuestionOptionId });

            entity.HasOne(x => x.QuizAnswer)
                  .WithMany(a => a.SelectedOptions)
                  .HasForeignKey(x => x.QuizAnswerId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.QuestionOption)
                  .WithMany()
                  .HasForeignKey(x => x.QuestionOptionId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // ── Progress ─────────────────────────────────────────────────────────────
    private static void ConfigureProgressRelationships(ModelBuilder builder)
    {
        builder.Entity<LessonProgress>(entity =>
        {
            entity.HasOne(p => p.StudentProfile)
                  .WithMany(sp => sp.LessonProgress)
                  .HasForeignKey(p => p.StudentProfileId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Lesson)
                  .WithMany(l => l.LessonProgress)
                  .HasForeignKey(p => p.LessonId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Tenant)
                  .WithMany()
                  .HasForeignKey(p => p.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => new { p.StudentProfileId, p.LessonId })
                  .IsUnique()
                  .HasFilter("[IsDeleted] = 0");
        });
    }

    // ── Additional Indexes ───────────────────────────────────────────────────
    private static void ConfigureIndexes(ModelBuilder builder)
    {
        // Tenant-scoped username uniqueness
        builder.Entity<ApplicationUser>()
               .HasIndex(u => new { u.TenantId, u.NormalizedUserName })
               .IsUnique();

        // Tenant-scoped email uniqueness (only when email is set)
        builder.Entity<ApplicationUser>()
               .HasIndex(u => new { u.TenantId, u.NormalizedEmail })
               .IsUnique()
               .HasFilter("[NormalizedEmail] IS NOT NULL");
    }

    // ── Global Query Filters (Soft-Delete + Multi-Tenancy) ───────────────────
    private void ConfigureGlobalFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var filter = CreateGlobalFilter(entityType.ClrType);
            if (filter != null)
            {
                builder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    private LambdaExpression? CreateGlobalFilter(Type entityType)
    {
        var parameter          = Expression.Parameter(entityType, "e");
        Expression? combined   = null;

        // ── Soft-delete filter ────────────────────────────────────────────────
        if (typeof(BaseEntity).IsAssignableFrom(entityType))
        {
            var isDeletedProp = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            combined = Expression.Equal(isDeletedProp, Expression.Constant(false));
        }

        // ── Multi-tenant filter ───────────────────────────────────────────────
        if (typeof(IMultiTenant).IsAssignableFrom(entityType))
        {
            var currentTenantProp = Expression.Property(
                Expression.Constant(this), nameof(CurrentTenantId));

            // When CurrentTenantId == 0 (e.g. anonymous login flow) bypass tenant filter
            var isTenantZero  = Expression.Equal(currentTenantProp, Expression.Constant(0));
            var tenantIdProp  = Expression.Property(parameter, nameof(IMultiTenant.TenantId));
            var tenantMatches = Expression.Equal(tenantIdProp, currentTenantProp);
            var tenantFilter  = Expression.OrElse(isTenantZero, tenantMatches);

            combined = combined == null
                ? tenantFilter
                : Expression.AndAlso(combined, tenantFilter);
        }

        return combined != null
            ? Expression.Lambda(combined, parameter)
            : null;
    }
}
