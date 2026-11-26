namespace MatForge.Data.Seed;

internal enum DemoPhase
{
    Blue,
    Purple,
    Holiday,
    Injury,
    Comeback,
    Brown
}

// The story of the demo athlete, counted in weeks before the week of the end date
internal static class DemoTimeline
{
    public static DemoPhase PhaseOf(int weeksBack) => weeksBack switch
    {
        <= 13 => DemoPhase.Brown,
        <= 21 => DemoPhase.Comeback,
        <= 24 => DemoPhase.Injury,
        _ when IsHoliday(weeksBack) => DemoPhase.Holiday,
        < 160 => DemoPhase.Purple,
        _ => DemoPhase.Blue
    };

    // Two weeks off each year and one more week six months later, all before the injury
    private static bool IsHoliday(int weeksBack) => weeksBack >= 30 && (weeksBack - 30) % 52 is 0 or 1 or 26;
}
