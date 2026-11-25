using FluentAssertions;
using MatForge.Application.Features.Competitions;

namespace MatForge.UnitTests.Competitions;

public class CompetitionListQueryValidatorTests
{
    private static readonly DateOnly From = new(2026, 1, 1);

    [Theory]
    [InlineData(0)]
    [InlineData(5000)]
    public void Range_in_order_passes_at_any_length(int days)
    {
        Validate(From, From.AddDays(days)).Should().BeEmpty();
    }

    [Fact]
    public void To_before_from_fails_on_to()
    {
        Validate(From, From.AddDays(-1)).Should().Equal("To");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Missing_dates_pass(bool hasFrom, bool hasTo)
    {
        Validate(hasFrom ? From : null, hasTo ? From : null).Should().BeEmpty();
    }

    private static List<string> Validate(DateOnly? from, DateOnly? to) =>
        new CompetitionListQueryValidator().Validate(new CompetitionListQuery { From = from, To = to })
            .Errors.Select(e => e.PropertyName).ToList();
}
