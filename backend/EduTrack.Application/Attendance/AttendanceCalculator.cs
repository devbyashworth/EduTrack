using EduTrack.Domain;

namespace EduTrack.Application.Attendance;

public sealed record AttendanceSummary(
    int Total,
    int Present,
    int Late,
    int Absent,
    int Excused,
    /// <summary>Attendance percentage, or null when there are no countable sessions yet.</summary>
    decimal? Percentage);

public static class AttendanceCalculator
{
    /// <summary>
    /// Excused sessions are excluded from the denominator. Late counts as attended by default;
    /// pass <paramref name="countLateAsAttended"/> = false for institutions that treat late as absent.
    /// </summary>
    public static AttendanceSummary Summarise(
        IEnumerable<AttendanceStatus> statuses,
        bool countLateAsAttended = true)
    {
        var list = statuses.ToList();
        int present = list.Count(s => s == AttendanceStatus.Present);
        int late = list.Count(s => s == AttendanceStatus.Late);
        int absent = list.Count(s => s == AttendanceStatus.Absent);
        int excused = list.Count(s => s == AttendanceStatus.Excused);

        int attended = present + (countLateAsAttended ? late : 0);
        int countable = list.Count - excused;

        decimal? pct = countable == 0
            ? null
            : Math.Round((decimal)attended / countable * 100m, 2, MidpointRounding.AwayFromZero);

        return new AttendanceSummary(list.Count, present, late, absent, excused, pct);
    }
}
