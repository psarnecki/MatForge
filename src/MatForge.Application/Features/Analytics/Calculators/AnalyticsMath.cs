namespace MatForge.Application.Features.Analytics.Calculators;

public static class AnalyticsMath
{
    // Shares and rates use 4 decimal places, with midpoints rounded away from zero
    public static decimal RoundFraction(decimal fraction) => Math.Round(fraction, 4, MidpointRounding.AwayFromZero);

    public static decimal Share(int count, int total) => RoundFraction((decimal)count / total);

    // Draws remain in the denominator
    public static decimal? WinRate(int wins, int rounds) => rounds == 0 ? null : Share(wins, rounds);
}
