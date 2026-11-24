using FluentAssertions;
using MatForge.Application.Features.Competitions;
using MatForge.Data.Enums;

namespace MatForge.UnitTests.Competitions;

public class CompetitionRequestFieldsValidatorTests
{
    [Fact]
    public void Competition_without_fights_passes()
    {
        Validate(Competition()).Should().BeEmpty();
    }

    [Fact]
    public void Draw_without_details_passes()
    {
        Validate(Competition(Fight(FightResult.Draw))).Should().BeEmpty();
    }

    [Fact]
    public void Draw_with_method_fails_on_method()
    {
        Validate(Competition(Fight(FightResult.Draw, FightMethod.Points))).Should().Equal("Fights[0].Method");
    }

    [Fact]
    public void Draw_with_technique_fails_on_technique_id()
    {
        Validate(Competition(Fight(FightResult.Draw, techniqueId: 1))).Should().Equal("Fights[0].TechniqueId");
    }

    [Fact]
    public void Draw_with_method_and_technique_fails_on_both()
    {
        Validate(Competition(Fight(FightResult.Draw, FightMethod.Submission, 1)))
            .Should().Equal("Fights[0].Method", "Fights[0].TechniqueId");
    }

    [Theory]
    [InlineData(FightResult.Win)]
    [InlineData(FightResult.Loss)]
    public void Win_or_loss_without_method_fails_on_method(FightResult result)
    {
        Validate(Competition(Fight(result))).Should().Equal("Fights[0].Method");
    }

    [Fact]
    public void Win_without_method_but_with_technique_fails_on_both()
    {
        Validate(Competition(Fight(FightResult.Win, techniqueId: 1)))
            .Should().Equal("Fights[0].Method", "Fights[0].TechniqueId");
    }

    [Theory]
    [InlineData(FightMethod.Submission)]
    [InlineData(FightMethod.Throw)]
    [InlineData(FightMethod.Pin)]
    public void Finish_with_technique_passes(FightMethod method)
    {
        Validate(Competition(Fight(FightResult.Win, method, 1), Fight(FightResult.Loss, method, 1))).Should().BeEmpty();
    }

    [Theory]
    [InlineData(FightMethod.Points)]
    [InlineData(FightMethod.Decision)]
    [InlineData(FightMethod.Disqualification)]
    [InlineData(FightMethod.Walkover)]
    public void Method_without_a_finish_and_with_technique_fails_on_technique_id(FightMethod method)
    {
        Validate(Competition(Fight(FightResult.Win, method, 1))).Should().Equal("Fights[0].TechniqueId");
    }

    [Theory]
    [InlineData(FightMethod.Submission)]
    [InlineData(FightMethod.Points)]
    [InlineData(FightMethod.Walkover)]
    public void Win_or_loss_with_method_and_without_technique_passes(FightMethod method)
    {
        Validate(Competition(Fight(FightResult.Win, method), Fight(FightResult.Loss, method))).Should().BeEmpty();
    }

    [Fact]
    public void Error_key_uses_the_index_of_the_bad_fight()
    {
        var fights = new[]
        {
            Fight(FightResult.Win, FightMethod.Points), Fight(FightResult.Draw), Fight(FightResult.Draw, FightMethod.Points)
        };

        Validate(Competition(fights)).Should().Equal("Fights[2].Method");
    }

    [Fact]
    public void Null_fight_fails_on_its_index()
    {
        Validate(Competition(Fight(FightResult.Draw), null!)).Should().Equal("Fights[1]");
    }

    private static List<string> Validate(CompetitionRequest request) =>
        new CompetitionRequestFieldsValidator().Validate(request).Errors.Select(e => e.PropertyName).ToList();

    private static CompetitionRequest Competition(params FightRequest[] fights) =>
        new()
        {
            Name = "Warsaw Open",
            Date = new DateOnly(2026, 9, 12),
            DisciplineId = 1,
            FormatId = 1,
            Fights = [..fights]
        };

    private static FightRequest Fight(FightResult result, FightMethod? method = null, int? techniqueId = null) =>
        new() { Result = result, Method = method, TechniqueId = techniqueId };
}
