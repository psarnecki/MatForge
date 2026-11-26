using MatForge.Data.Entities;
using MatForge.Data.Enums;

namespace MatForge.Data.Seed;

// Sparring rounds of the demo athlete, featuring a favorite finish, a recurring weakness and a win rate that grows over time
internal class DemoRounds(Random random, DemoDictionary dictionary)
{
    private const double DrawChance = 0.10;
    private const double WinTechniqueChance = 0.75;
    private const double LossTechniqueChance = 0.80;

    // Chokes that need a collar
    private static readonly HashSet<string> GiOnly = ["cross_collar_choke", "bow_and_arrow", "loop_choke"];

    private static readonly (Finish Finish, int Weight)[] BlueWins =
    [
        (new("armbar", [("mount", 6), ("closed_guard", 4)]), 22),
        (new("americana", [("side_control", 6), ("mount", 4)]), 15),
        (new("kimura", [("half_guard", 1), ("side_control", 1)]), 15),
        (new("cross_collar_choke", [("mount", 1), ("closed_guard", 1)]), 12),
        (new("rear_naked_choke", [("back_control", 1)], 0.95), 12),
        (new("triangle", [("closed_guard", 1)]), 10),
        (new("ezekiel", [("mount", 1)]), 6),
        (new("other_finish", [("other_position", 1)]), 8)
    ];

    // The rear naked choke from the back is the favorite finish
    private static readonly (Finish Finish, int Weight)[] LaterWins =
    [
        (new("rear_naked_choke", [("back_control", 1)], 0.95), 45),
        (new("triangle", [("closed_guard", 7), ("open_guard", 3)]), 9),
        (new("armbar", [("mount", 1), ("closed_guard", 1)]), 9),
        (new("kimura", [("half_guard", 1), ("side_control", 1)]), 7),
        (new("guillotine", [("front_headlock", 6), ("closed_guard", 4)]), 7),
        (new("darce", [("front_headlock", 1), ("turtle", 1)]), 6),
        (new("bow_and_arrow", [("back_control", 1)], 0.95), 5),
        (new("straight_ankle_lock", [("outside_ashi", 1)]), 5),
        (new("other_finish", [("other_position", 1)]), 4)
    ];

    private static readonly (Finish Finish, int Weight)[] BlueLosses =
    [
        (new("rear_naked_choke", [("back_control", 1)], 0.95), 18),
        (new("armbar", [("mount", 1), ("closed_guard", 1)]), 18),
        (new("triangle", [("closed_guard", 1)]), 15),
        (new("cross_collar_choke", [("mount", 1)]), 12),
        (new("kimura", [("half_guard", 1)]), 12),
        (new("guillotine", [("front_headlock", 1)]), 10),
        (new("americana", [("side_control", 1)]), 8),
        (new("other_finish", [("other_position", 1)]), 7)
    ];

    // Heel hooks from the saddle are a recurring weakness, especially without the gi
    private static readonly (Finish Finish, int Weight)[] LaterNoGiLosses =
    [
        (new("inside_heel_hook", [("saddle", 19), ("fifty_fifty", 1)], 0.95), 60),
        (new("guillotine", [("front_headlock", 1)]), 9),
        (new("rear_naked_choke", [("back_control", 1)]), 8),
        (new("kneebar", [("saddle", 1)], 0.95), 6),
        (new("arm_triangle", [("side_control", 1)]), 6),
        (new("armbar", [("mount", 1)]), 6),
        (new("darce", [("front_headlock", 1)]), 5)
    ];

    private static readonly (Finish Finish, int Weight)[] LaterGiLosses =
    [
        (new("inside_heel_hook", [("saddle", 1)], 0.95), 20),
        (new("cross_collar_choke", [("mount", 1)]), 12),
        (new("bow_and_arrow", [("back_control", 1)]), 12),
        (new("armbar", [("closed_guard", 1)]), 12),
        (new("triangle", [("closed_guard", 1)]), 12),
        (new("straight_ankle_lock", [("outside_ashi", 1)]), 10),
        (new("loop_choke", [("turtle", 1)]), 8),
        (new("kneebar", [("saddle", 1)], 0.95), 6),
        (new("other_finish", [("other_position", 1)]), 8)
    ];

