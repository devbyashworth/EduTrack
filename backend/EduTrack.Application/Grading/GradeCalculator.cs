namespace EduTrack.Application.Grading;

/// <summary>An assessment's configuration plus the student's raw mark (null if not yet marked).</summary>
public sealed record AssessmentScore(decimal? Mark, decimal MaxMark, decimal Weight);

public sealed record GradeResult(
    /// <summary>Points earned toward the final mark so far, in percentage points of the whole course.</summary>
    decimal WeightedTotal,
    /// <summary>Total weight of assessments that have a mark.</summary>
    decimal WeightAssessed,
    /// <summary>True when every assessment has a mark, i.e. WeightedTotal is a final mark.</summary>
    bool IsComplete,
    /// <summary>Grade label; only set when the result is complete.</summary>
    string? Grade,
    /// <summary>Pass/fail; only set when the result is complete.</summary>
    bool? Passed);

/// <summary>
/// Central grade calculation. All weighting and grade-boundary logic lives here so it can be
/// tested in isolation and reused by every endpoint.
/// </summary>
public static class GradeCalculator
{
    public const int DecimalPlaces = 2;

    /// <summary>
    /// Weighted total = sum of (mark / maxMark) * weight. With weights in percentage points
    /// (20, 30, 50) the total is already a percentage.
    /// </summary>
    public static GradeResult Calculate(IEnumerable<AssessmentScore> scores, GradingPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentNullException.ThrowIfNull(policy);

        var list = scores.ToList();
        if (list.Count == 0)
            throw new ArgumentException("At least one assessment is required.", nameof(scores));

        foreach (var s in list)
        {
            if (s.MaxMark <= 0)
                throw new ArgumentException("Assessment maximum mark must be greater than zero.", nameof(scores));
            if (s.Weight < 0)
                throw new ArgumentException("Assessment weight cannot be negative.", nameof(scores));
            if (s.Mark is { } m && (m < 0 || m > s.MaxMark))
                throw new ArgumentException($"Mark {m} is outside the allowed range 0–{s.MaxMark}.", nameof(scores));
        }

        decimal total = 0m;
        decimal weightAssessed = 0m;
        foreach (var s in list.Where(x => x.Mark.HasValue))
        {
            total += s.Mark!.Value / s.MaxMark * s.Weight;
            weightAssessed += s.Weight;
        }

        total = Round(total);
        var complete = list.All(s => s.Mark.HasValue);

        return new GradeResult(
            WeightedTotal: total,
            WeightAssessed: weightAssessed,
            IsComplete: complete,
            Grade: complete ? policy.GradeFor(total) : null,
            Passed: complete ? total >= policy.PassMark : null);
    }

    /// <summary>Returns an error message if weights do not add up to the policy's required total, else null.</summary>
    public static string? ValidateWeights(IEnumerable<decimal> weights, GradingPolicy policy)
    {
        var total = weights.Sum();
        return total == policy.RequiredTotalWeight
            ? null
            : $"Assessment weights total {total}% but must total {policy.RequiredTotalWeight}%.";
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, DecimalPlaces, MidpointRounding.AwayFromZero);
}
