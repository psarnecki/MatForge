using FluentAssertions;
using MatForge.Application.Features.Sessions;

namespace MatForge.UnitTests.Sessions;

public class SessionListQueryValidatorTests
{
    private static readonly DateOnly From = new(2026, 1, 1);

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(400)]
    public void Range_of_at_most_400_days_passes(int days)
    {
        Validate(From, From.AddDays(days)).Should().BeEmpty();
    }

    [Fact]
    public void Range_of_401_days_fails_on_to()
    {
        Validate(From, From.AddDays(401)).Should().Equal("To");
    }

    [Fact]
    public void To_before_from_fails_on_to()
    {
        Validate(From, From.AddDays(-1)).Should().Equal("To");
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Missing_date_is_left_to_the_required_rule(bool hasFrom, bool hasTo)
    {
        Validate(hasFrom ? From : null, hasTo ? From : null).Should().BeEmpty();
    }

    private static List<string> Validate(DateOnly? from, DateOnly? to) =>
        new SessionListQueryValidator().Validate(new SessionListQuery { From = from, To = to })
            .Errors.Select(e => e.PropertyName).ToList();
}