    private static readonly (Finish Finish, int Weight)[] WrestlingWins =
    [
        (new("double_leg", [("standing", 1)]), 40),
        (new("single_leg", [("standing", 7), ("scramble", 3)]), 35),
        (new("high_crotch", [("standing", 1)]), 15),
        (new("body_lock_takedown", [("clinch", 1)]), 10)
    ];

    private static readonly (Finish Finish, int Weight)[] WrestlingLosses =
    [
        (new("double_leg", [("standing", 1)]), 50),
        (new("single_leg", [("standing", 7), ("scramble", 3)]), 50)
    ];

    // Characters without rounds get none
    public List<SparringRound> For(string discipline, string format, string character, int weeksBack)
    {
        var phase = DemoTimeline.PhaseOf(weeksBack);
        var count = (character, phase) switch
        {
            ("sparring", DemoPhase.Blue) => random.Next(5, 8),
            ("sparring", DemoPhase.Comeback) => random.Next(5, 7),
            ("sparring", _) => random.Next(8, 11),
            ("mixed", _) when discipline == "wrestling" => 3,
            ("mixed", _) => random.Next(3, 6),
            ("open_mat", _) => random.Next(5, 9),
            _ => 0
        };

        var winChance = WinChance(phase, weeksBack);
        var rounds = new List<SparringRound>();
        for (var order = 1; order <= count; order++)
        {
            var roll = random.NextDouble();
            var result = roll < winChance ? RoundResult.Win : roll < winChance + DrawChance ? RoundResult.Draw : RoundResult.Loss;
            rounds.Add(Round(order, result, discipline, format, phase));
        }

        return rounds;
    }

    // Grows slowly over the blue and purple years, drops after the injury and peaks at brown belt
    private static double WinChance(DemoPhase phase, int weeksBack) => phase switch
    {
        DemoPhase.Blue => 0.32 + 0.13 * (259 - weeksBack) / 99.0,
        DemoPhase.Comeback => 0.34,
        DemoPhase.Brown => 0.64,
        _ => 0.45 + 0.13 * (159 - weeksBack) / 134.0
    };

    private SparringRound Round(int order, RoundResult result, string discipline, string format, DemoPhase phase)
    {
        var round = new SparringRound { Order = order, Result = result };
        if (result == RoundResult.Draw
            || !random.Chance(result == RoundResult.Win ? WinTechniqueChance : LossTechniqueChance))
        {
            return round;
        }

        var finishes = FinishesFor(result, discipline, format, phase)
            .Where(f => format == "gi" || !GiOnly.Contains(f.Finish.Technique))
            .ToList();
        var finish = random.Pick(finishes);
        round.TechniqueId = dictionary.Techniques[finish.Technique];

        if (random.Chance(finish.PositionChance))
        {
            round.PositionId = dictionary.Positions[random.Pick(finish.Positions)];
        }

        return round;
    }

    private static (Finish Finish, int Weight)[] FinishesFor(RoundResult result, string discipline, string format,
        DemoPhase phase) =>
        (result, discipline, phase, format) switch
        {
            (RoundResult.Win, "wrestling", _, _) => WrestlingWins,
            (RoundResult.Loss, "wrestling", _, _) => WrestlingLosses,
            (RoundResult.Win, _, DemoPhase.Blue, _) => BlueWins,
            (RoundResult.Win, _, _, _) => LaterWins,
            (_, _, DemoPhase.Blue, _) => BlueLosses,
            (_, _, _, "gi") => LaterGiLosses,
            _ => LaterNoGiLosses
        };

    // A finishing technique with the positions it comes from and the chance that the athlete records the position
    private record Finish(string Technique, (string Position, int Weight)[] Positions, double PositionChance = 0.6);
}
