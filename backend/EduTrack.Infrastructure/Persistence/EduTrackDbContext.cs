using EduTrack.Domain;
using EduTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Infrastructure.Persistence;

public class EduTrackDbContext : DbContext
{
    public EduTrackDbContext(DbContextOptions<EduTrackDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Lecturer> Lecturers => Set<Lecturer>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<AcademicTerm> AcademicTerms => Set<AcademicTerm>();
    public DbSet<CourseOffering> CourseOfferings => Set<CourseOffering>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentResult> AssessmentResults => Set<AssessmentResult>();
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<Student>(e =>
        {
            e.Property(x => x.StudentNumber).HasMaxLength(20).IsRequired();
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(30);
            e.HasIndex(x => x.StudentNumber).IsUnique();
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.User).WithOne().HasForeignKey<Student>(x => x.UserId);
        });

        b.Entity<Lecturer>(e =>
        {
            e.Property(x => x.EmployeeNumber).HasMaxLength(20).IsRequired();
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.EmployeeNumber).IsUnique();
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.User).WithOne().HasForeignKey<Lecturer>(x => x.UserId);
        });

        b.Entity<Course>(e =>
        {
            e.Property(x => x.CourseCode).HasMaxLength(20).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.CourseCode).IsUnique();
        });

        b.Entity<AcademicTerm>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.StartDate);
        });

        b.Entity<CourseOffering>(e =>
        {
            e.HasIndex(x => x.TermId);
            e.HasIndex(x => x.LecturerId);
            e.HasIndex(x => new { x.CourseId, x.TermId });
        });

        b.Entity<Enrollment>(e =>
        {
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            // One enrolment per student per course offering. A withdrawn student is
            // re-activated rather than given a second row.
            e.HasIndex(x => new { x.StudentId, x.CourseOfferingId }).IsUnique();
            e.HasIndex(x => x.CourseOfferingId);
        });

        b.Entity<Assessment>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.MaxMark).HasPrecision(7, 2);
            e.Property(x => x.Weight).HasPrecision(5, 2);
            e.HasIndex(x => x.CourseOfferingId);
        });

        b.Entity<AssessmentResult>(e =>
        {
            e.Property(x => x.Mark).HasPrecision(7, 2);
            // A student has at most one mark per assessment.
            e.HasIndex(x => new { x.AssessmentId, x.EnrollmentId }).IsUnique();
            e.HasIndex(x => x.EnrollmentId);
        });

        b.Entity<ClassSession>(e =>
        {
            e.Property(x => x.Topic).HasMaxLength(200);
            e.HasIndex(x => new { x.CourseOfferingId, x.SessionDate });
        });

        b.Entity<AttendanceRecord>(e =>
        {
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            // Duplicate attendance for the same enrolment and session is impossible.
            e.HasIndex(x => new { x.ClassSessionId, x.EnrollmentId }).IsUnique();
            e.HasIndex(x => x.EnrollmentId);
        });

        b.Entity<AuditLog>(e =>
        {
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Details).HasMaxLength(2000);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => x.CreatedAt);
        });

        // SQL Server's default collation compares text case-insensitively, but SQLite does not.
        // Match SQL Server so "S0001" and "s0001" (or two emails differing only by case) collide
        // in development exactly as they will in production.
        if (Database.IsSqlite())
        {
            b.Entity<User>().Property(x => x.Email).UseCollation("NOCASE");
            b.Entity<Student>().Property(x => x.StudentNumber).UseCollation("NOCASE");
            b.Entity<Lecturer>().Property(x => x.EmployeeNumber).UseCollation("NOCASE");
            b.Entity<Course>().Property(x => x.CourseCode).UseCollation("NOCASE");
        }

        // Academic history must be preserved, so there are no cascading deletes anywhere.
        // This runs last so it also covers relationships configured explicitly above.
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
