namespace MatForge.Application.Features.Analytics.Calculators;

public enum Bucket
{
    Week,
    Month
}

// ISO weeks start on Monday and months on the 1st, so a bucket may start before the period
public static class DateBuckets
{
    private const int MaxWeekBucketDays = 92;

    public static Bucket For(DateRange period) => period.Days <= MaxWeekBucketDays ? Bucket.Week : Bucket.Month;

    public static DateOnly Start(DateOnly date, Bucket bucket) =>
        bucket == Bucket.Week
            ? date.AddDays(-(((int)date.DayOfWeek + 6) % 7))
            : new DateOnly(date.Year, date.Month, 1);

    // Include all overlapping buckets, even empty ones
    public static List<DateOnly> Starts(DateRange period, Bucket bucket)
    {
        var starts = new List<DateOnly>();
        for (var start = Start(period.From, bucket); start <= period.To; start = Next(start, bucket))
        {
            starts.Add(start);
        }

        return starts;
    }

    private static DateOnly Next(DateOnly start, Bucket bucket) =>
        bucket == Bucket.Week ? start.AddDays(7) : start.AddMonths(1);
}
