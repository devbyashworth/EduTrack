using EduTrack.Domain;
using EduTrack.Domain.Entities;
using EduTrack.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Tests.Database;

/// <summary>
/// Verifies that database integrity rules are enforced by real constraints, not just by application
/// code. Uses in-memory SQLite, which enforces unique indexes and foreign keys (EF InMemory does not).
/// Covers AT-01, AT-02 and AT-07.
/// </summary>
public sealed class DatabaseConstraintTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<EduTrackDbContext> _options;

    public DatabaseConstraintTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<EduTrackDbContext>().UseSqlite(_connection).Options;

        using var db = new EduTrackDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private EduTrackDbContext NewContext() => new(_options);

    private static User NewUser(string email, UserRole role) =>
        new() { Email = email, PasswordHash = "not-a-real-hash", Role = role };

    /// <summary>Seeds one student, one lecturer, one term, one course offering and one enrolment.</summary>
    private (int studentId, int offeringId, int enrollmentId) SeedEnrollment()
    {
        using var db = NewContext();
        var student = new Student
        {
            StudentNumber = "S0001", FirstName = "Test", LastName = "Student",
            User = NewUser("s1@example.test", UserRole.Student),
        };
        var lecturer = new Lecturer
        {
            EmployeeNumber = "E0001", FirstName = "Test", LastName = "Lecturer",
            User = NewUser("l1@example.test", UserRole.Lecturer),
        };
        var course = new Course { CourseCode = "CS101", Name = "Intro", Credits = 12 };
        var term = new AcademicTerm
        {
            Name = "2026 S1", StartDate = new DateOnly(2026, 2, 1), EndDate = new DateOnly(2026, 6, 30),
        };
        var offering = new CourseOffering { Course = course, Term = term, Lecturer = lecturer };
        var enrollment = new Enrollment { Student = student, CourseOffering = offering };
        db.Add(enrollment);
        db.SaveChanges();
        return (student.Id, offering.Id, enrollment.Id);
    }

    [Fact]
    public void Duplicate_student_numbers_are_rejected()
    {
        using var db = NewContext();
        db.Add(new Student { StudentNumber = "S1", FirstName = "A", LastName = "A", User = NewUser("a@example.test", UserRole.Student) });
        db.SaveChanges();

        db.Add(new Student { StudentNumber = "S1", FirstName = "B", LastName = "B", User = NewUser("b@example.test", UserRole.Student) });
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void Uniqueness_ignores_letter_case_like_SQL_Server()
    {
        using var db = NewContext();
        db.Add(new Student { StudentNumber = "S1", FirstName = "A", LastName = "A", User = NewUser("Mixed@Example.test", UserRole.Student) });
        db.Add(new Course { CourseCode = "CS101", Name = "One", Credits = 12 });
        db.SaveChanges();

        // Same student number, different case.
        db.Add(new Student { StudentNumber = "s1", FirstName = "B", LastName = "B", User = NewUser("other@example.test", UserRole.Student) });
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
        db.ChangeTracker.Clear();

        // Same course code, different case.
        db.Add(new Course { CourseCode = "cs101", Name = "Two", Credits = 12 });
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
        db.ChangeTracker.Clear();

        // Same email, different case.
        db.Add(NewUser("mixed@example.test", UserRole.Lecturer));
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void Duplicate_course_codes_are_rejected()
    {
        using var db = NewContext();
        db.Add(new Course { CourseCode = "CS101", Name = "One", Credits = 12 });
        db.SaveChanges();

        db.Add(new Course { CourseCode = "CS101", Name = "Two", Credits = 12 });
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void Duplicate_account_emails_are_rejected()
    {
        using var db = NewContext();
        db.Add(NewUser("same@example.test", UserRole.Student));
        db.SaveChanges();

        db.Add(NewUser("same@example.test", UserRole.Lecturer));
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void A_student_cannot_be_enrolled_twice_in_the_same_course_offering()
    {
        var (studentId, offeringId, _) = SeedEnrollment();

        using var db = NewContext();
        db.Add(new Enrollment { StudentId = studentId, CourseOfferingId = offeringId });
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void Attendance_cannot_be_recorded_twice_for_the_same_enrolment_and_session()
    {
        var (_, offeringId, enrollmentId) = SeedEnrollment();

        using var db = NewContext();
        var session = new ClassSession { CourseOfferingId = offeringId, SessionDate = new DateOnly(2026, 3, 2) };
        db.Add(session);
        db.SaveChanges();

        db.Add(new AttendanceRecord { ClassSessionId = session.Id, EnrollmentId = enrollmentId, Status = AttendanceStatus.Present, RecordedByUserId = 1 });
        db.SaveChanges();

        db.Add(new AttendanceRecord { ClassSessionId = session.Id, EnrollmentId = enrollmentId, Status = AttendanceStatus.Absent, RecordedByUserId = 1 });
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void A_student_has_at_most_one_mark_per_assessment()
    {
        var (_, offeringId, enrollmentId) = SeedEnrollment();

        using var db = NewContext();
        var assessment = new Assessment { CourseOfferingId = offeringId, Name = "Test 1", MaxMark = 100, Weight = 100 };
        db.Add(assessment);
        db.SaveChanges();

        db.Add(new AssessmentResult { AssessmentId = assessment.Id, EnrollmentId = enrollmentId, Mark = 70, RecordedByUserId = 1 });
        db.SaveChanges();

        db.Add(new AssessmentResult { AssessmentId = assessment.Id, EnrollmentId = enrollmentId, Mark = 80, RecordedByUserId = 1 });
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void Foreign_keys_prevent_enrolments_for_unknown_students()
    {
        var (_, offeringId, _) = SeedEnrollment();

        using var db = NewContext();
        db.Add(new Enrollment { StudentId = 9999, CourseOfferingId = offeringId });
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void Deleting_a_student_who_has_enrolments_is_blocked_to_preserve_history()
    {
        var (studentId, _, _) = SeedEnrollment();

        using var db = NewContext();
        var student = db.Students.Single(s => s.Id == studentId);
        db.Remove(student);
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void Marks_keep_two_decimal_places()
    {
        var (_, offeringId, enrollmentId) = SeedEnrollment();
        int assessmentId;
        using (var db = NewContext())
        {
            var assessment = new Assessment { CourseOfferingId = offeringId, Name = "Exam", MaxMark = 100, Weight = 100 };
            db.Add(assessment);
            db.SaveChanges();
            assessmentId = assessment.Id;
            db.Add(new AssessmentResult { AssessmentId = assessmentId, EnrollmentId = enrollmentId, Mark = 74.25m, RecordedByUserId = 1 });
            db.SaveChanges();
        }

        using var read = NewContext();
        Assert.Equal(74.25m, read.AssessmentResults.Single(r => r.AssessmentId == assessmentId).Mark);
    }
}
