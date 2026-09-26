namespace HexShield.Models.Identity;

public static class LmsRoles
{
    public const string Admin = "Admin";
    public const string Teacher = "Teacher";
    public const string Student = "Student";

    public static readonly IReadOnlyList<string> All = [Admin, Teacher, Student];
}
