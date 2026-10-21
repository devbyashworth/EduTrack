namespace EduTrack.Domain.Entities;

/// <summary>An assignment, test or examination within a course offering.</summary>
public class Assessment
{
    public int Id { get; set; }
    public int CourseOfferingId { get; set; }
    public CourseOffering? CourseOffering { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>The mark a student can score at most (e.g. 100, or 40 for a short quiz).</summary>
    public decimal MaxMark { get; set; }

    /// <summary>Contribution to the final mark in percentage points (20 means 20%).</summary>
    public decimal Weight { get; set; }

    /// <summary>Students can only see marks once the assessment is published.</summary>
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int? PublishedByUserId { get; set; }

    public ICollection<AssessmentResult> Results { get; set; } = new List<AssessmentResult>();
}

/// <summary>A student's raw mark for one assessment. Raw marks are stored so calculations can be audited.</summary>
public class AssessmentResult
{
    public int Id { get; set; }
    public int AssessmentId { get; set; }
    public Assessment? Assessment { get; set; }
    public int EnrollmentId { get; set; }
    public Enrollment? Enrollment { get; set; }
    public decimal Mark { get; set; }
    public int RecordedByUserId { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
