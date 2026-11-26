using MatForge.Data.Entities;
using MatForge.Data.Enums;

namespace MatForge.Data.Seed;

// Weekly timetables of the demo athlete, one per phase of the story
internal class DemoWeekPlan(Random random, DemoDictionary dictionary, IReadOnlyDictionary<string, Club> clubs,
    DemoRounds rounds)
{
    public const string NorthSide = "North Side BJJ";
    public const string Riverside = "Riverside Grappling";
    public const string CityWrestling = "City Wrestling Club";

    // Chance that the athlete turns up for a planned grappling session
    private const double Attendance = 0.88;

    private static readonly (int Feeling, int Weight)[] Feelings = [(2, 5), (3, 25), (4, 45), (5, 25)];
    private static readonly (int Feeling, int Weight)[] LowFeelings = [(2, 10), (3, 50), (4, 30), (5, 10)];

    // Notes that tell the story of the injury, by week and weekday
    private static readonly Dictionary<(int WeeksBack, DayOfWeek Day), string> StoryNotes = new()
    {
        [(24, DayOfWeek.Monday)] = "Knee rehab after the Warsaw No-Gi Open, no grappling for three weeks.",
        [(21, DayOfWeek.Wednesday)] = "First sparring after the knee injury, still careful with leg locks."
    };

    // Mostly gi, no wrestling yet
    private static readonly Slot[] BlueWeek =
    [
        One(DayOfWeek.Monday, 0.7, Other("strength", 60)),
        One(DayOfWeek.Monday, Attendance, Bjj("gi", "technique", NorthSide, 90)),
        One(DayOfWeek.Tuesday, Attendance, Bjj("gi", "drilling", NorthSide, 75)),
        One(DayOfWeek.Wednesday, Attendance, Bjj("gi", "sparring", NorthSide, 90)),
        One(DayOfWeek.Thursday, 0.3, Bjj("gi", "technique", NorthSide, 90)),
        new(DayOfWeek.Friday, Attendance, [(Bjj("gi", "mixed", NorthSide, 90), 2), (Bjj("nogi", "mixed", NorthSide, 90), 1)]),
        new(DayOfWeek.Saturday, 0.6, [(Bjj("gi", "open_mat", Riverside, 120), 7), (Bjj("nogi", "open_mat", Riverside, 120), 3)]),
        One(DayOfWeek.Sunday, 0.5, Other("running", 40))
    ];

    // Wednesday is the hard sparring day, Friday and Saturday also have rounds
    private static readonly Slot[] FullWeek =
    [
        One(DayOfWeek.Monday, 0.9, Other("strength", 60)),
        One(DayOfWeek.Monday, Attendance, Bjj("nogi", "technique", NorthSide, 90)),
        One(DayOfWeek.Tuesday, 0.5, Other("running", 40)),
        new(DayOfWeek.Tuesday, Attendance,
            [(Bjj("gi", "drilling", NorthSide, 75), 1), (Bjj("gi", "situational_drills", NorthSide, 75), 1)]),
        One(DayOfWeek.Wednesday, Attendance, Bjj("nogi", "sparring", NorthSide, 90)),
        One(DayOfWeek.Thursday, 0.9, Other("strength", 60)),
        new(DayOfWeek.Thursday, Attendance, [(Wrestling("technique", 90), 1), (Wrestling("mixed", 90), 1)]),
        One(DayOfWeek.Friday, Attendance, Bjj("gi", "mixed", NorthSide, 90)),
        new(DayOfWeek.Saturday, Attendance,
        [
            (Bjj("nogi", "open_mat", Riverside, 120), 50),
            (Bjj("gi", "open_mat", Riverside, 120), 34),
            (Bjj("nogi", "open_mat", null, 120), 10),
            (Bjj("gi", "seminar", null, 150), 6)
        ]),
        One(DayOfWeek.Sunday, 0.4, Bjj("nogi", "yoga_mobility", null, 45)),
        new(DayOfWeek.Sunday, 0.4, [(Other("conditioning", 40), 1), (Other("cycling", 75), 1)])
    ];

    // No grappling while the knee heals
    private static readonly Slot[] InjuryWeek =
    [
        One(DayOfWeek.Monday, 1, Other("strength", 45)),
        One(DayOfWeek.Wednesday, 1, Other("swimming", 40)),
        One(DayOfWeek.Thursday, 1, Other("strength", 45)),
        One(DayOfWeek.Saturday, 0.7, Other("cycling", 60))
    ];

    // Back on the mat, without wrestling and open mats
    private static readonly Slot[] ComebackWeek =
    [
        One(DayOfWeek.Monday, 1, Other("strength", 60)),
        One(DayOfWeek.Monday, 0.95, Bjj("nogi", "technique", NorthSide, 90)),
        One(DayOfWeek.Tuesday, 0.95, Bjj("gi", "drilling", NorthSide, 75)),
        One(DayOfWeek.Wednesday, 0.95, Bjj("nogi", "sparring", NorthSide, 90)),
        One(DayOfWeek.Thursday, 1, Other("strength", 60)),
        One(DayOfWeek.Friday, 0.95, Bjj("gi", "mixed", NorthSide, 90))
    ];

    private static readonly Slot[] HolidayWeek =
    [
        One(DayOfWeek.Tuesday, 0.6, Other("running", 40)),
        One(DayOfWeek.Thursday, 0.5, Other("swimming", 40)),
        One(DayOfWeek.Saturday, 0.4, Other("cycling", 75))
    ];

    // A competition takes the place of the Saturday session
    public List<TrainingSession> Sessions(int weeksBack, DateOnly monday, bool competing)
    {
        var phase = DemoTimeline.PhaseOf(weeksBack);
        var feelings = phase is DemoPhase.Injury or DemoPhase.Comeback ? LowFeelings : Feelings;
        var sessions = new List<TrainingSession>();

        foreach (var slot in Timetable(phase))
        {
            if ((competing && slot.Day == DayOfWeek.Saturday) || !random.Chance(slot.Chance))
            {
                continue;
            }

            var planned = random.Pick(slot.Options);
            var date = monday.AddDays(((int)slot.Day + 6) % 7);
            var minutes = planned.Minutes + 15 * random.Next(-1, 2);
            var session = Create(planned, date, minutes, random.Pick(feelings), weeksBack);
            session.Notes = StoryNotes.GetValueOrDefault((weeksBack, slot.Day));
            sessions.Add(session);
        }

        return sessions;
    }

    private static Slot[] Timetable(DemoPhase phase) => phase switch
    {
        DemoPhase.Blue => BlueWeek,
        DemoPhase.Holiday => HolidayWeek,
        DemoPhase.Injury => InjuryWeek,
        DemoPhase.Comeback => ComebackWeek,
        _ => FullWeek
    };

    private TrainingSession Create(Planned planned, DateOnly date, int minutes, int feeling, int weeksBack) =>
        planned.Activity == null
            ? new TrainingSession
            {
                Date = date, Kind = SessionKind.Grappling, DurationMinutes = minutes, Feeling = feeling,
                DisciplineId = dictionary.Disciplines[planned.Discipline!],
                FormatId = dictionary.Formats[(planned.Discipline!, planned.Format!)],
                CharacterId = dictionary.Characters[planned.Character!],
                Club = planned.Club == null ? null : clubs[planned.Club],
                Rounds = rounds.For(planned.Discipline!, planned.Format!, planned.Character!, weeksBack)
            }
            : new TrainingSession
            {
                Date = date, Kind = SessionKind.Other, DurationMinutes = minutes, Feeling = feeling,
                ActivityTypeId = dictionary.ActivityTypes[planned.Activity]
            };

    private static Slot One(DayOfWeek day, double chance, Planned planned) => new(day, chance, [(planned, 1)]);

    private static Planned Bjj(string format, string character, string? club, int minutes) =>
        new("bjj", format, character, club, null, minutes);

    private static Planned Wrestling(string character, int minutes) =>
        new("wrestling", "freestyle", character, CityWrestling, null, minutes);

    private static Planned Other(string activity, int minutes) => new(null, null, null, null, activity, minutes);

    // Grappling sessions have a discipline, format and character, while other sessions have only an activity
    private record Planned(string? Discipline, string? Format, string? Character, string? Club, string? Activity, int Minutes);

    // A weekday on which the athlete may train, with one of the options picked by weight
    private record Slot(DayOfWeek Day, double Chance, (Planned Session, int Weight)[] Options);
}
