using FluentAssertions;
using MatForge.Application.Features.Analytics;

namespace MatForge.UnitTests.Analytics;

public class AnalyticsQueryValidatorTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);
    private static readonly DateOnly LastAllowedTo = new(Today.Year + 1, 12, 31);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Missing_dates_pass(bool hasFrom, bool hasTo)
    {
        var date = new DateOnly(2026, 1, 1);

        Validate(hasFrom ? date : null, hasTo ? date : null).Should().BeEmpty();
    }

    [Fact]
    public void From_on_1_january_2000_passes()
    {
        Validate(new DateOnly(2000, 1, 1), null).Should().BeEmpty();
    }

    [Fact]
    public void From_before_2000_fails_on_from()
    {
        Validate(new DateOnly(1999, 12, 31), new DateOnly(2026, 1, 1)).Should().Equal("From");
    }

    [Fact]
    public void To_before_2000_fails_on_to()
    {
        Validate(null, new DateOnly(1999, 12, 31)).Should().Equal("To");
    }

    [Fact]
    public void Same_day_passes()
    {
        Validate(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1)).Should().BeEmpty();
    }

    [Fact]
    public void To_before_from_fails_on_to()
    {
        Validate(new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 1)).Should().Equal("To");
    }

    [Fact]
    public void From_today_without_to_passes()
    {
        Validate(Today, null).Should().BeEmpty();
    }

    [Fact]
    public void Future_from_without_to_fails_on_from()
    {
        Validate(Today.AddDays(1), null).Should().Equal("From");
    }

    [Fact]
    public void Future_from_with_a_later_to_passes()
    {
        Validate(Today.AddDays(1), Today.AddDays(2)).Should().BeEmpty();
    }

    [Fact]
    public void To_on_31_december_next_year_passes()
    {
        Validate(null, LastAllowedTo).Should().BeEmpty();
    }

    [Fact]
    public void To_after_next_year_fails_on_to()
    {
        Validate(null, LastAllowedTo.AddDays(1)).Should().Equal("To");
    }

    private static List<string> Validate(DateOnly? from, DateOnly? to) =>
        new AnalyticsQueryValidator().Validate(new AnalyticsQuery { From = from, To = to })
            .Errors.Select(e => e.PropertyName).ToList();
}
