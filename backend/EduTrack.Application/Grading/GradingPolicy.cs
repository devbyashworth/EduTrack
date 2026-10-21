namespace EduTrack.Application.Grading;

/// <summary>A grade label that starts at a minimum percentage (e.g. "A" from 80).</summary>
public sealed record GradeBand(string Grade, decimal MinPercent);

/// <summary>
/// Institution-specific grading rules. Nothing here is hard-coded into the calculator, because
/// grade boundaries and pass requirements differ between institutions.
/// </summary>
public sealed class GradingPolicy
{
    public GradingPolicy(decimal passMark, IEnumerable<GradeBand> bands)
    {
        if (passMark < 0 || passMark > 100)
            throw new ArgumentOutOfRangeException(nameof(passMark), "Pass mark must be between 0 and 100.");

        var ordered = bands.OrderByDescending(b => b.MinPercent).ToList();
        if (ordered.Count == 0)
            throw new ArgumentException("At least one grade band is required.", nameof(bands));
        if (ordered.Any(b => b.MinPercent < 0 || b.MinPercent > 100))
            throw new ArgumentException("Grade band minimums must be between 0 and 100.", nameof(bands));
        if (ordered.Select(b => b.MinPercent).Distinct().Count() != ordered.Count)
            throw new ArgumentException("Grade bands must have distinct minimums.", nameof(bands));
        if (ordered[^1].MinPercent != 0)
            throw new ArgumentException("The lowest grade band must start at 0 so every mark maps to a grade.", nameof(bands));

        PassMark = passMark;
        Bands = ordered;
    }

    public decimal PassMark { get; }

    /// <summary>Bands ordered from highest to lowest minimum.</summary>
    public IReadOnlyList<GradeBand> Bands { get; }

    /// <summary>Required total of all assessment weights, in percentage points.</summary>
    public decimal RequiredTotalWeight { get; init; } = 100m;

    /// <summary>A common 5-band scale, used only as a default for demos and tests.</summary>
    public static GradingPolicy Default() => new(
        passMark: 50m,
        bands: new[]
        {
            new GradeBand("A", 80m),
            new GradeBand("B", 70m),
            new GradeBand("C", 60m),
            new GradeBand("D", 50m),
            new GradeBand("F", 0m),
        });

    public string GradeFor(decimal percent) =>
        Bands.First(b => percent >= b.MinPercent).Grade;
}
