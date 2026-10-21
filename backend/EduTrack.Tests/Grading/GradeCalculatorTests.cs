using EduTrack.Application.Grading;

namespace EduTrack.Tests.Grading;

/// <summary>Covers acceptance test AT-03 (grade calculation) and AT-04 (mark validation).</summary>
public class GradeCalculatorTests
{
    private static readonly GradingPolicy Policy = GradingPolicy.Default();

    [Fact]
    public void Weighted_total_matches_the_documented_worked_example()
    {
        var result = GradeCalculator.Calculate(new[]
        {
            new AssessmentScore(80, 100, 20),
            new AssessmentScore(70, 100, 30),
            new AssessmentScore(75, 100, 50),
        }, Policy);

        Assert.Equal(74.5m, result.WeightedTotal);
        Assert.True(result.IsComplete);
        Assert.Equal("B", result.Grade);
        Assert.True(result.Passed);
    }

    [Fact]
    public void Marks_are_normalised_against_each_assessments_maximum()
    {
        var result = GradeCalculator.Calculate(new[]
        {
            new AssessmentScore(40, 50, 40),   // 80% of 40 = 32
            new AssessmentScore(90, 100, 60),  // 90% of 60 = 54
        }, Policy);

        Assert.Equal(86m, result.WeightedTotal);
        Assert.Equal("A", result.Grade);
    }

    [Fact]
    public void Incomplete_results_report_progress_but_no_grade_or_pass_status()
    {
        var result = GradeCalculator.Calculate(new[]
        {
            new AssessmentScore(80, 100, 20),
            new AssessmentScore(null, 100, 80),
        }, Policy);

        Assert.False(result.IsComplete);
        Assert.Equal(16m, result.WeightedTotal);
        Assert.Equal(20m, result.WeightAssessed);
        Assert.Null(result.Grade);
        Assert.Null(result.Passed);
    }

    [Theory]
    [InlineData(49.99, "F", false)]
    [InlineData(50, "D", true)]
    [InlineData(59.99, "D", true)]
    [InlineData(60, "C", true)]
    [InlineData(70, "B", true)]
    [InlineData(80, "A", true)]
    [InlineData(100, "A", true)]
    public void Grade_boundaries_follow_the_configured_policy(double percent, string grade, bool passed)
    {
        var result = GradeCalculator.Calculate(
            new[] { new AssessmentScore((decimal)percent, 100, 100) }, Policy);

        Assert.Equal(grade, result.Grade);
        Assert.Equal(passed, result.Passed);
    }

    [Fact]
    public void A_different_institutional_policy_changes_the_outcome()
    {
        var policy = new GradingPolicy(40m, new[]
        {
            new GradeBand("Distinction", 75m),
            new GradeBand("Merit", 60m),
            new GradeBand("Pass", 40m),
            new GradeBand("Fail", 0m),
        });

        var result = GradeCalculator.Calculate(new[] { new AssessmentScore(45, 100, 100) }, policy);

        Assert.Equal("Pass", result.Grade);
        Assert.True(result.Passed);
    }

    [Fact]
    public void Totals_are_rounded_to_two_decimals_away_from_zero()
    {
        var result = GradeCalculator.Calculate(
            new[] { new AssessmentScore(33.335m, 100, 100) }, Policy);

        Assert.Equal(33.34m, result.WeightedTotal);
    }

    [Theory]
    [InlineData(101, 100, 100)]   // mark above maximum
    [InlineData(-1, 100, 100)]    // negative mark
    [InlineData(1, 0, 100)]       // zero maximum
    [InlineData(1, 100, -5)]      // negative weight
    public void Invalid_scores_are_rejected(double mark, double max, double weight)
    {
        Assert.Throws<ArgumentException>(() => GradeCalculator.Calculate(
            new[] { new AssessmentScore((decimal)mark, (decimal)max, (decimal)weight) }, Policy));
    }

    [Fact]
    public void An_empty_assessment_list_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            GradeCalculator.Calculate(Array.Empty<AssessmentScore>(), Policy));
    }

    [Fact]
    public void Weights_must_total_the_policys_required_weight()
    {
        Assert.Null(GradeCalculator.ValidateWeights(new[] { 20m, 30m, 50m }, Policy));
        Assert.NotNull(GradeCalculator.ValidateWeights(new[] { 20m, 30m, 40m }, Policy));
        Assert.NotNull(GradeCalculator.ValidateWeights(new[] { 60m, 60m }, Policy));
    }

    [Theory]
    [InlineData(0, 100, true)]
    [InlineData(100, 100, true)]
    [InlineData(-0.01, 100, false)]
    [InlineData(100.01, 100, false)]
    [InlineData(10, 0, false)]
    public void Mark_validation_enforces_the_zero_to_maximum_range(double mark, double max, bool valid)
    {
        var ok = MarkValidation.TryValidate((decimal)mark, (decimal)max, out var error);

        Assert.Equal(valid, ok);
        Assert.Equal(valid, error is null);
    }

    [Fact]
    public void Policy_requires_a_band_starting_at_zero_and_distinct_minimums()
    {
        Assert.Throws<ArgumentException>(() => new GradingPolicy(50, new[] { new GradeBand("A", 50m) }));
        Assert.Throws<ArgumentException>(() => new GradingPolicy(50, new[]
        {
            new GradeBand("A", 50m), new GradeBand("B", 50m), new GradeBand("F", 0m),
        }));
    }
}
