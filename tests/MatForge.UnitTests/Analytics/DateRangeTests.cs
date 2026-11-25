using System.Globalization;
using FluentAssertions;
using MatForge.Application.Features.Analytics.Calculators;

namespace MatForge.UnitTests.Analytics;

public class DateRangeTests
{
    private static readonly DateOnly Today = new(2026, 11, 25);

    [Theory]
    [InlineData("2026-09-01", "2026-09-01", 1)]
    [InlineData("2026-09-01", "2026-09-28", 28)]
    [InlineData("2024-02-28", "2024-03-01", 3)]
    [InlineData("2026-12-31", "2027-01-01", 2)]
    public void Days_counts_both_ends(string from, string to, int days)
    {
        Range(from, to).Days.Should().Be(days);
    }

    [Theory]
    [InlineData("2026-09-15", "2026-09-15", "2026-09-14", "2026-09-14")]
    [InlineData("2026-09-01", "2026-09-28", "2026-08-04", "2026-08-31")]
    [InlineData("2026-03-01", "2026-03-31", "2026-01-29", "2026-02-28")]
    [InlineData("2024-03-01", "2024-03-31", "2024-01-30", "2024-02-29")]
    [InlineData("2026-01-01", "2026-01-10", "2025-12-22", "2025-12-31")]
    public void Previous_has_the_same_length_and_ends_the_day_before(string from, string to, string previousFrom,
        string previousTo)
    {
        var previous = Range(from, to).Previous;

        previous.Should().Be(Range(previousFrom, previousTo));
        previous.Days.Should().Be(Range(from, to).Days);
    }

    [Theory]
    [InlineData("2026-09-09", false)]
    [InlineData("2026-09-10", true)]
    [InlineData("2026-09-20", true)]
    [InlineData("2026-09-21", false)]
    public void Contains_both_ends_only(string date, bool expected)
    {
        Range("2026-09-10", "2026-09-20").Contains(Date(date)).Should().Be(expected);
    }

    [Fact]
    public void Given_dates_are_kept()
    {
        DateRange.Resolve(Date("2026-09-01"), Date("2026-09-28"), Date("2025-01-01"), Today)
            .Should().Be(Range("2026-09-01", "2026-09-28"));
    }

    [Fact]
    public void Missing_to_is_today()
    {
        DateRange.Resolve(Date("2026-09-01"), null, null, Today).Should().Be(new DateRange(Date("2026-09-01"), Today));
    }

    [Fact]
    public void Missing_from_is_the_earliest_data()
    {
        DateRange.Resolve(null, null, Date("2026-03-10"), Today).Should().Be(new DateRange(Date("2026-03-10"), Today));
    }

    [Fact]
    public void Missing_from_is_never_later_than_to()
    {
        DateRange.Resolve(null, Date("2026-06-30"), Date("2026-07-01"), Today)
            .Should().Be(Range("2026-06-30", "2026-06-30"));
    }

    [Fact]
    public void Missing_from_equals_to_without_data()
    {
        DateRange.Resolve(null, null, null, Today).Should().Be(new DateRange(Today, Today));
        DateRange.Resolve(null, Date("2026-06-30"), null, Today).Should().Be(Range("2026-06-30", "2026-06-30"));
    }

    [Fact]
    public void Missing_from_is_not_before_2000()
    {
        DateRange.Resolve(null, null, Date("1995-05-05"), Today).Should().Be(new DateRange(Date("2000-01-01"), Today));
    }

    private static DateOnly Date(string value) => DateOnly.Parse(value, CultureInfo.InvariantCulture);

    private static DateRange Range(string from, string to) => new(Date(from), Date(to));
}
