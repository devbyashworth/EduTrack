using EduTrack.Application.Attendance;
using EduTrack.Domain;

namespace EduTrack.Tests.Attendance;

public class AttendanceCalculatorTests
{
    private static readonly AttendanceStatus[] Sample =
    {
        AttendanceStatus.Present, AttendanceStatus.Present, AttendanceStatus.Late,
        AttendanceStatus.Absent, AttendanceStatus.Excused,
    };

    [Fact]
    public void Excused_sessions_are_excluded_and_late_counts_as_attended_by_default()
    {
        var summary = AttendanceCalculator.Summarise(Sample);

        Assert.Equal(5, summary.Total);
        Assert.Equal(1, summary.Excused);
        Assert.Equal(75m, summary.Percentage); // 3 attended of 4 countable
    }

    [Fact]
    public void Late_can_be_treated_as_absent_when_the_institution_requires_it()
    {
        var summary = AttendanceCalculator.Summarise(Sample, countLateAsAttended: false);

        Assert.Equal(50m, summary.Percentage); // 2 attended of 4 countable
    }

    [Fact]
    public void No_countable_sessions_gives_no_percentage()
    {
        Assert.Null(AttendanceCalculator.Summarise(Array.Empty<AttendanceStatus>()).Percentage);
        Assert.Null(AttendanceCalculator.Summarise(new[] { AttendanceStatus.Excused }).Percentage);
    }
}
