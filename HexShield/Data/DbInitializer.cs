using HexShield.Models.Academic;
using HexShield.Models.Common;
using HexShield.Models.Identity;
using HexShield.Models.Quizzes;
using HexShield.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HexShield.Data;

public static class DbInitializer
{
    /// <summary>
    /// Applies pending migrations and seeds essential bootstrap data (default tenant, LMS roles,
    /// default Admin/Teacher/Student accounts, system permissions, and initial portfolio curriculum).
    /// Safe to call on every startup — all operations are idempotent.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            // ── 1. Apply EF Core Migrations ─────────────────────────────────────────
            await db.Database.MigrateAsync();

            // ── 1b. Ensure Assignments & Submissions text columns are widened to nvarchar(max) ──
            await db.Database.ExecuteSqlRawAsync(@"
                IF COL_LENGTH('dbo.Submissions', 'Content') IS NOT NULL
                    ALTER TABLE [dbo].[Submissions] ALTER COLUMN [Content] nvarchar(max) NULL;
                IF COL_LENGTH('dbo.Submissions', 'Feedback') IS NOT NULL
                    ALTER TABLE [dbo].[Submissions] ALTER COLUMN [Feedback] nvarchar(max) NULL;
                IF COL_LENGTH('dbo.Submissions', 'AttachmentUrl') IS NOT NULL
                    ALTER TABLE [dbo].[Submissions] ALTER COLUMN [AttachmentUrl] nvarchar(2000) NULL;
                IF COL_LENGTH('dbo.Assignments', 'Description') IS NOT NULL
                    ALTER TABLE [dbo].[Assignments] ALTER COLUMN [Description] nvarchar(max) NULL;
                IF COL_LENGTH('dbo.Assignments', 'Title') IS NOT NULL
                    ALTER TABLE [dbo].[Assignments] ALTER COLUMN [Title] nvarchar(500) NOT NULL;
            ");

            // ── 2. Seed Default Tenant ─────────────────────────────────────────────
            var defaultTenant = await db.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Identifier == "default" || t.Id == 1);

            if (defaultTenant == null)
            {
                defaultTenant = new Tenant
                {
                    Name = "HexShield Enterprise Academy",
                    Identifier = "default",
                    IsActive = true
                };
                db.Tenants.Add(defaultTenant);
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded default tenant with ID {TenantId}.", defaultTenant.Id);
            }

