using System.Globalization;
using FluentAssertions;
using MatForge.Application.Features.Analytics.Calculators;

namespace MatForge.UnitTests.Analytics;

public class DateBucketsTests
{
    [Theory]
    [InlineData("2026-08-31")]
    [InlineData("2026-09-01")]
    [InlineData("2026-09-02")]
    [InlineData("2026-09-03")]
    [InlineData("2026-09-04")]
    [InlineData("2026-09-05")]
    [InlineData("2026-09-06")]
    public void Week_starts_on_monday(string date)
    {
        DateBuckets.Start(Date(date), Bucket.Week).Should().Be(Date("2026-08-31"));
    }

    [Theory]
    [InlineData("2024-02-29", "2024-02-01")]
    [InlineData("2026-12-31", "2026-12-01")]
    [InlineData("2026-12-01", "2026-12-01")]
    public void Month_starts_on_the_first(string date, string start)
    {
        DateBuckets.Start(Date(date), Bucket.Month).Should().Be(Date(start));
    }

    [Theory]
    [InlineData("2026-01-01", "2026-01-01", Bucket.Week)]
    [InlineData("2026-01-01", "2026-04-02", Bucket.Week)]
    [InlineData("2026-01-01", "2026-04-03", Bucket.Month)]
    public void Bucket_is_week_up_to_92_days_and_month_from_93(string from, string to, Bucket bucket)
    {
        DateBuckets.For(Range(from, to)).Should().Be(bucket);
    }

    [Fact]
    public void Weeks_cover_partial_weeks_at_both_ends()
    {
        // From Sunday to Monday
        DateBuckets.Starts(Range("2026-09-06", "2026-09-14"), Bucket.Week)
            .Should().Equal(Date("2026-08-31"), Date("2026-09-07"), Date("2026-09-14"));
    }

    [Fact]
    public void One_day_period_has_one_week()
    {
        DateBuckets.Starts(Range("2026-09-06", "2026-09-06"), Bucket.Week).Should().Equal(Date("2026-08-31"));
    }

    [Fact]
    public void Weeks_cross_the_year_boundary_by_their_monday()
    {
        // ISO week 53 of 2020 starts on 2020-12-28, week 1 of 2021 on 2021-01-04
        DateBuckets.Starts(Range("2020-12-31", "2021-01-04"), Bucket.Week)
            .Should().Equal(Date("2020-12-28"), Date("2021-01-04"));
    }

    [Fact]
    public void Months_cover_partial_months_at_both_ends()
    {
        DateBuckets.Starts(Range("2026-01-15", "2026-03-10"), Bucket.Month)
            .Should().Equal(Date("2026-01-01"), Date("2026-02-01"), Date("2026-03-01"));
    }

    [Fact]
    public void Months_cross_the_year_boundary()
    {
        DateBuckets.Starts(Range("2026-11-20", "2027-02-03"), Bucket.Month)
            .Should().Equal(Date("2026-11-01"), Date("2026-12-01"), Date("2027-01-01"), Date("2027-02-01"));
    }

    private static DateOnly Date(string value) => DateOnly.Parse(value, CultureInfo.InvariantCulture);

    private static DateRange Range(string from, string to) => new(Date(from), Date(to));
}
