namespace EduTrack.Application.Grading;

public static class MarkValidation
{
    /// <summary>Marks cannot be negative or exceed the assessment's maximum mark.</summary>
    public static bool TryValidate(decimal mark, decimal maxMark, out string? error)
    {
        if (maxMark <= 0)
        {
            error = "Assessment maximum mark must be greater than zero.";
            return false;
        }
        if (mark < 0)
        {
            error = "Mark cannot be negative.";
            return false;
        }
        if (mark > maxMark)
        {
            error = $"Mark cannot exceed the assessment maximum of {maxMark}.";
            return false;
        }

        error = null;
        return true;
    }
}
