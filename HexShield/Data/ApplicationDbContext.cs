using System.Linq.Expressions;
using HexShield.Models.Academic;
using HexShield.Models.Assignments;
using HexShield.Models.Common;
using HexShield.Models.Identity;
using HexShield.Models.Progress;
using HexShield.Models.Quizzes;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace HexShield.Data;
public class ApplicationDbContext : IdentityDbContext<ApplicationUser,ApplicationRole,string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    //academics
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<TeacherProfile> TeacherProfiles => Set<TeacherProfile>();
    //Organization
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<OrganizationNode> OrganizationNodes => Set<OrganizationNode>();
    public DbSet<UserOrganizationRole> UserOrganizationRoles => Set<UserOrganizationRole>();
    //Assignment
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();
    //Quiz
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
    public DbSet<QuizAnswerOption> QuizAnswerOptions => Set<QuizAnswerOption>();
    //progress
    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
    //save changes
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
    private void ProcessAuditingAndSoftDelete()
    {
        var now = DateTimeOffset.UtcNow;
        foreach(var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.IsDeleted = false;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ConfigureIdentity(builder);
        ConfigureTenantRelationships(builder);
        ConfigureOrganizationRelationships(builder);
        ConfigureAcademicRelationships(builder);
        ConfigureAssignmentRelationships(builder);
        ConfigureQuizRelationships(builder);
        ConfigureProgressRelationships(builder);
        ConfigureIndexes(builder);
        ConfigureSoftDelete(builder);
    }
    //Identity
    private static void ConfigureIdentity(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName).HasMaxLength(100);
            entity.HasOne(u => u.Tenant).WithMany(t => t.Users).HasForeignKey(t => t.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(u => u.NormalizedUserName).HasDatabaseName("UserNameIndex").IsUnique(false);
            entity.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique(false);
        });
        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(r => r.Description).HasMaxLength(250);
        });
    }
    //tenant
    private static void ConfigureTenantRelationships(ModelBuilder builder)
    {
        builder.Entity<Tenant>(entity =>
        {
            entity.Property(t => t.Name).HasMaxLength(100);
            entity.Property(t => t.Identifier).HasMaxLength(50);
            entity.Property(t => t.CustomDomain).HasMaxLength(200);
            entity.HasIndex(t => t.Identifier).IsUnique().HasFilter("[IsDeleted] = 0");
        });
    }
    //organization
    private static void ConfigureOrganizationRelationships(ModelBuilder builder)
    {
        builder.Entity<OrganizationNode>(entity =>
        {
            //tenant -> OrganizationNode
            entity.HasOne(o => o.Tenant).WithMany(t => t.OrganizationNodes).HasForeignKey(o => o.TenantId).OnDelete(DeleteBehavior.Restrict);
            //OrganizationNode -> ParentOrganizationNode
            entity.HasOne(o => o.Parent).WithMany(o => o.Children).HasForeignKey(o => o.ParentId).OnDelete(DeleteBehavior.Restrict);
            //unique code within tanent
            entity.HasIndex(o => new { o.TenantId, o.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
            entity.HasIndex(o => o.HierarchyPath);
        });
        builder.Entity<UserOrganizationRole>(entity =>
        {
            entity.HasOne(x => x.User).WithMany(u => u.OrganizationRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Role).WithMany(u => u.OrganizationRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.OrganizationNode).WithMany(u => u.UserOrganizationRoles).HasForeignKey(x => x.OrganizationNodeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.UserId, x.RoleId, x.OrganizationNodeId }).IsUnique().HasFilter("[IsDeleted] = 0");
        });
    }
    //Academic
    private static void ConfigureAcademicRelationships(ModelBuilder builder)
    {
        //teacher-profile - ApplicationUser
        builder.Entity<TeacherProfile>(entity =>
        {
            entity.HasOne(t => t.User).WithOne(u => u.TeacherProfile).HasForeignKey<TeacherProfile>(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(t => t.UserId).IsUnique().HasFilter("[IsDeleted] = 0");
            entity.HasIndex(t => t.EmployeeNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        });
        //student-profile - ApplicationUser
        builder.Entity<StudentProfile>(entity =>
        {
            entity.HasOne(s => s.User).WithOne(s => s.StudentProfile).HasForeignKey<StudentProfile>(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(s => s.UserId).IsUnique().HasFilter("[IsDeleted] = 0");
            entity.HasIndex(s => s.StudentNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        });
        //Course 
        builder.Entity<Course>(entity =>
        {
            //Course - teacher
            entity.HasOne(c => c.PrimaryTeacherProfile).WithMany(c => c.PrimaryCourses).HasForeignKey(t => t.PrimaryTeacherProfileId).OnDelete(DeleteBehavior.Restrict);
            //Course - OrganizationNode
            entity.HasOne(c => c.OrganizationNode).WithMany(c => c.Courses).HasForeignKey(c => c.OrganizationNodeId).OnDelete(DeleteBehavior.Restrict);
            //course - tenant
            entity.HasOne(c => c.Tenant).WithMany(c => c.Courses).HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
            //each course is unique
            entity.HasIndex(c => new { c.TenantId, c.CourseCode }).IsUnique().HasFilter("[IsDeleted] = 0");
        });
        //lesson -> Course
        builder.Entity<Lesson>(entity =>
        {
            entity.HasOne(l => l.Course).WithMany(c => c.Lessons).HasForeignKey(l => l.CourseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(l => new { l.CourseId, l.SortOrder }).IsUnique().HasFilter("[IsDeleted] = 0");
        });
        //enrollement course + student
        builder.Entity<Enrollment>(entity =>
        {
            entity.HasOne(e => e.StudentProfile).WithMany(e => e.Enrollments).HasForeignKey(e => e.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Course).WithMany(e => e.Enrollments).HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.StudentProfileId, e.CourseId }).IsUnique().HasFilter("[IsDeleted] = 0");
        });
    }
    //Assignment
    private static void ConfigureAssignmentRelationships(ModelBuilder builder)
    {
        builder.Entity<Assignment>(entity =>
        {
            entity.HasOne(a => a.Course).WithMany(a => a.Assignments).HasForeignKey(a => a.CourseId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Submission>(entity =>
        {
            entity.HasOne(s => s.Assignment).WithMany(s => s.Submissions).HasForeignKey(s => s.AssignmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(s => s.StudentProfile).WithMany(s => s.Submissions).HasForeignKey(s => s.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(s => s.GradedBy).WithMany().HasForeignKey(s => s.GradedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(s => new { s.AssignmentId, s.StudentProfileId }).IsUnique().HasFilter("[IsDeleted] = 0");
        });
    }
    //Quiz
    private static void ConfigureQuizRelationships(ModelBuilder builder) 
    {
        builder.Entity<Quiz>(entity =>
        {
            entity.HasOne(q => q.Course).WithMany(q => q.Quizzes).HasForeignKey(q => q.CourseId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Question>(entity =>
        {
            entity.HasOne(q => q.Quiz).WithMany(q => q.Questions).HasForeignKey(q => q.QuizId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(q => new { q.QuizId, q.SortOrder }).IsUnique().HasFilter("[IsDeleted] = 0");
        });
        builder.Entity<QuestionOption>(entity =>
        {
            entity.HasOne(q => q.Question).WithMany(q => q.Options).HasForeignKey(q => q.QuestionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(q => new { q.QuestionId, q.SortOrder }).IsUnique().HasFilter("[IsDeleted] = 0");
        });
        builder.Entity<QuizAttempt>(entity =>
        {
            entity.HasOne(a => a.Quiz).WithMany(a => a.Attempts).HasForeignKey(a => a.QuizId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(a => a.StudentProfile).WithMany(a => a.QuizAttempts).HasForeignKey(a => a.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<QuizAnswer>(entity =>
        {
            entity.HasOne(a => a.QuizAttempt).WithMany(a => a.Answers).HasForeignKey(a => a.QuizAttemptId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(a => a.Question).WithMany().HasForeignKey(a => a.QuestionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(a => new { a.QuizAttemptId, a.QuestionId }).IsUnique().HasFilter("[IsDeleted] = 0");
        });
        builder.Entity<QuizAnswerOption>(entity =>
        {
            entity.HasKey(x => new { x.QuizAnswerId, x.QuestionOptionId });
            entity.HasOne(x => x.QuizAnswer).WithMany(x => x.SelectedOptions).HasForeignKey(x => x.QuizAnswerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.QuestionOption).WithMany().HasForeignKey(x => x.QuestionOptionId).OnDelete(DeleteBehavior.Restrict);
        });
    }
    //Progress
    private static void ConfigureProgressRelationships(ModelBuilder builder)
    {
        builder.Entity<LessonProgress>(entity =>
        {
            entity.HasOne(p => p.StudentProfile).WithMany(p => p.LessonProgress).HasForeignKey(p => p.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(p => p.Lesson).WithMany(p => p.LessonProgress).HasForeignKey(p => p.LessonId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(p => new { p.StudentProfileId, p.LessonId }).IsUnique().HasFilter("[IsDeleted] = 0");
        });
    }
    //Indexes
    private static void ConfigureIndexes(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>().HasIndex(u => new { u.TenantId, u.NormalizedUserName }).IsUnique();
        builder.Entity<ApplicationUser>().HasIndex(u => new { u.TenantId, u.NormalizedEmail }).IsUnique().HasFilter("[NormalizedEmail] IS NOT NULL");
    }
    //Soft Delete
    private static void ConfigureSoftDelete(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                builder.Entity(entityType.ClrType).HasQueryFilter(CreateSoftDeleteFilter(entityType.ClrType));
            }
        }
    }
    private static LambdaExpression CreateSoftDeleteFilter(Type entityType)
    {
        var parameter = Expression.Parameter(entityType,"e");
        var property =  Expression.Property(parameter,nameof(BaseEntity.IsDeleted));
        var condition = Expression.Equal(property,Expression.Constant(false));
        return Expression.Lambda(condition,parameter);
    }
}
