using FluentValidation;
using MatForge.Application.Common;
using MatForge.Data;
using MatForge.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace MatForge.Application.Features.Sessions;

// Rules that need only the request
public class SessionRequestFieldsValidator : AbstractValidator<SessionRequest>
{
    public SessionRequestFieldsValidator()
    {
        // The date is at most one day ahead
        RuleFor(r => r.Date)
            .Must(date => date <= DateOnly.FromDateTime(DateTime.Now).AddDays(1))
            .WithMessage("Choose a date no later than tomorrow.")
            .When(r => r.Date != null);

        // Grappling sessions have a discipline, a format and a character, but no activity type
        When(r => r.Kind == SessionKind.Grappling, () =>
        {
            RuleFor(r => r.DisciplineId).NotNull().WithMessage("Choose a discipline.");
            RuleFor(r => r.FormatId).NotNull().WithMessage("Choose a format.");
            RuleFor(r => r.CharacterId).NotNull().WithMessage("Choose the character of this session.");
            RuleFor(r => r.ActivityTypeId).Null().WithMessage("Grappling sessions have no activity type.");
        });

        // Other sessions have an activity type and nothing specific to grappling
        When(r => r.Kind == SessionKind.Other, () =>
        {
            RuleFor(r => r.ActivityTypeId).NotNull().WithMessage("Choose an activity type.");
            RuleFor(r => r.DisciplineId).Null().WithMessage("Other activities have no discipline.");
            RuleFor(r => r.FormatId).Null().WithMessage("Other activities have no format.");
            RuleFor(r => r.CharacterId).Null().WithMessage("Other activities have no character.");
            RuleFor(r => r.ClubId).Null().WithMessage("Other activities have no club.");
            RuleFor(r => r.Rounds).Empty().WithMessage("Other activities have no rounds.");
        });

        // A draw has no technique and no position, a position requires a technique
        RuleFor(r => r.Rounds).Custom((rounds, context) =>
        {
            for (var i = 0; i < rounds.Count; i++)
            {
                var round = rounds[i];

                // A JSON array can contain null entries
                if (round is null)
                {
                    context.AddFailure($"Rounds[{i}]", "Choose a result for this round.");
                    continue;
                }

                if (round.Result == RoundResult.Draw)
                {
                    if (round.TechniqueId != null)
                    {
                        context.AddFailure($"Rounds[{i}].TechniqueId", "A draw has no technique.");
                    }

                    if (round.PositionId != null)
                    {
                        context.AddFailure($"Rounds[{i}].PositionId", "A draw has no position.");
                    }
                }
                else if (round.PositionId != null && round.TechniqueId == null)
                {
                    context.AddFailure($"Rounds[{i}].PositionId", "Choose a technique before choosing a position.");
                }
            }
        });
    }
}

public class SessionRequestValidator : AbstractValidator<SessionRequest>
{
    public SessionRequestValidator(AppDbContext db)
    {
        Include(new SessionRequestFieldsValidator());

        When(r => r.Kind == SessionKind.Other, () =>
        {
            // The activity type exists
            RuleFor(r => r.ActivityTypeId).CustomAsync(async (activityTypeId, context, cancellationToken) =>
            {
                if (activityTypeId != null && !await db.ActivityTypes.AnyAsync(a => a.Id == activityTypeId, cancellationToken))
                {
                    context.AddFailure("Choose an activity type.");
                }
            });
        });

        When(r => r.Kind == SessionKind.Grappling, () =>
        {
            // The format belongs to the discipline
            RuleFor(r => r.FormatId).CustomAsync(async (formatId, context, cancellationToken) =>
            {
                var disciplineId = context.InstanceToValidate.DisciplineId;
                if (formatId != null && disciplineId != null
                    && !await DisciplineRules.FormatBelongsToDisciplineAsync(db, formatId.Value, disciplineId.Value, cancellationToken))
                {
                    context.AddFailure("Choose a format of this discipline.");
                }
            });

            // The character exists and allows rounds if the session has any
            RuleFor(r => r.CharacterId).CustomAsync(async (characterId, context, cancellationToken) =>
            {
                if (characterId == null)
                {
                    return;
                }

                var character = await db.TrainingCharacters
                    .Where(c => c.Id == characterId)
                    .Select(c => new { c.Name, c.AllowsRounds })
                    .SingleOrDefaultAsync(cancellationToken);

                if (character == null)
                {
                    context.AddFailure("Choose the character of this session.");
                }
                else if (!character.AllowsRounds && context.InstanceToValidate.Rounds.Count > 0)
                {
                    context.AddFailure("Rounds", $"{character.Name} sessions have no rounds.");
                }
            });

            // The club belongs to the current user (the query filter hides other users' clubs)
            RuleFor(r => r.ClubId).CustomAsync(async (clubId, context, cancellationToken) =>
            {
                if (clubId != null && !await db.Clubs.AnyAsync(c => c.Id == clubId, cancellationToken))
                {
                    context.AddFailure("Choose one of your clubs.");
                }
            });

            // Techniques and positions are offered in the discipline (one query each for all rounds)
            RuleFor(r => r.Rounds).CustomAsync(async (rounds, context, cancellationToken) =>
            {
                var disciplineId = context.InstanceToValidate.DisciplineId;
                if (disciplineId == null)
                {
                    return;
                }

                var entries = rounds.OfType<RoundRequest>().ToList();
                var techniqueIds = entries.Where(r => r.TechniqueId != null).Select(r => r.TechniqueId!.Value).Distinct().ToList();
                var positionIds = entries.Where(r => r.PositionId != null).Select(r => r.PositionId!.Value).Distinct().ToList();

                HashSet<int> offeredTechniques = techniqueIds.Count > 0
                    ? await DisciplineRules.GetOfferedTechniqueIdsAsync(db, techniqueIds, disciplineId.Value, cancellationToken)
                    : [];

                HashSet<int> offeredPositions = positionIds.Count > 0
                    ? await db.PositionDisciplines
                        .Where(p => p.DisciplineId == disciplineId && positionIds.Contains(p.PositionId))
                        .Select(p => p.PositionId)
                        .ToHashSetAsync(cancellationToken)
                    : [];

                for (var i = 0; i < rounds.Count; i++)
                {
                    var round = rounds[i];
                    if (round?.TechniqueId is { } techniqueId && !offeredTechniques.Contains(techniqueId))
                    {
                        context.AddFailure($"Rounds[{i}].TechniqueId", "Choose a technique of this discipline.");
                    }

                    if (round?.PositionId is { } positionId && !offeredPositions.Contains(positionId))
                    {
                        context.AddFailure($"Rounds[{i}].PositionId", "Choose a position of this discipline.");
                    }
                }
            });
        });
    }
}

public class SessionListQueryValidator : AbstractValidator<SessionListQuery>
{
    private const int MaxRangeDays = 400;

    public SessionListQueryValidator()
    {
        // The range is in order and at most 400 days long
        RuleFor(q => q.To).Custom((to, context) =>
        {
            var from = context.InstanceToValidate.From;
            if (from == null || to == null)
            {
                return;
            }

            if (to < from)
            {
                context.AddFailure("Choose an end date that is not before the start date.");
            }
            else if (to.Value.DayNumber - from.Value.DayNumber > MaxRangeDays)
            {
                context.AddFailure($"Choose a range of at most {MaxRangeDays} days.");
            }
        });
    }
}
