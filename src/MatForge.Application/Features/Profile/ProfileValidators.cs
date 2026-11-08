using FluentValidation;
using MatForge.Data;
using Microsoft.EntityFrameworkCore;

namespace MatForge.Application.Features.Profile;

public class UpdateBeltsRequestValidator : AbstractValidator<UpdateBeltsRequest>
{
    public UpdateBeltsRequestValidator(AppDbContext db)
    {
        // One belt per discipline, the belt belongs to that discipline, the degree is within the belt's range
        RuleFor(r => r.Belts).CustomAsync(async (belts, context, cancellationToken) =>
        {
            var beltIds = belts.OfType<BeltRequest>().Select(b => b.BeltId).Distinct().ToList();
            var knownBelts = await db.Belts
                .Where(b => beltIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, cancellationToken);

            var disciplineIds = new HashSet<int>();
            for (var i = 0; i < belts.Count; i++)
            {
                var item = belts[i];

                // A JSON array can contain null entries
                if (item is null)
                {
                    context.AddFailure($"Belts[{i}]", "Choose a belt.");
                    continue;
                }

                if (!disciplineIds.Add(item.DisciplineId))
                {
                    context.AddFailure($"Belts[{i}].DisciplineId", "Choose only one belt per discipline.");
                }

                if (!knownBelts.TryGetValue(item.BeltId, out var belt) || belt.DisciplineId != item.DisciplineId)
                {
                    context.AddFailure($"Belts[{i}].BeltId", "Choose a belt of this discipline.");
                }
                else if (item.Degree < belt.MinDegree || item.Degree > belt.MaxDegree)
                {
                    context.AddFailure($"Belts[{i}].Degree",
                        $"Choose a degree from {belt.MinDegree} to {belt.MaxDegree}.");
                }
            }
        });
    }
}