            // ── 3. Seed LMS Roles ──────────────────────────────────────────────────
            foreach (var roleName in LmsRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new ApplicationRole
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpperInvariant(),
                        Description = $"System role: {roleName}",
                        TenantId = defaultTenant.Id
                    });

                    if (result.Succeeded)
                        logger.LogInformation("Seeded role: {Role}", roleName);
                }
            }

            // ── 4. Seed Default System Permissions ─────────────────────────────────
            var defaultPermissions = new (string Name, string Description)[]
            {
                ("Courses.Manage", "Create, edit, publish, and archive courses"),
                ("Lessons.Manage", "Author and reorder course lessons"),
                ("Assignments.Grade", "Review and grade student assignment submissions"),
                ("Quizzes.Manage", "Author timed quizzes and question banks"),
                ("Attendance.Manage", "Record and update student session attendance"),
                ("Users.Governance", "Provision users, manage roles, and enforce tenant isolation")
            };

            foreach (var (permName, permDesc) in defaultPermissions)
            {
                if (!await db.Permissions.AnyAsync(p => p.Name == permName))
                {
                    db.Permissions.Add(new ApplicationPermission
                    {
                        Name = permName,
                        Description = permDesc,
                        IsSystemPermission = true
                    });
                }
            }
            await db.SaveChangesAsync();

            // ── 5. Seed Default Admin User ─────────────────────────────────────────
            const string adminEmail = "admin@hexshield.com";
            var adminUser = await EnsureUserAsync(
                userManager,
                db,
                adminEmail,
                "HexShield Administrator",
                "Admin@12345678!",
                LmsRoles.Admin,
                defaultTenant.Id,
                logger);

            // ── 6. Seed Default Teacher User & Profile ─────────────────────────────
            const string teacherEmail = "teacher@hexshield.com";
            var teacherUser = await EnsureUserAsync(
                userManager,
                db,
                teacherEmail,
                "Dr. Elena Vance",
                "Teacher@12345678!",
                LmsRoles.Teacher,
                defaultTenant.Id,
                logger);

            var teacherProfile = await db.TeacherProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(tp => tp.UserId == teacherUser.Id);
            if (teacherProfile == null)
            {
                teacherProfile = new TeacherProfile
                {
                    UserId = teacherUser.Id,
                    EmployeeNumber = "EMP-2026-1001",
                    Bio = "Principal Instructor in Cloud Security & Zero-Trust Systems",
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedById = adminUser.Id
                };
                db.TeacherProfiles.Add(teacherProfile);
                await db.SaveChangesAsync();
            }

            // ── 7. Seed Default Student User & Profile ─────────────────────────────
            const string studentEmail = "student@hexshield.com";
            var studentUser = await EnsureUserAsync(
                userManager,
                db,
                studentEmail,
                "Alex Mercer",
                "Student@12345678!",
                LmsRoles.Student,
                defaultTenant.Id,
                logger);

            var studentProfile = await db.StudentProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(sp => sp.UserId == studentUser.Id);
            if (studentProfile == null)
            {
                studentProfile = new StudentProfile
                {
                    UserId = studentUser.Id,
                    StudentNumber = "STU-2026-10001",
                    Department = "Cybersecurity & Distributed Systems",
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedById = adminUser.Id
                };
                db.StudentProfiles.Add(studentProfile);
                await db.SaveChangesAsync();
            }

            // ── 8. Seed Default Organization Node & Sample Course ──────────────────
            var rootNode = await db.OrganizationNodes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(n => n.TenantId == defaultTenant.Id && !n.IsDeleted);
            if (rootNode == null)
            {
                rootNode = new OrganizationNode
                {
                    TenantId = defaultTenant.Id,
                    Name = "Faculty of Cybersecurity & Engineering",
                    Code = "FCE-01",
                    NodeType = "Faculty",
                    HierarchyPath = "/",
                    CreatedById = adminUser.Id
                };
                db.OrganizationNodes.Add(rootNode);
                await db.SaveChangesAsync();
            }

            var hasCourses = await db.Courses
                .IgnoreQueryFilters()
                .AnyAsync(c => c.TenantId == defaultTenant.Id && !c.IsDeleted);

            if (!hasCourses)
            {
                var sampleCourse = new Course
                {
                    TenantId = defaultTenant.Id,
                    CourseCode = "CS-401",
                    Title = "Zero-Trust Architecture & Identity Security",
                    Description = "Comprehensive engineering curriculum covering multi-tenant isolation, JWT & refresh token rotation, RBAC governance, and distributed cloud security.",
                    IsPublished = true,
                    PrimaryTeacherProfileId = teacherProfile.Id,
                    OrganizationNodeId = rootNode.Id,
                    CreatedById = teacherUser.Id
                };
                db.Courses.Add(sampleCourse);
                await db.SaveChangesAsync();

                var lesson1 = new Lesson
                {
                    TenantId = defaultTenant.Id,
                    CourseId = sampleCourse.Id,
                    Title = "Module 1: Multi-Tenant Isolation & Global Query Filters",
                    Content = "Explore how HexShield enforces cryptographic and relational tenant boundaries using EF Core global query filters, claims-based tenant resolution, and row-version concurrency tokens.",
                    SortOrder = 1,
                    IsPublished = true,
                    CreatedById = teacherUser.Id
                };
                var lesson2 = new Lesson
                {
                    TenantId = defaultTenant.Id,
                    CourseId = sampleCourse.Id,
                    Title = "Module 2: OAuth 2.0 Refresh Token Rotation & Replay Defense",
                    Content = "Understand one-time refresh token rotation, SHA-256 token hashing at rest, and automatic session family revocation when replay attacks are detected.",
                    SortOrder = 2,
                    IsPublished = true,
                    CreatedById = teacherUser.Id
                };
                db.Lessons.AddRange(lesson1, lesson2);
                await db.SaveChangesAsync();

                var assignment = new Assignment
                {
                    TenantId = defaultTenant.Id,
                    CourseId = sampleCourse.Id,
                    LessonId = lesson1.Id,
                    Title = "Lab 1: Designing a Multi-Tenant Security Policy",
                    Description = "Document how your service layer prevents cross-tenant data leakage and handles optimistic concurrency conflicts.",
                    DueDate = DateTime.UtcNow.AddDays(14),
                    MaxScore = 100,
                    IsPublished = true,
                    CreatedById = teacherUser.Id
                };
                db.Assignments.Add(assignment);

                var quiz = new Quiz
                {
                    TenantId = defaultTenant.Id,
                    CourseId = sampleCourse.Id,
                    LessonId = lesson2.Id,
                    Title = "Zero-Trust & Token Security Assessment",
                    Instructions = "Select the best answer for each question on JWT authentication and multi-tenant isolation.",
                    TimeLimitMinutes = 20,
                    MaxAttempts = 3,
                    PassPercentage = 70,
                    IsPublished = true,
                    CreatedById = teacherUser.Id,
                    Questions = new List<Question>
                    {
                        new Question
                        {
                            Text = "How does HexShield store refresh tokens in the database to prevent credential exposure?",
                            Marks = 10,
                            SortOrder = 1,
                            CreatedById = teacherUser.Id,
                            Options = new List<QuestionOption>
                            {
                                new QuestionOption { Text = "Hashed with SHA-256 and rotated on every use", IsCorrect = true, SortOrder = 1, CreatedById = teacherUser.Id },
                                new QuestionOption { Text = "Plaintext strings without expiration", IsCorrect = false, SortOrder = 2, CreatedById = teacherUser.Id },
                                new QuestionOption { Text = "Base64 encoded in public URL query strings", IsCorrect = false, SortOrder = 3, CreatedById = teacherUser.Id }
                            }
                        },
                        new Question
                        {
                            Text = "What happens when a revoked refresh token is presented again (replay attack)?",
                            Marks = 10,
                            SortOrder = 2,
                            CreatedById = teacherUser.Id,
                            Options = new List<QuestionOption>
                            {
                                new QuestionOption { Text = "All active refresh tokens for that user are immediately revoked", IsCorrect = true, SortOrder = 1, CreatedById = teacherUser.Id },
                                new QuestionOption { Text = "The old token is accepted indefinitely", IsCorrect = false, SortOrder = 2, CreatedById = teacherUser.Id },
                                new QuestionOption { Text = "Rate limiting is disabled", IsCorrect = false, SortOrder = 3, CreatedById = teacherUser.Id }
                            }
                        }
                    }
                };
                db.Quizzes.Add(quiz);

                var enrollment = new Enrollment
                {
                    TenantId = defaultTenant.Id,
                    CourseId = sampleCourse.Id,
                    StudentProfileId = studentProfile.Id,
                    EnrolledAt = DateTime.UtcNow,
                    Status = EnrollmentStatus.Active,
                    CreatedById = adminUser.Id
                };
                db.Enrollments.Add(enrollment);

                await db.SaveChangesAsync();
                logger.LogInformation("Seeded initial portfolio course '{CourseCode}' with lessons, assignment, quiz, and student enrollment.", sampleCourse.CourseCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database initialization failed. Application cannot start safely.");
            throw;
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        string email,
        string fullName,
        string password,
        string roleName,
        int tenantId,
        ILogger logger)
    {
        var normalizedEmail = email.ToUpperInvariant();
        var user = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                TenantId = tenantId,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(user, roleName);
                logger.LogInformation("Seeded default {Role} user: {Email}", roleName, email);
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(user, roleName))
            {
                await userManager.AddToRoleAsync(user, roleName);
            }
            user.IsActive = true;
            user.EmailConfirmed = true;
            user.LockoutEnd = null;
            await userManager.UpdateAsync(user);
        }

        return user;
    }
}
