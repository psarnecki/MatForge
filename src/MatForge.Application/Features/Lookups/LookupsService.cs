using MatForge.Data;
using Microsoft.EntityFrameworkCore;

namespace MatForge.Application.Features.Lookups;

public class LookupsService(AppDbContext db) : ILookupsService
{
    // For GET /api/lookups
    public async Task<LookupsDto> GetAsync(CancellationToken cancellationToken)
    {
        var formats = await db.DisciplineFormats
            .OrderBy(f => f.SortOrder).ThenBy(f => f.Name)
            .ToListAsync(cancellationToken);

        var belts = await db.Belts
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
            .ToListAsync(cancellationToken);

        // Formats and belts are nested under their discipline
        var disciplines = (await db.Disciplines
                .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
                .ToListAsync(cancellationToken))
            .Select(d => new DisciplineDto(d.Id, d.Code, d.Name,
                formats.Where(f => f.DisciplineId == d.Id)
                    .Select(f => new LookupItemDto(f.Id, f.Code, f.Name)).ToList(),
                belts.Where(b => b.DisciplineId == d.Id)
                    .Select(b => new BeltDto(b.Id, b.Code, b.Name, b.Rank, b.MinDegree, b.MaxDegree)).ToList()))
            .ToList();

        var characters = await db.TrainingCharacters
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new TrainingCharacterDto(c.Id, c.Code, c.Name, c.AllowsRounds))
            .ToListAsync(cancellationToken);

        var activityTypes = await db.ActivityTypes
            .OrderBy(a => a.SortOrder).ThenBy(a => a.Name)
            .Select(a => new LookupItemDto(a.Id, a.Code, a.Name))
            .ToListAsync(cancellationToken);

        var techniqueCategories = await db.TechniqueCategories
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new TechniqueCategoryDto(c.Id, c.Code, c.Name,
                c.Disciplines.OrderBy(d => d.DisciplineId).Select(d => d.DisciplineId).ToList()))
            .ToListAsync(cancellationToken);

        var techniques = await db.Techniques
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
            .Select(t => new TechniqueDto(t.Id, t.Code, t.Name, t.CategoryId))
            .ToListAsync(cancellationToken);

        var positions = await db.Positions
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Name)
            .Select(p => new PositionDto(p.Id, p.Code, p.Name,
                p.Disciplines.OrderBy(d => d.DisciplineId).Select(d => d.DisciplineId).ToList()))
            .ToListAsync(cancellationToken);

        var materialCategories = await db.MaterialCategories
            .OrderBy(m => m.SortOrder).ThenBy(m => m.Name)
            .Select(m => new LookupItemDto(m.Id, m.Code, m.Name))
            .ToListAsync(cancellationToken);

        return new LookupsDto(disciplines, characters, activityTypes, techniqueCategories, techniques, positions,
            materialCategories);
    }
}
