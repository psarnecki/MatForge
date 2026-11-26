using MatForge.Data.Entities;

namespace MatForge.Data.Seed;

public record DemoData(List<Club> Clubs, List<UserBelt> Belts, List<TrainingSession> Sessions, List<Competition> Competitions);

// Five years of a competitive grappler's training, aligned to the week of the end date
public static class DemoDataGenerator
{
    // Weeks of history, the last one being the week of the end date
    public const int Weeks = 260;

    public static DemoData Generate(DateOnly endDate, DemoDictionary dictionary)
    {
        var random = new Random(42);
        var clubs = new[] { DemoWeekPlan.NorthSide, DemoWeekPlan.Riverside, DemoWeekPlan.CityWrestling }
            .Select(name => new Club { Name = name })
            .ToList();
        var weekPlan = new DemoWeekPlan(random, dictionary, clubs.ToDictionary(c => c.Name),
            new DemoRounds(random, dictionary));

        var endWeekMonday = endDate.AddDays(-(((int)endDate.DayOfWeek + 6) % 7));
        var sessions = new List<TrainingSession>();
        for (var weeksBack = Weeks - 1; weeksBack >= 0; weeksBack--)
        {
            sessions.AddRange(weekPlan.Sessions(weeksBack, endWeekMonday.AddDays(-7 * weeksBack)));
        }

        // The whole last week is drawn first, so dropping the days after the end date moves nothing before it
        sessions.RemoveAll(s => s.Date > endDate);

        var belt = new UserBelt
        {
            DisciplineId = dictionary.Disciplines["bjj"], BeltId = dictionary.Belts[("bjj", "brown")], Degree = 0
        };

        return new DemoData(clubs, [belt], sessions, []);
    }
}
