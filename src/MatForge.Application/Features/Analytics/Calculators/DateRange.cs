namespace MatForge.Application.Features.Analytics.Calculators;

// Inclusive period [From, To] used by analytics
public readonly record struct DateRange(DateOnly From, DateOnly To)
{
    public static readonly DateOnly MinFrom = new(2000, 1, 1);

    public int Days => To.DayNumber - From.DayNumber + 1;

    // Same length, directly before
    public DateRange Previous => new(From.AddDays(-Days), From.AddDays(-1));

    public bool Contains(DateOnly date) => date >= From && date <= To;

    // A missing to is today, a missing from is the earliest data but never later than to and never before 2000
    public static DateRange Resolve(DateOnly? from, DateOnly? to, DateOnly? earliestData, DateOnly today)
    {
        var end = to ?? today;
        if (from != null)
        {
            return new DateRange(from.Value, end);
        }

        var start = earliestData != null && earliestData < end ? earliestData.Value : end;
        return new DateRange(start < MinFrom ? MinFrom : start, end);
    }
}
