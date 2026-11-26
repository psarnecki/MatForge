using MatForge.Data.Entities;
using MatForge.Data.Enums;

namespace MatForge.Data.Seed;

// Competitions of the demo athlete, with random brackets in the earlier years and scripted events in the story
internal class DemoCompetitions(Random random, DemoDictionary dictionary, DemoRounds rounds)
{
    // The events of the story, where the later ones shape the insights for the last 90 days and the 90 days before them
    private static readonly Scripted[] ScriptedEvents =
    [
        // The first medal, in the first year at blue belt
        new(233, "Łódź Open", "Łódź", "bjj", "gi", "Blue", 3, null, null,
            [Win(FightMethod.Submission, "armbar"), Win(FightMethod.Points), Loss(FightMethod.Points)]),
        new(120, "Warsaw Wrestling Open", "Warsaw", "wrestling", "freestyle", null, 3, null, null,
            [Win(FightMethod.Points), Win(FightMethod.Pin, "pin_fall"), Loss(FightMethod.Points)]),
        new(60, "Sub-Only Superfight Night", "Wrocław", "bjj", "nogi", "Purple", null, "Superfight", null,
            [new(FightResult.Draw, null, null)]),
        new(25, "Warsaw No-Gi Open", "Warsaw", "bjj", "nogi", "Purple", 2, null, "Hurt my knee in the final.",
        [
            Win(FightMethod.Submission, "rear_naked_choke"), Win(FightMethod.Points),
            Win(FightMethod.Submission, "triangle"), Loss(FightMethod.Submission, "inside_heel_hook")
        ]),
        new(15, "Łódź No-Gi Cup", "Łódź", "bjj", "nogi", "Purple", 1, null, "First competition after the knee injury.",
            [Win(FightMethod.Points), Win(FightMethod.Submission, "rear_naked_choke")]),
        new(11, "Kraków Grappling League", "Kraków", "bjj", "nogi", "Brown", null, "Group stage", null,
        [
            Win(FightMethod.Submission, "rear_naked_choke"), Loss(FightMethod.Submission, "inside_heel_hook"),
            Loss(FightMethod.Points)
        ]),
        new(7, "Gdańsk No-Gi Open", "Gdańsk", "bjj", "nogi", "Brown", null, "Quarterfinal", null,
            [Win(FightMethod.Submission, "armbar"), Loss(FightMethod.Decision)]),
        new(4, "Kraków Grappling League", "Kraków", "bjj", "nogi", "Brown", null, "Group stage", null,
        [
            Win(FightMethod.Submission, "rear_naked_choke"), Loss(FightMethod.Submission, "inside_heel_hook"),
            Loss(FightMethod.Submission, "kneebar")
        ]),
        new(-UpcomingWeeks, "Poznań No-Gi Open", "Poznań", "bjj", "nogi", "Brown", null, null, null, [])
    ];

    private static readonly string[] Cities = ["Warsaw", "Kraków", "Poznań", "Gdańsk", "Wrocław", "Łódź", "Katowice", "Lublin"];

    private static readonly string[] Opponents =
    [
        "M. Nowicki", "K. Zieliński", "P. Wójcik", "T. Kamiński", "J. Lewandowski", "A. Dąbrowski", "R. Kozłowski",
        "B. Jankowski", "D. Mazur", "S. Krawczyk", "Ł. Piotrowski", "G. Grabowski", "W. Pawlak", "F. Michalski",
        "O. Król", "H. Wieczorek"
    ];

    private static readonly (FightMethod Method, int Weight)[] WinMethods =
    [
        (FightMethod.Submission, 50), (FightMethod.Points, 35), (FightMethod.Decision, 10),
        (FightMethod.Walkover, 3), (FightMethod.Disqualification, 2)
    ];

    private static readonly (FightMethod Method, int Weight)[] LossMethods =
        [(FightMethod.Submission, 40), (FightMethod.Points, 45), (FightMethod.Decision, 15)];

    public const int UpcomingWeeks = 4;

    // Random competitions stay out of the weeks around the injury
    private const int LastRandomWeek = 30;

    private int _weeksUntilNext = 4;

