namespace EduTrack.Api.Auth;

public static class Roles
{
    public const string Administrator = "Administrator";
    public const string Lecturer = "Lecturer";
    public const string Student = "Student";
}

public static class Policies
{
    public const string Administrator = nameof(Administrator);
    public const string Lecturer = nameof(Lecturer);
    public const string Student = nameof(Student);

    /// <summary>Administrators and lecturers.</summary>
    public const string Staff = nameof(Staff);
}

public static class ClaimNames
{
    /// <summary>Id of the Student profile, present only for student accounts.</summary>
    public const string StudentId = "edutrack:student_id";

    /// <summary>Id of the Lecturer profile, present only for lecturer accounts.</summary>
    public const string LecturerId = "edutrack:lecturer_id";
}

public static class RateLimitPolicies
{
    public const string Login = "login";
}
