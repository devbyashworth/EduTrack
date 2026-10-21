namespace EduTrack.Domain;

public enum UserRole
{
    Administrator = 1,
    Lecturer = 2,
    Student = 3
}

public enum EnrollmentStatus
{
    Active = 1,
    Withdrawn = 2,
    Completed = 3
}

public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3,
    Excused = 4
}