    public Competition? ForWeek(int weeksBack, DateOnly saturday)
    {
        var scripted = ScriptedEvents.SingleOrDefault(s => s.WeeksBack == weeksBack);
        if (scripted != null)
        {
            _weeksUntilNext = 10;
            return Create(scripted, saturday);
        }

        var phase = DemoTimeline.PhaseOf(weeksBack);
        if (weeksBack < LastRandomWeek || phase == DemoPhase.Holiday || --_weeksUntilNext > 0)
        {
            return null;
        }

        _weeksUntilNext = random.Next(10, 15);
        return RandomBracket(weeksBack, phase, saturday);
    }

    private Competition Create(Scripted scripted, DateOnly saturday)
    {
        var competition = NewCompetition(scripted.Name, scripted.City, scripted.Discipline, scripted.Format,
            scripted.SkillLevel, saturday);
        competition.Place = scripted.Place;
        competition.ResultNote = scripted.ResultNote;
        competition.Notes = scripted.Notes;
        competition.Fights = scripted.Fights
            .Select((f, i) => NewFight(i + 1, f.Result, f.Method, f.Technique, scripted.SkillLevel))
            .ToList();
        return competition;
    }

    // Single elimination, where the athlete fights until the first loss or the end of the bracket
    private Competition RandomBracket(int weeksBack, DemoPhase phase, DateOnly saturday)
    {
        var format = random.Chance(phase == DemoPhase.Blue ? 0.3 : 0.6) ? "nogi" : "gi";
        var city = Cities[random.Next(Cities.Length)];
        var skillLevel = phase == DemoPhase.Blue ? "Blue" : "Purple";
        var competition = NewCompetition(format == "gi" ? $"{city} Open" : $"{city} No-Gi Open", city, "bjj", format,
            skillLevel, saturday);

        var bracketSize = random.Next(3, 5);
        var winChance = Math.Min(0.85, DemoRounds.WinChance(phase, weeksBack) + 0.1);
        for (var order = 1; order <= bracketSize; order++)
        {
            var won = random.Chance(winChance);
            var method = random.Pick(won ? WinMethods : LossMethods);
            var technique = method == FightMethod.Submission ? rounds.PickTechnique(won, format, phase) : null;
            competition.Fights.Add(NewFight(order, won ? FightResult.Win : FightResult.Loss, method, technique, skillLevel));

            if (!won)
            {
                var stagesLeft = bracketSize - order;
                competition.Place = stagesLeft switch { 0 => 2, 1 => 3, _ => null };
                competition.ResultNote = stagesLeft switch { 2 => "Quarterfinal", > 2 => "Round of 16", _ => null };
                return competition;
            }
        }

        competition.Place = 1;
        return competition;
    }

    private Competition NewCompetition(string name, string city, string discipline, string format, string? skillLevel,
        DateOnly date) => new()
    {
        Name = name,
        Date = date,
        Location = city,
        DisciplineId = dictionary.Disciplines[discipline],
        FormatId = dictionary.Formats[(discipline, format)],
        AgeCategory = discipline == "wrestling" ? "Senior" : "Adult",
        SkillLevel = skillLevel,
        WeightCategory = (discipline, format) switch
        {
            ("wrestling", _) => "-79 kg",
            (_, "gi") => "-82.3 kg",
            _ => "-80 kg"
        }
    };

    // Points and decisions go the full match time, which increases with the belt
    private Fight NewFight(int order, FightResult result, FightMethod? method, string? technique, string? skillLevel)
    {
        var matchSeconds = skillLevel switch { "Blue" => 360, "Purple" => 420, "Brown" => 480, _ => 360 };
        return new Fight
        {
            Order = order,
            OpponentName = random.Chance(0.8) ? Opponents[random.Next(Opponents.Length)] : null,
            Result = result,
            Method = method,
            TechniqueId = technique == null ? null : dictionary.Techniques[technique],
            DurationSeconds = method switch
            {
                FightMethod.Walkover => null,
                FightMethod.Submission or FightMethod.Pin or FightMethod.Disqualification => random.Next(30, matchSeconds),
                _ => matchSeconds
            }
        };
    }

    private static ScriptedFight Win(FightMethod method, string? technique = null) => new(FightResult.Win, method, technique);

    private static ScriptedFight Loss(FightMethod method, string? technique = null) => new(FightResult.Loss, method, technique);

    private record Scripted(int WeeksBack, string Name, string City, string Discipline, string Format, string? SkillLevel,
        int? Place, string? ResultNote, string? Notes, ScriptedFight[] Fights);

    private record ScriptedFight(FightResult Result, FightMethod? Method, string? Technique);
}
