namespace EduTrack.Domain.Entities;

/// <summary>Reusable course catalogue record (e.g. "CS101").</summary>
public class Course
{
    public int Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Credits { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AcademicTerm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

/// <summary>One course delivered in a specific term by a specific lecturer.</summary>
public class CourseOffering
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public int TermId { get; set; }
    public AcademicTerm? Term { get; set; }
    public int LecturerId { get; set; }
    public Lecturer? Lecturer { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
    public ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();
}

/// <summary>Connects a student to a course offering.</summary>
public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int CourseOfferingId { get; set; }
    public CourseOffering? CourseOffering { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
}
