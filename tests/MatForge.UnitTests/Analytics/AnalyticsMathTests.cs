using System.Globalization;
using FluentAssertions;
using MatForge.Application.Features.Analytics.Calculators;

namespace MatForge.UnitTests.Analytics;

public class AnalyticsMathTests
{
    [Theory]
    [InlineData(2, 3, "0.6667")]
    [InlineData(1, 3, "0.3333")]
    [InlineData(1, 8, "0.125")]
    [InlineData(5, 6, "0.8333")]
    [InlineData(0, 5, "0")]
    [InlineData(5, 5, "1")]
    public void Share_has_four_decimals(int count, int total, string expected)
    {
        AnalyticsMath.Share(count, total).Should().Be(Number(expected));
    }

    [Theory]
    [InlineData(1, 32, "0.0313")]
    [InlineData(5, 32, "0.1563")]
    public void Share_rounds_midpoints_away_from_zero(int count, int total, string expected)
    {
        AnalyticsMath.Share(count, total).Should().Be(Number(expected));
    }

    [Fact]
    public void Win_rate_is_null_without_rounds()
    {
        AnalyticsMath.WinRate(0, 0).Should().BeNull();
    }

    [Fact]
    public void Win_rate_is_a_rounded_share()
    {
        AnalyticsMath.WinRate(7, 15).Should().Be(0.4667m);
    }

    [Theory]
    [InlineData(12, 20, 7, 15, "0.1333")]
    [InlineData(7, 15, 12, 20, "-0.1333")]
    [InlineData(10, 20, 15, 32, "0.0313")]
    [InlineData(25, 41, 26, 51, "0.1")]
    public void Rate_difference_subtracts_the_unrounded_rates_and_rounds_once(int wins, int total, int otherWins,
        int otherTotal, string expected)
    {
        AnalyticsMath.RateDifference(wins, total, otherWins, otherTotal).Should().Be(Number(expected));
    }

    [Fact]
    public void Rate_difference_differs_from_subtracting_rounded_rates()
    {
        // The rounded rates 0.6667 and 0.4333 would give 0.2334
        AnalyticsMath.RateDifference(2, 3, 13, 30).Should().Be(0.2333m);
    }

    private static decimal Number(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
}
