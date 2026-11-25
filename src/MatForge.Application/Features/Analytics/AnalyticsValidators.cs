using System.Globalization;
using FluentValidation;
using MatForge.Application.Features.Analytics.Calculators;

namespace MatForge.Application.Features.Analytics;

public class AnalyticsQueryValidator : AbstractValidator<AnalyticsQuery>
{
    public AnalyticsQueryValidator()
    {
        // The start is not before 2000 or after today when the end is omitted
        RuleFor(q => q.From).Custom((from, context) =>
        {
            if (from == null)
            {
                return;
            }

            if (from < DateRange.MinFrom)
            {
                context.AddFailure("Choose a start date from 1 January 2000 on.");
            }
            else if (context.InstanceToValidate.To == null && from > Today())
            {
                context.AddFailure("Choose a start date that is not after today.");
            }
        });

        // The end is between 2000 and the end of next year and not before the start
        RuleFor(q => q.To).Custom((to, context) =>
        {
            if (to == null)
            {
                return;
            }

            var lastDay = new DateOnly(Today().Year + 1, 12, 31);
            var from = context.InstanceToValidate.From;
            if (to < DateRange.MinFrom)
            {
                context.AddFailure("Choose an end date from 1 January 2000 on.");
            }
            else if (to > lastDay)
            {
                context.AddFailure($"Choose an end date no later than {lastDay.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)}.");
            }
            else if (from != null && to < from)
            {
                context.AddFailure("Choose an end date that is not before the start date.");
            }
        });
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.Now);
}
