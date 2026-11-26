using System.Globalization;
using FluentValidation;
using MatForge.Application.Common;
using MatForge.Application.Features.Analytics.Calculators;
using MatForge.Data;
using Microsoft.EntityFrameworkCore;

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

// Uses the same period rules and checks the filters against the dictionaries
public class AnalyticsFilterQueryValidator : AbstractValidator<AnalyticsFilterQuery>
{
    public AnalyticsFilterQueryValidator(AppDbContext db)
    {
        Include(new AnalyticsQueryValidator());

        // The discipline exists
        RuleFor(q => q.DisciplineId).CustomAsync(async (disciplineId, context, cancellationToken) =>
        {
            if (disciplineId != null && !await db.Disciplines.AnyAsync(d => d.Id == disciplineId, cancellationToken))
            {
                context.AddFailure("Choose a discipline.");
            }
        });

        // The format exists, and belongs to the discipline when both are given
        RuleFor(q => q.FormatId).CustomAsync(async (formatId, context, cancellationToken) =>
        {
            if (formatId == null)
            {
                return;
            }

            var disciplineId = context.InstanceToValidate.DisciplineId;
            if (disciplineId == null)
            {
                if (!await db.DisciplineFormats.AnyAsync(f => f.Id == formatId, cancellationToken))
                {
                    context.AddFailure("Choose a format.");
                }
            }
            else if (!await DisciplineRules.FormatBelongsToDisciplineAsync(db, formatId.Value, disciplineId.Value, cancellationToken))
            {
                context.AddFailure("Choose a format of this discipline.");
            }
        });
    }
}
