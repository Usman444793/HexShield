namespace HexShield.Shared.Enums;

public enum EnrollmentStatus
{
    Active = 1,
    Completed = 2,
    Dropped = 3,
    Suspended = 4
}

public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3,
    Excused = 4
}

public enum SubmissionStatus
{
    Submitted = 1,
    Graded = 2,
    Returned = 3
}

public static class LmsRoles
{
    public const string Admin = "Admin";
    public const string Teacher = "Teacher";
    public const string Student = "Student";

    public static readonly IReadOnlyList<string> All = [Admin, Teacher, Student];
}
