using FluentAssertions;
using MatForge.Application.Features.Sessions;
using MatForge.Data.Enums;

namespace MatForge.UnitTests.Sessions;

public class SessionRequestFieldsValidatorTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

    [Fact]
    public void Valid_grappling_session_passes()
    {
        Validate(Grappling()).Should().BeEmpty();
    }

    [Fact]
    public void Valid_other_session_passes()
    {
        Validate(Other()).Should().BeEmpty();
    }

    [Fact]
    public void Grappling_without_discipline_fails_on_discipline_id()
    {
        Validate(Grappling(disciplineId: null)).Should().Equal("DisciplineId");
    }

    [Fact]
    public void Grappling_without_format_fails_on_format_id()
    {
        Validate(Grappling(formatId: null)).Should().Equal("FormatId");
    }

    [Fact]
    public void Grappling_without_character_fails_on_character_id()
    {
        Validate(Grappling(characterId: null)).Should().Equal("CharacterId");
    }

    [Fact]
    public void Grappling_with_activity_type_fails_on_activity_type_id()
    {
        Validate(Grappling(activityTypeId: 1)).Should().Equal("ActivityTypeId");
    }

    [Fact]
    public void Other_without_activity_type_fails_on_activity_type_id()
    {
        Validate(Other(activityTypeId: null)).Should().Equal("ActivityTypeId");
    }

    [Fact]
    public void Other_with_discipline_fails_on_discipline_id()
    {
        Validate(Other(disciplineId: 1)).Should().Equal("DisciplineId");
    }

    [Fact]
    public void Other_with_format_fails_on_format_id()
    {
        Validate(Other(formatId: 1)).Should().Equal("FormatId");
    }

    [Fact]
    public void Other_with_character_fails_on_character_id()
    {
        Validate(Other(characterId: 1)).Should().Equal("CharacterId");
    }

    [Fact]
    public void Other_with_club_fails_on_club_id()
    {
        Validate(Other(clubId: 1)).Should().Equal("ClubId");
    }

    [Fact]
    public void Other_with_rounds_fails_on_rounds()
    {
        Validate(Other(rounds: [Round(RoundResult.Win)])).Should().Equal("Rounds");
    }

    [Theory]
    [InlineData(-400)]
    [InlineData(0)]
    [InlineData(1)]
    public void Date_up_to_one_day_ahead_passes(int daysFromToday)
    {
        Validate(Grappling(date: Today.AddDays(daysFromToday))).Should().BeEmpty();
    }

    [Fact]
    public void Date_more_than_one_day_ahead_fails_on_date()
    {
        Validate(Grappling(date: Today.AddDays(2))).Should().Equal("Date");
    }

    [Fact]
    public void Draw_with_technique_fails_on_technique_id()
    {
        Validate(Grappling(rounds: [Round(RoundResult.Draw, techniqueId: 1)])).Should().Equal("Rounds[0].TechniqueId");
    }

    [Fact]
    public void Draw_with_position_fails_on_position_id()
    {
        Validate(Grappling(rounds: [Round(RoundResult.Draw, positionId: 1)])).Should().Equal("Rounds[0].PositionId");
    }

    [Fact]
    public void Draw_with_technique_and_position_fails_on_both()
    {
        Validate(Grappling(rounds: [Round(RoundResult.Draw, techniqueId: 1, positionId: 1)]))
            .Should().Equal("Rounds[0].TechniqueId", "Rounds[0].PositionId");
    }

    [Theory]
    [InlineData(RoundResult.Win)]
    [InlineData(RoundResult.Loss)]
    public void Position_without_technique_fails_on_position_id(RoundResult result)
    {
        Validate(Grappling(rounds: [Round(result, positionId: 1)])).Should().Equal("Rounds[0].PositionId");
    }

    [Fact]
    public void Error_key_uses_the_index_of_the_bad_round()
    {
        var rounds = new List<RoundRequest>
        {
            Round(RoundResult.Win), Round(RoundResult.Draw), Round(RoundResult.Draw, techniqueId: 1)
        };

        Validate(Grappling(rounds: rounds)).Should().Equal("Rounds[2].TechniqueId");
    }

    [Fact]
    public void Null_round_fails_on_its_index()
    {
        Validate(Grappling(rounds: [Round(RoundResult.Win), null!])).Should().Equal("Rounds[1]");
    }

    [Theory]
    [InlineData(RoundResult.Win)]
    [InlineData(RoundResult.Loss)]
    public void Win_or_loss_without_technique_passes(RoundResult result)
    {
        Validate(Grappling(rounds: [Round(result)])).Should().BeEmpty();
    }

    [Theory]
    [InlineData(RoundResult.Win)]
    [InlineData(RoundResult.Loss)]
    public void Win_or_loss_with_technique_and_optional_position_passes(RoundResult result)
    {
        Validate(Grappling(rounds: [Round(result, techniqueId: 1), Round(result, techniqueId: 1, positionId: 1)]))
            .Should().BeEmpty();
    }

    [Fact]
    public void Draw_without_details_passes()
    {
        Validate(Grappling(rounds: [Round(RoundResult.Draw)])).Should().BeEmpty();
    }

    private static List<string> Validate(SessionRequest request) =>
        new SessionRequestFieldsValidator().Validate(request).Errors.Select(e => e.PropertyName).ToList();

    private static SessionRequest Grappling(int? disciplineId = 1, int? formatId = 1, int? characterId = 1,
        int? activityTypeId = null, DateOnly? date = null, List<RoundRequest>? rounds = null) =>
        new()
        {
            Kind = SessionKind.Grappling,
            Date = date ?? Today,
            DurationMinutes = 90,
            Feeling = 4,
            DisciplineId = disciplineId,
            FormatId = formatId,
            CharacterId = characterId,
            ActivityTypeId = activityTypeId,
            Rounds = rounds ?? []
        };

    private static SessionRequest Other(int? activityTypeId = 1, int? disciplineId = null, int? formatId = null,
        int? characterId = null, int? clubId = null, List<RoundRequest>? rounds = null) =>
        new()
        {
            Kind = SessionKind.Other,
            Date = Today,
            DurationMinutes = 45,
            Feeling = 3,
            ActivityTypeId = activityTypeId,
            DisciplineId = disciplineId,
            FormatId = formatId,
            CharacterId = characterId,
            ClubId = clubId,
            Rounds = rounds ?? []
        };

    private static RoundRequest Round(RoundResult result, int? techniqueId = null, int? positionId = null) =>
        new() { Result = result, TechniqueId = techniqueId, PositionId = positionId };
}
