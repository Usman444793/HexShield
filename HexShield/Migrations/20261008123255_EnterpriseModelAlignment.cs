using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HexShield.Migrations
{
    /// <inheritdoc />
    public partial class EnterpriseModelAlignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_AspNetUsers_GradedById",
                table: "Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_StudentProfiles_StudentProfileId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_AssignmentId_StudentProfileId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_GradedById",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "GradedAt",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "GradedById",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "Submissions");

            migrationBuilder.RenameColumn(
                name: "DurationMinutes",
                table: "Quizzes",
                newName: "TimeLimitMinutes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAt",
                table: "Submissions",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<int>(
                name: "StudentProfileId",
                table: "Submissions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Feedback",
                table: "Submissions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Submissions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 10000);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "Submissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Grade",
                table: "Submissions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Submissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "Submissions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Submissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "Quizzes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LessonId",
                table: "Quizzes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxAttempts",
                table: "Quizzes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PassPercentage",
                table: "Quizzes",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Quizzes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartedAt",
                table: "QuizAttempts",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "QuizAttempts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedAt",
                table: "QuizAttempts",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Passed",
                table: "QuizAttempts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "QuizAttempts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "QuizAttempts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPossibleScore",
                table: "QuizAttempts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrect",
                table: "QuizAnswers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SelectedOptionId",
                table: "QuizAnswers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "LessonProgresses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Enrollments",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EnrolledAt",
                table: "Enrollments",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Enrollments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Assignments",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxScore",
                table: "Assignments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "Assignments",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Assignments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<int>(
                name: "LessonId",
                table: "Assignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Assignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    LessonId = table.Column<int>(type: "int", nullable: true),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SessionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attendances_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attendances_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attendances_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attendances_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AssignmentId_StudentId",
                table: "Submissions",
                columns: new[] { "AssignmentId", "StudentId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_StudentId",
                table: "Submissions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_TenantId",
                table: "Submissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_LessonId",
                table: "Quizzes",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_TenantId",
                table: "Quizzes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_TenantId",
                table: "QuizAttempts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_TenantId",
                table: "Lessons",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonProgresses_TenantId",
                table: "LessonProgresses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_TenantId",
                table: "Enrollments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_LessonId",
                table: "Assignments",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TenantId",
                table: "Assignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_CourseId",
                table: "Attendances",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_LessonId",
                table: "Attendances",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_StudentId_LessonId_TenantId",
                table: "Attendances",
                columns: new[] { "StudentId", "LessonId", "TenantId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_TenantId",
                table: "Attendances",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Lessons_LessonId",
                table: "Assignments",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Tenants_TenantId",
                table: "Assignments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Tenants_TenantId",
                table: "Enrollments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LessonProgresses_Tenants_TenantId",
                table: "LessonProgresses",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_Tenants_TenantId",
                table: "Lessons",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuizAttempts_Tenants_TenantId",
                table: "QuizAttempts",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Lessons_LessonId",
                table: "Quizzes",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Tenants_TenantId",
                table: "Quizzes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_AspNetUsers_StudentId",
                table: "Submissions",
                column: "StudentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_StudentProfiles_StudentProfileId",
                table: "Submissions",
                column: "StudentProfileId",
                principalTable: "StudentProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Tenants_TenantId",
                table: "Submissions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Lessons_LessonId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Tenants_TenantId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Tenants_TenantId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_LessonProgresses_Tenants_TenantId",
                table: "LessonProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_Tenants_TenantId",
                table: "Lessons");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizAttempts_Tenants_TenantId",
                table: "QuizAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Lessons_LessonId",
                table: "Quizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Tenants_TenantId",
                table: "Quizzes");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_AspNetUsers_StudentId",
                table: "Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_StudentProfiles_StudentProfileId",
                table: "Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Tenants_TenantId",
                table: "Submissions");

            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_AssignmentId_StudentId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_StudentId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_TenantId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_LessonId",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_TenantId",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_QuizAttempts_TenantId",
                table: "QuizAttempts");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_TenantId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_LessonProgresses_TenantId",
                table: "LessonProgresses");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_TenantId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_LessonId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_TenantId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "LessonId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "MaxAttempts",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "PassPercentage",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "Passed",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "TotalPossibleScore",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "IsCorrect",
                table: "QuizAnswers");

            migrationBuilder.DropColumn(
                name: "SelectedOptionId",
                table: "QuizAnswers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "LessonProgresses");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "LessonId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Assignments");

            migrationBuilder.RenameColumn(
                name: "TimeLimitMinutes",
                table: "Quizzes",
                newName: "DurationMinutes");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "SubmittedAt",
                table: "Submissions",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "StudentProfileId",
                table: "Submissions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Feedback",
                table: "Submissions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Submissions",
                type: "nvarchar(max)",
                maxLength: 10000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GradedAt",
                table: "Submissions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradedById",
                table: "Submissions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "Submissions",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "QuizAttempts",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "QuizAttempts",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "QuizAttempts",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Enrollments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "EnrolledAt",
                table: "Enrollments",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Assignments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<int>(
                name: "MaxScore",
                table: "Assignments",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "DueDate",
                table: "Assignments",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Assignments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AssignmentId_StudentProfileId",
                table: "Submissions",
                columns: new[] { "AssignmentId", "StudentProfileId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_GradedById",
                table: "Submissions",
                column: "GradedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_AspNetUsers_GradedById",
                table: "Submissions",
                column: "GradedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_StudentProfiles_StudentProfileId",
                table: "Submissions",
                column: "StudentProfileId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
