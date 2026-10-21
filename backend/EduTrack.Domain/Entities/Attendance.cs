namespace EduTrack.Domain.Entities;

public class ClassSession
{
    public int Id { get; set; }
    public int CourseOfferingId { get; set; }
    public CourseOffering? CourseOffering { get; set; }
    public DateOnly SessionDate { get; set; }
    public string? Topic { get; set; }
}

/// <summary>Attendance for one enrolment in one class session (unique per pair).</summary>
public class AttendanceRecord
{
    public int Id { get; set; }
    public int ClassSessionId { get; set; }
    public ClassSession? ClassSession { get; set; }
    public int EnrollmentId { get; set; }
    public Enrollment? Enrollment { get; set; }
    public AttendanceStatus Status { get; set; }
    public int RecordedByUserId { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>History of important academic changes (mark edits, publication, deactivation).</summary>
public class AuditLog
{
    public long Id { get; set; }
    public int ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
