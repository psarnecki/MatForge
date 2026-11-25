using FluentValidation;
using MatForge.Application.Common;
using MatForge.Data;
using MatForge.Data.Enums;

namespace MatForge.Application.Features.Competitions;

// Rules that need only the request
public class CompetitionRequestFieldsValidator : AbstractValidator<CompetitionRequest>
{
    public CompetitionRequestFieldsValidator()
    {
        // A draw has no method and no technique, a win or loss has a method, a technique needs a submission, throw or pin
        RuleFor(r => r.Fights).Custom((fights, context) =>
        {
            for (var i = 0; i < fights.Count; i++)
            {
                var fight = fights[i];

                // A JSON array can contain null entries
                if (fight is null)
                {
                    context.AddFailure($"Fights[{i}]", "Choose a result for this fight.");
                    continue;
                }

                if (fight.Result == FightResult.Draw)
                {
                    if (fight.Method != null)
                    {
                        context.AddFailure($"Fights[{i}].Method", "A draw has no method.");
                    }

                    if (fight.TechniqueId != null)
                    {
                        context.AddFailure($"Fights[{i}].TechniqueId", "A draw has no technique.");
                    }

                    continue;
                }

                if (fight.Method == null)
                {
                    context.AddFailure($"Fights[{i}].Method", "Choose how the fight was decided.");
                }

                if (fight.TechniqueId != null && fight.Method is not (FightMethod.Submission or FightMethod.Throw or FightMethod.Pin))
                {
                    context.AddFailure($"Fights[{i}].TechniqueId", "Only a submission, throw or pin has a technique.");
                }
            }
        });
    }
}

public class CompetitionRequestValidator : AbstractValidator<CompetitionRequest>
{
    public CompetitionRequestValidator(AppDbContext db)
    {
        Include(new CompetitionRequestFieldsValidator());

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

        // Techniques are offered in the discipline (one query for all fights)
        RuleFor(r => r.Fights).CustomAsync(async (fights, context, cancellationToken) =>
        {
            var disciplineId = context.InstanceToValidate.DisciplineId;
            var techniqueIds = fights.OfType<FightRequest>()
                .Where(f => f.TechniqueId != null)
                .Select(f => f.TechniqueId!.Value)
                .Distinct()
                .ToList();
            if (disciplineId == null || techniqueIds.Count == 0)
            {
                return;
            }

            var offered = await DisciplineRules.GetOfferedTechniqueIdsAsync(db, techniqueIds, disciplineId.Value, cancellationToken);
            for (var i = 0; i < fights.Count; i++)
            {
                if (fights[i]?.TechniqueId is { } techniqueId && !offered.Contains(techniqueId))
                {
                    context.AddFailure($"Fights[{i}].TechniqueId", "Choose a technique of this discipline.");
                }
            }
        });
    }
}

public class CompetitionListQueryValidator : AbstractValidator<CompetitionListQuery>
{
    public CompetitionListQueryValidator()
    {
        // Both dates are optional, but when both are given they are in order
        RuleFor(q => q.To)
            .Must((query, to) => to >= query.From)
            .WithMessage("Choose an end date that is not before the start date.")
            .When(q => q.From != null && q.To != null);
    }
}
